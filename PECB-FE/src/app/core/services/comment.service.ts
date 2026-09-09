import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiEnvelope } from '../models/api.models';
import { TicketComment } from '../models/ticket.models';

export interface CreateCommentRequest {
  ticketId: string;
  authorName: string;
  body: string;
}

@Injectable({ providedIn: 'root' })
export class CommentService {
  private readonly http = inject(HttpClient);

  /**
   * The server refuses comments on a closed ticket, so a rule violation here
   * surfaces as an `ApiError` with code `Server.InvalidOperation`.
   */
  create(request: CreateCommentRequest): Observable<TicketComment> {
    return this.http
      .post<ApiEnvelope<TicketComment>>(
        `${environment.apiBaseUrl}/comment/ticket/create`,
        request,
      )
      .pipe(map((response) => response.data));
  }
}
