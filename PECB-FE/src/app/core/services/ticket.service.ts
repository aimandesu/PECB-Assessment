import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiError } from '../http/api-error';
import { ApiEnvelope, PaginatedResult } from '../models/api.models';
import {
  AssignerOption,
  CreateTicketRequest,
  Status,
  Ticket,
  TicketQuery,
  UpdateTicketRequest,
} from '../models/ticket.models';

/**
 * The only place that knows how to talk to the ticket endpoints.
 * Components consume the observables it returns and never touch `HttpClient`.
 */
@Injectable({ providedIn: 'root' })
export class TicketService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/ticket`;

  /**
   * Server-side search, filtering and paging. Every criterion travels as a query
   * parameter; nothing is filtered in the browser.
   */
  list(query: TicketQuery): Observable<PaginatedResult<Ticket>> {
    let params = new HttpParams()
      .set('pageNumber', query.page)
      .set('pageSize', query.pageSize);

    const search = query.search.trim();
    if (search) {
      params = params.set('search', search);
    }
    if (query.status) {
      params = params.set('status', query.status);
    }
    if (query.priority) {
      params = params.set('priority', query.priority);
    }
    if (query.agentId) {
      params = params.set('agentId', query.agentId);
    }
    if (query.overdueOnly) {
      params = params.set('isTicketOverdue', true);
    }

    return this.http.get<PaginatedResult<Ticket>>(`${this.base}/get-all`, { params });
  }

  get(ticketId: string): Observable<Ticket | null> {
    const params = new HttpParams().set('ticketId', ticketId);
    return this.http
      .get<ApiEnvelope<Ticket | null>>(`${this.base}/get`, { params })
      .pipe(map((response) => response.data ?? null));
  }

  create(request: CreateTicketRequest): Observable<Ticket> {
    return this.http
      .post<ApiEnvelope<Ticket>>(`${this.base}/create`, request)
      .pipe(map((response) => required(response, 'The ticket could not be created.')));
  }

  update(request: UpdateTicketRequest): Observable<Ticket> {
    return this.http
      .patch<ApiEnvelope<Ticket | null>>(`${this.base}/update`, request)
      .pipe(map((response) => required(response, 'The ticket could not be updated.')));
  }

  delete(ticketId: string): Observable<void> {
    const params = new HttpParams().set('ticketId', ticketId);
    return this.http
      .delete<ApiEnvelope<Ticket | null>>(`${this.base}/delete`, { params })
      .pipe(map(() => undefined));
  }

  changeStatus(ticketId: string, status: Status): Observable<Ticket> {
    return this.http
      .patch<ApiEnvelope<Ticket | null>>(`${this.base}/status`, { ticketId, status })
      .pipe(map((response) => required(response, 'The status could not be changed.')));
  }

  setAgent(
    ticketId: string,
    agentId: string,
    assignerOption: AssignerOption,
  ): Observable<Ticket> {
    return this.http
      .patch<ApiEnvelope<Ticket | null>>(`${this.base}/assigner`, {
        ticketId,
        agentId,
        assignerOption,
      })
      .pipe(map((response) => required(response, 'The assignment could not be saved.')));
  }
}

/**
 * The backend answers "not found" with HTTP 200 and a null payload, so a missing
 * record has to be detected from the envelope rather than the status code.
 */
function required<T>(response: ApiEnvelope<T | null>, fallback: string): T {
  if (response.data == null) {
    throw new ApiError(404, 'Server.NotFound', [response.description ?? fallback], {});
  }
  return response.data;
}
