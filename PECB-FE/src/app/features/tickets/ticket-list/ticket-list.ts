import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable, toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, FormControl, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { of } from 'rxjs';
import { catchError, debounceTime, map, startWith, switchMap } from 'rxjs/operators';
import { ApiError } from '../../../core/http/api-error';
import { PaginatedResult } from '../../../core/models/api.models';
import {
  PRIORITIES,
  Priority,
  STATUSES,
  Status,
  Ticket,
  TicketQuery,
  statusLabel,
} from '../../../core/models/ticket.models';
import { AgentService } from '../../../core/services/agent.service';
import { NotificationService } from '../../../core/services/notification.service';
import { TicketService } from '../../../core/services/ticket.service';
import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog';
import { EmptyStateComponent } from '../../../shared/components/empty-state';
import { ErrorSummaryComponent } from '../../../shared/components/error-summary';
import { PaginationComponent } from '../../../shared/components/pagination';
import { SpinnerComponent } from '../../../shared/components/spinner';
import { TicketBadgeComponent } from '../../../shared/components/ticket-badge';
import { DueInPipe } from '../../../shared/pipes/due-date.pipe';
import { TicketFormDialogComponent } from '../ticket-form-dialog/ticket-form-dialog';

/** What the table renders at any moment: exactly one of loading, error, or data. */
interface ListState {
  loading: boolean;
  error: ApiError | null;
  result: PaginatedResult<Ticket> | null;
}

const PAGE_SIZES = [10, 25, 50] as const;

@Component({
  selector: 'app-ticket-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    DatePipe,
    DueInPipe,
    TicketBadgeComponent,
    PaginationComponent,
    SpinnerComponent,
    EmptyStateComponent,
    ErrorSummaryComponent,
    ConfirmDialogComponent,
    TicketFormDialogComponent,
  ],
  templateUrl: './ticket-list.html',
  styleUrl: './ticket-list.scss',
})
export class TicketListComponent {
  private readonly tickets = inject(TicketService);
  private readonly agentsApi = inject(AgentService);
  private readonly notifications = inject(NotificationService);
  private readonly fb = inject(FormBuilder);

  protected readonly statuses = STATUSES;
  protected readonly priorities = PRIORITIES;
  protected readonly pageSizes = PAGE_SIZES;
  protected readonly statusLabel = statusLabel;

  /** The single source of truth for what the server is asked for. */
  private readonly query = signal<TicketQuery>({
    page: 1,
    pageSize: 10,
    search: '',
    status: null,
    priority: null,
    agentId: null,
    overdueOnly: false,
  });

  /** Bumped after a mutation to force a refetch with the same criteria. */
  private readonly revision = signal(0);

  protected readonly searchControl = new FormControl('', { nonNullable: true });

  protected readonly filterForm = this.fb.nonNullable.group({
    status: '',
    priority: '',
    agentId: '',
    overdueOnly: false,
  });

  protected readonly agents = toSignal(
    this.agentsApi.list().pipe(catchError(() => of([]))),
    { initialValue: [] },
  );

  /** Ticket DTOs carry only the agent id, so names are resolved from the roster. */
  private readonly agentNames = computed(
    () => new Map(this.agents().map((agent) => [agent.id, agent.fullName])),
  );

  private readonly request = computed(() => ({
    query: this.query(),
    revision: this.revision(),
  }));

  /**
   * Every criteria change cancels the request in flight (`switchMap`) so a slow
   * response for an old term can never overwrite a newer one.
   */
  protected readonly state = toSignal(
    toObservable(this.request).pipe(
      switchMap(({ query }) =>
        this.tickets.list(query).pipe(
          map((result): ListState => ({ loading: false, error: null, result })),
          catchError((error: unknown) =>
            of<ListState>({ loading: false, error: asApiError(error), result: null }),
          ),
          startWith<ListState>({ loading: true, error: null, result: null }),
        ),
      ),
    ),
    { initialValue: { loading: true, error: null, result: null } as ListState },
  );

  protected readonly rows = computed(() => this.state().result?.data ?? []);
  /** Read from the criteria rather than the response, so it holds steady while loading. */
  protected readonly pageSize = computed(() => this.query().pageSize);
  protected readonly hasFilters = computed(() => {
    const query = this.query();
    return Boolean(
      query.search || query.status || query.priority || query.agentId || query.overdueOnly,
    );
  });

  protected readonly formOpen = signal(false);
  protected readonly editing = signal<Ticket | null>(null);
  protected readonly pendingDelete = signal<Ticket | null>(null);
  protected readonly deleting = signal(false);

  constructor() {
    // A debounced search stream: one request per settled term.
    //
    // De-duplication compares against the term actually in flight rather than using
    // `distinctUntilChanged`, whose internal memory would survive `clearFilters()`
    // resetting the control silently and then wrongly swallow the same term retyped.
    this.searchControl.valueChanges
      .pipe(
        map((value) => value.trim()),
        debounceTime(350),
        takeUntilDestroyed(),
      )
      .subscribe((search) => {
        if (search !== this.query().search) {
          this.patchQuery({ search });
        }
      });

    this.filterForm.valueChanges.pipe(takeUntilDestroyed()).subscribe((value) =>
      this.patchQuery({
        status: (value.status || null) as Status | null,
        priority: (value.priority || null) as Priority | null,
        agentId: value.agentId || null,
        overdueOnly: value.overdueOnly ?? false,
      }),
    );
  }

  protected agentName(agentId: string | null): string {
    if (!agentId) {
      return 'Unassigned';
    }
    return this.agentNames().get(agentId) ?? 'Unknown agent';
  }

  protected goToPage(page: number): void {
    this.query.update((query) => ({ ...query, page }));
  }

  protected changePageSize(value: string): void {
    this.patchQuery({ pageSize: Number(value) });
  }

  protected clearFilters(): void {
    this.searchControl.setValue('', { emitEvent: false });
    this.filterForm.reset({ status: '', priority: '', agentId: '', overdueOnly: false });
    this.query.update((query) => ({
      ...query,
      page: 1,
      search: '',
      status: null,
      priority: null,
      agentId: null,
      overdueOnly: false,
    }));
  }

  protected openCreate(): void {
    this.editing.set(null);
    this.formOpen.set(true);
  }

  protected openEdit(ticket: Ticket): void {
    this.editing.set(ticket);
    this.formOpen.set(true);
  }

  protected closeForm(): void {
    this.formOpen.set(false);
    this.editing.set(null);
  }

  protected onSaved(): void {
    this.closeForm();
    this.refresh();
  }

  protected askDelete(ticket: Ticket): void {
    this.pendingDelete.set(ticket);
  }

  protected cancelDelete(): void {
    if (!this.deleting()) {
      this.pendingDelete.set(null);
    }
  }

  protected confirmDelete(): void {
    const target = this.pendingDelete();
    if (!target || this.deleting()) {
      return;
    }

    this.deleting.set(true);
    this.tickets.delete(target.id).subscribe({
      next: () => {
        this.deleting.set(false);
        this.pendingDelete.set(null);
        this.notifications.success(`Ticket ${target.reference} deleted.`);
        this.stepBackIfPageEmptied();
        this.refresh();
      },
      error: (error: unknown) => {
        this.deleting.set(false);
        this.pendingDelete.set(null);
        this.notifications.error(asApiError(error).message);
      },
    });
  }

  private refresh(): void {
    this.revision.update((value) => value + 1);
  }

  /** Deleting the last row of the last page would otherwise leave the user on an empty page. */
  private stepBackIfPageEmptied(): void {
    const query = this.query();
    if (this.rows().length === 1 && query.page > 1) {
      this.query.update((current) => ({ ...current, page: current.page - 1 }));
    }
  }

  /** Any change to the criteria returns the user to the first page. */
  private patchQuery(patch: Partial<TicketQuery>): void {
    this.query.update((query) => ({ ...query, ...patch, page: 1 }));
  }
}

function asApiError(error: unknown): ApiError {
  return error instanceof ApiError
    ? error
    : new ApiError(0, 'Client.Unexpected', ['An unexpected error occurred.'], {});
}
