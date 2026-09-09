import { HttpErrorResponse } from '@angular/common/http';
import { ApiErrorBody } from '../models/api.models';

/**
 * Maps a FluentValidation message back to the form control it belongs to.
 *
 * The backend returns `ValidationResult.ToString()`, which is the message text only
 * with no property name attached, so the association has to be made by keyword.
 * Anything that does not match still shows up in the summary banner.
 */
const FIELD_HINTS: ReadonlyArray<readonly [RegExp, string]> = [
  [/\btitle\b/i, 'title'],
  [/\bdescription\b/i, 'description'],
  [/customer\s*name/i, 'customerName'],
  [/customer\s*email/i, 'customerEmail'],
  [/\bpriority\b/i, 'priority'],
  [/\bstatus\b/i, 'status'],
  [/author\s*name/i, 'authorName'],
  [/\bbody\b/i, 'body'],
];

/** A backend failure normalised into something a template can render directly. */
export class ApiError extends Error {
  constructor(
    /** HTTP status code; 0 when the request never reached the server. */
    readonly status: number,
    /** Backend error code, e.g. `CreateTicket.Validation` or `Server.InvalidOperation`. */
    readonly code: string,
    /** One entry per line of detail returned by the server. */
    readonly messages: string[],
    /** Messages that could be attributed to a specific form control. */
    readonly fieldErrors: Readonly<Record<string, string[]>>,
  ) {
    super(messages[0] ?? code);
    this.name = 'ApiError';
  }

  /** True for a business rule violation raised by the domain (e.g. an illegal transition). */
  get isRuleViolation(): boolean {
    return this.code === 'Server.InvalidOperation';
  }

  static fromHttp(response: HttpErrorResponse): ApiError {
    if (response.status === 0) {
      return new ApiError(
        0,
        'Network.Unreachable',
        ['Could not reach the API. Check that the backend is running on http://localhost:5184.'],
        {},
      );
    }

    const body = (response.error ?? {}) as ApiErrorBody;
    const code = typeof body.description === 'string' ? body.description : 'Server.Error';
    const messages = extractMessages(body, response);

    const fieldErrors: Record<string, string[]> = {};
    for (const message of messages) {
      const hint = FIELD_HINTS.find(([pattern]) => pattern.test(message));
      if (hint) {
        (fieldErrors[hint[1]] ??= []).push(message);
      }
    }

    return new ApiError(response.status, code, messages, fieldErrors);
  }
}

function extractMessages(body: ApiErrorBody, response: HttpErrorResponse): string[] {
  const detail = body.customObject;

  if (typeof detail === 'string' && detail.trim()) {
    // FluentValidation joins its messages with the server's newline separator.
    return detail
      .split(/\r?\n/)
      .map((line) => line.trim())
      .filter(Boolean);
  }

  if (Array.isArray(detail)) {
    return detail.map(String).filter(Boolean);
  }

  if (typeof body.description === 'string' && body.description.trim()) {
    return [body.description];
  }

  return [response.message || `Request failed with status ${response.status}.`];
}
