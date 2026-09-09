# PECB Support Desk — Frontend

Angular 21 client for the `PECB-BE` ticketing API.

## Running

```bash
npm install
npm start            # http://localhost:4200
```

The backend is expected on `http://localhost:5184` (the `http` launch profile).
`ng serve` proxies `/api/*` to it via `proxy.conf.json`, so the browser never makes a
cross-origin request and **the backend does not need a CORS policy for local development**.
If you deploy the two separately, add CORS on the server and point
`src/environments/environment.ts` at the absolute API URL.

```bash
npm test             # Karma + Jasmine, headless Chrome
npm run build
```

## Structure

```
src/app/
  core/
    domain/status-transitions.ts   client mirror of Ticket.UpdateStatus
    http/api-error.ts              backend error envelope -> renderable error
    http/http-error.interceptor.ts
    models/                        DTO-shaped interfaces, enums as string unions
    services/                      the only place HttpClient is used
  shared/                          badge, pager, spinner, empty state, dialogs, field errors
  features/tickets/
    ticket-list/                   table, filters, paging
    ticket-detail/                 info, comments, status, assignment
    ticket-form-dialog/            create + edit reactive form
```

Components never touch `HttpClient`; every call goes through `TicketService`,
`AgentService` or `CommentService`.

## How the requirements are met

**Server-side search and filtering.** `TicketService.list()` puts every criterion on the
query string (`search`, `status`, `priority`, `agentId`, `isTicketOverdue`, `pageNumber`,
`pageSize`). Nothing is filtered in the browser.

**Debounced search.** The search box is a `FormControl`; its `valueChanges` run through
`debounceTime(350)`, and the resulting criteria feed a `switchMap` so a slow response for an
old term can never overwrite a newer one. Duplicate terms are dropped by comparing against
the term actually in flight rather than with `distinctUntilChanged`, whose internal memory
would survive `clearFilters()` resetting the control and then swallow the same term retyped.

**Allowed transitions only.** `allowedTransitions()` in `core/domain` mirrors the server's
rules, including that `New -> InProgress` needs an assigned agent (that option renders
disabled with the reason shown). The server stays the authority and re-validates; a
rejection comes back as `Server.InvalidOperation` and is shown in place.

**Closed tickets are read-only.** `readOnly()` drives the whole detail page. The comment
form and the agent select are disabled through the form model (not the `disabled`
attribute, which reactive forms warn against), and status/edit actions are disabled too.

**Validation.** Client rules mirror the server's FluentValidation rules and the entity
`[MaxLength]` attributes. Server messages arrive as a newline-joined string with no field
names, so `ApiError` attributes them back to controls by keyword and shows anything left
over in a summary banner.

## Seeding agents

Assignment needs at least one agent:

```bash
curl -X POST http://localhost:5184/agent/create \
  -H "Content-Type: application/json" \
  -d '{"fullName":"Dana Reyes","department":"Technical"}'
```

## See also

The repository root `README.md` covers the backend architecture, the full API reference and
the remaining known issues.
