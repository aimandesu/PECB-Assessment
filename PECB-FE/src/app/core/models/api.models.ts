/**
 * Shapes returned by the ASP.NET Core backend.
 *
 * Most endpoints wrap their payload in `ResultResponse<T>`, which serialises to
 * `{ data, description? }`. `GET /ticket/get-all` is the exception: it returns a
 * bare `Paginate<T>`.
 */

/** `Success<T>` — the envelope used by every non-paginated endpoint. */
export interface ApiEnvelope<T> {
  data: T;
  description?: string;
}

/** `Paginate<T>` — returned unwrapped by the ticket list endpoint. */
export interface PaginatedResult<T> {
  data: T[];
  currentPage: number;
  pageSize: number;
  total: number;
  lastPage: number;
}

/** `Error` — the failure body. `customObject` carries the human-readable detail. */
export interface ApiErrorBody {
  description?: string;
  customObject?: unknown;
}
