import { ChangeDetectionStrategy, Component, computed, effect, inject, input, output, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize } from 'rxjs/operators';
import { allowedTransitions } from '../../../core/domain/status-transitions';
import { ApiError } from '../../../core/http/api-error';
import {
  PRIORITIES,
  Priority,
  Status,
  Ticket,
  TICKET_LIMITS,
  statusLabel,
} from '../../../core/models/ticket.models';
import { NotificationService } from '../../../core/services/notification.service';
import { TicketService } from '../../../core/services/ticket.service';
import { ErrorSummaryComponent } from '../../../shared/components/error-summary';
import { FieldErrorComponent } from '../../../shared/components/field-error';
import { notBlank } from '../../../shared/validators';

/**
 * Create/edit dialog.
 *
 * Client-side rules mirror the server's FluentValidation rules and the `[MaxLength]`
 * attributes on the entity, so the common cases never reach the network. Whatever
 * the server still rejects is fed back into the same fields.
 */
@Component({
  selector: 'app-ticket-form-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, FieldErrorComponent, ErrorSummaryComponent],
  templateUrl: './ticket-form-dialog.html',
  styleUrl: './ticket-form-dialog.scss',
})
export class TicketFormDialogComponent {
  private readonly fb = inject(FormBuilder);
  private readonly tickets = inject(TicketService);
  private readonly notifications = inject(NotificationService);

  readonly open = input.required<boolean>();
  /** `null` opens the dialog in create mode. */
  readonly ticket = input<Ticket | null>(null);

  readonly saved = output<Ticket>();
  readonly closed = output<void>();

  protected readonly limits = TICKET_LIMITS;
  protected readonly priorities = PRIORITIES;
  protected readonly statusLabel = statusLabel;

  protected readonly submitting = signal(false);
  protected readonly serverError = signal<ApiError | null>(null);

  protected readonly form = this.fb.nonNullable.group({
    title: ['', [Validators.required, notBlank, Validators.maxLength(TICKET_LIMITS.title)]],
    description: [
      '',
      [Validators.required, notBlank, Validators.maxLength(TICKET_LIMITS.description)],
    ],
    customerName: [
      '',
      [Validators.required, notBlank, Validators.maxLength(TICKET_LIMITS.customerName)],
    ],
    customerEmail: [
      '',
      [
        Validators.required,
        notBlank,
        Validators.email,
        Validators.maxLength(TICKET_LIMITS.customerEmail),
      ],
    ],
    priority: ['Normal' as Priority, [Validators.required]],
    status: ['New' as Status, [Validators.required]],
  });

  protected readonly isEdit = computed(() => this.ticket() !== null);

  /**
   * In edit mode the only selectable statuses are the current one plus the
   * transitions the domain currently permits, because `PATCH /ticket/update`
   * runs the same transition check as the dedicated status endpoint.
   */
  protected readonly statusOptions = computed<Status[]>(() => {
    const current = this.ticket();
    if (!current) {
      return ['New'];
    }
    const reachable = allowedTransitions(current.status, current.assignedAgent !== null)
      .filter((option) => !option.blocked)
      .map((option) => option.target);
    return [current.status, ...reachable];
  });

  protected readonly fieldErrors = computed(() => this.serverError()?.fieldErrors ?? {});

  constructor() {
    // Re-seed the form whenever the dialog is opened, so a cancelled edit does not
    // leak its values into the next one.
    effect(() => {
      if (!this.open()) {
        return;
      }
      const current = this.ticket();
      this.serverError.set(null);
      this.form.reset({
        title: current?.title ?? '',
        description: current?.description ?? '',
        customerName: current?.customerName ?? '',
        customerEmail: current?.customerEmail ?? '',
        priority: current?.priority ?? 'Normal',
        status: current?.status ?? 'New',
      });
    });
  }

  protected errorsFor(field: string): readonly string[] {
    return this.fieldErrors()[field] ?? [];
  }

  protected submit(): void {
    if (this.submitting()) {
      return;
    }

    this.form.markAllAsTouched();
    this.serverError.set(null);

    if (this.form.invalid) {
      return;
    }

    const value = this.form.getRawValue();
    const payload = {
      title: value.title.trim(),
      description: value.description.trim(),
      customerName: value.customerName.trim(),
      customerEmail: value.customerEmail.trim(),
      priority: value.priority,
    };

    const existing = this.ticket();
    const request$ = existing
      ? this.tickets.update({ ...payload, id: existing.id, status: value.status })
      : this.tickets.create(payload);

    this.submitting.set(true);
    request$.pipe(finalize(() => this.submitting.set(false))).subscribe({
      next: (ticket) => {
        this.notifications.success(
          existing ? `Ticket ${ticket.reference} updated.` : `Ticket ${ticket.reference} created.`,
        );
        this.saved.emit(ticket);
      },
      error: (error: unknown) => {
        if (error instanceof ApiError) {
          this.serverError.set(error);
        } else {
          this.serverError.set(
            new ApiError(0, 'Client.Unexpected', ['An unexpected error occurred.'], {}),
          );
        }
      },
    });
  }

  protected cancel(): void {
    if (!this.submitting()) {
      this.closed.emit();
    }
  }
}
