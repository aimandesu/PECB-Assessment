import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, effect, inject, signal } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { of } from 'rxjs';
import { catchError, finalize, map, startWith, switchMap } from 'rxjs/operators';
import { allowedTransitions, isReadOnly } from '../../../core/domain/status-transitions';
import { ApiError } from '../../../core/http/api-error';
import {
  Status,
  Ticket,
  TICKET_LIMITS,
  statusLabel,
} from '../../../core/models/ticket.models';
import { AgentService } from '../../../core/services/agent.service';
import { CommentService } from '../../../core/services/comment.service';
import { NotificationService } from '../../../core/services/notification.service';
import { TicketService } from '../../../core/services/ticket.service';
import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog';
import { EmptyStateComponent } from '../../../shared/components/empty-state';
import { ErrorSummaryComponent } from '../../../shared/components/error-summary';
import { FieldErrorComponent } from '../../../shared/components/field-error';
import { SpinnerComponent } from '../../../shared/components/spinner';
import { TicketBadgeComponent } from '../../../shared/components/ticket-badge';
import { DueInPipe } from '../../../shared/pipes/due-date.pipe';
import { notBlank } from '../../../shared/validators';
import { TicketFormDialogComponent } from '../ticket-form-dialog/ticket-form-dialog';

interface DetailState {
  loading: boolean;
  error: ApiError | null;
  ticket: Ticket | null;
}

@Component({
  selector: 'app-ticket-detail',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    DatePipe,
    DueInPipe,
    TicketBadgeComponent,
    SpinnerComponent,
    EmptyStateComponent,
    ErrorSummaryComponent,
    FieldErrorComponent,
    ConfirmDialogComponent,
    TicketFormDialogComponent,
  ],
  templateUrl: './ticket-detail.html',
  styleUrl: './ticket-detail.scss',
})
export class TicketDetailComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly tickets = inject(TicketService);
  private readonly comments = inject(CommentService);
  private readonly agentsApi = inject(AgentService);
  private readonly notifications = inject(NotificationService);
  private readonly fb = inject(FormBuilder);

  protected readonly limits = TICKET_LIMITS;
  protected readonly statusLabel = statusLabel;

  private readonly ticketId = toSignal(
    this.route.paramMap.pipe(map((params) => params.get('id') ?? '')),
    { initialValue: '' },
  );

  /** Bumped after any mutation so the whole ticket is re-read from the server. */
  private readonly revision = signal(0);

  private readonly request = computed(() => ({
    id: this.ticketId(),
    revision: this.revision(),
  }));

  protected readonly state = toSignal(
    toObservable(this.request).pipe(
      switchMap(({ id }) =>
        this.tickets.get(id).pipe(
          map((ticket): DetailState => ({ loading: false, error: null, ticket })),
          catchError((error: unknown) =>
            of<DetailState>({ loading: false, error: asApiError(error), ticket: null }),
          ),
          startWith<DetailState>({ loading: true, error: null, ticket: null }),
        ),
      ),
    ),
    { initialValue: { loading: true, error: null, ticket: null } as DetailState },
  );

  protected readonly ticket = computed(() => this.state().ticket);

  /** A closed ticket is read-only: every mutating control on the page is disabled. */
  protected readonly readOnly = computed(() => {
    const ticket = this.ticket();
    return ticket ? isReadOnly(ticket.status) : true;
  });

  protected readonly transitions = computed(() => {
    const ticket = this.ticket();
    if (!ticket) {
      return [];
    }
    return allowedTransitions(ticket.status, ticket.assignedAgent !== null);
  });

  protected readonly agents = toSignal(
    this.agentsApi.list().pipe(catchError(() => of([]))),
    { initialValue: [] },
  );

  protected readonly activeAgents = computed(() => this.agents().filter((agent) => agent.active));

  protected readonly assignedAgent = computed(() => {
    const id = this.ticket()?.assignedAgent;
    return id ? (this.agents().find((agent) => agent.id === id) ?? null) : null;
  });

  protected readonly sortedComments = computed(() =>
    [...(this.ticket()?.comments ?? [])].sort(
      (a, b) => new Date(a.createdDate).getTime() - new Date(b.createdDate).getTime(),
    ),
  );

  protected readonly agentControl = new FormControl('', { nonNullable: true });

  protected readonly commentForm = this.fb.nonNullable.group({
    authorName: [
      '',
      [Validators.required, notBlank, Validators.maxLength(TICKET_LIMITS.commentAuthor)],
    ],
    body: ['', [Validators.required, notBlank, Validators.maxLength(TICKET_LIMITS.commentBody)]],
  });

  /** Failures from the inline actions, shown above the panel that caused them. */
  protected readonly statusError = signal<ApiError | null>(null);
  protected readonly assignError = signal<ApiError | null>(null);
  protected readonly commentError = signal<ApiError | null>(null);

  protected readonly changingStatus = signal(false);
  protected readonly assigning = signal(false);
  protected readonly postingComment = signal(false);
  protected readonly deleting = signal(false);

  protected readonly formOpen = signal(false);
  protected readonly confirmingDelete = signal(false);

  constructor() {
    // A closed ticket disables its inputs through the form model rather than the
    // `disabled` attribute, which reactive forms explicitly warn against.
    effect(() => {
      const locked = this.readOnly();
      const options = { emitEvent: false };
      if (locked) {
        this.commentForm.disable(options);
        this.agentControl.disable(options);
      } else {
        this.commentForm.enable(options);
        this.agentControl.enable(options);
      }
    });
  }

  /**
   * `TicketDto.CreatedDate` currently arrives as `0001-01-01` because the entity's
   * `CreatedDate` is private and Mapster cannot read it. Rendering a placeholder is
   * more honest than showing a date from the year 1.
   */
  protected hasRealDate(value: string | null): boolean {
    if (!value) {
      return false;
    }
    const parsed = new Date(value);
    return !Number.isNaN(parsed.getTime()) && parsed.getUTCFullYear() > 1;
  }

  protected commentServerErrors(field: string): readonly string[] {
    return this.commentError()?.fieldErrors[field] ?? [];
  }

  protected changeStatus(target: Status): void {
    const ticket = this.ticket();
    if (!ticket || this.changingStatus() || this.readOnly()) {
      return;
    }

    this.statusError.set(null);
    this.changingStatus.set(true);

    this.tickets
      .changeStatus(ticket.id, target)
      .pipe(finalize(() => this.changingStatus.set(false)))
      .subscribe({
        next: () => {
          this.notifications.success(`Status changed to ${statusLabel(target)}.`);
          this.reload();
        },
        error: (error: unknown) => this.statusError.set(asApiError(error)),
      });
  }

  protected assign(): void {
    const ticket = this.ticket();
    const agentId = this.agentControl.value;
    if (!ticket || !agentId || this.assigning() || this.readOnly()) {
      return;
    }

    this.assignError.set(null);
    this.assigning.set(true);

    this.tickets
      .setAgent(ticket.id, agentId, 'Assign')
      .pipe(finalize(() => this.assigning.set(false)))
      .subscribe({
        next: () => {
          this.notifications.success('Agent assigned.');
          this.agentControl.setValue('');
          this.reload();
        },
        error: (error: unknown) => this.assignError.set(asApiError(error)),
      });
  }

  protected unassign(): void {
    const ticket = this.ticket();
    // The server verifies that the agent being removed is the one on the ticket,
    // so the currently assigned id has to be sent back.
    const agentId = ticket?.assignedAgent;
    if (!ticket || !agentId || this.assigning() || this.readOnly()) {
      return;
    }

    this.assignError.set(null);
    this.assigning.set(true);

    this.tickets
      .setAgent(ticket.id, agentId, 'Unassign')
      .pipe(finalize(() => this.assigning.set(false)))
      .subscribe({
        next: () => {
          this.notifications.success('Agent unassigned.');
          this.reload();
        },
        error: (error: unknown) => this.assignError.set(asApiError(error)),
      });
  }

  protected addComment(): void {
    const ticket = this.ticket();
    if (!ticket || this.postingComment() || this.readOnly()) {
      return;
    }

    this.commentForm.markAllAsTouched();
    this.commentError.set(null);

    if (this.commentForm.invalid) {
      return;
    }

    const value = this.commentForm.getRawValue();
    this.postingComment.set(true);

    this.comments
      .create({
        ticketId: ticket.id,
        authorName: value.authorName.trim(),
        body: value.body.trim(),
      })
      .pipe(finalize(() => this.postingComment.set(false)))
      .subscribe({
        next: () => {
          // Keep the author so a reviewer can post several comments in a row.
          this.commentForm.controls.body.reset('');
          this.commentForm.controls.body.markAsUntouched();
          this.reload();
        },
        error: (error: unknown) => this.commentError.set(asApiError(error)),
      });
  }

  protected openEdit(): void {
    if (!this.readOnly()) {
      this.formOpen.set(true);
    }
  }

  protected closeForm(): void {
    this.formOpen.set(false);
  }

  protected onSaved(): void {
    this.formOpen.set(false);
    this.reload();
  }

  protected confirmDelete(): void {
    const ticket = this.ticket();
    if (!ticket || this.deleting()) {
      return;
    }

    this.deleting.set(true);
    this.tickets
      .delete(ticket.id)
      .pipe(finalize(() => this.deleting.set(false)))
      .subscribe({
        next: () => {
          this.confirmingDelete.set(false);
          this.notifications.success(`Ticket ${ticket.reference} deleted.`);
          void this.router.navigate(['/tickets']);
        },
        error: (error: unknown) => {
          this.confirmingDelete.set(false);
          this.notifications.error(asApiError(error).message);
        },
      });
  }

  private reload(): void {
    this.revision.update((value) => value + 1);
  }
}

function asApiError(error: unknown): ApiError {
  return error instanceof ApiError
    ? error
    : new ApiError(0, 'Client.Unexpected', ['An unexpected error occurred.'], {});
}
