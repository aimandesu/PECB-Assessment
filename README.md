# PECB Support Desk

A small ticketing system: an ASP.NET Core 9 minimal-API backend with a domain-driven
`Ticket` entity, and an Angular 21 frontend.

```
PECB-Assessment/
├── PECB-BE/          ASP.NET Core 9 API (Carter, EF Core, SQL Server)
│   └── Tests/        xUnit tests for the domain rules
└── PECB-FE/          Angular 21 client
```

---

## Getting started

### Prerequisites

| Tool       | Version            | Notes                                                                        |
| ---------- | ------------------ | ---------------------------------------------------------------------------- |
| .NET SDK   | 9.0 or newer       | Targets `net9.0`; `global.json` rolls forward to the latest installed major  |
| Node.js    | 20 or newer        | Developed on 22.17                                                           |
| SQL Server | any local instance | LocalDB (ships with Visual Studio / SQL Server Express) works out of the box |

### Run it

Two terminals, four commands. No configuration needed.

**Terminal 1 — API**

```bash
cd PECB-BE
dotnet run
```

**Terminal 2 — web app**

```bash
cd PECB-FE
npm install
npm start
```

Then open **<http://localhost:4200>**.

### What happens on first run

The API does its own setup in Development, so there is nothing to configure by hand:

1. **Creates the database.** Migrations are applied at startup, so there is no need to
   install the `dotnet-ef` global tool.
2. **Seeds three agents** (Technical, Billing, General) if the roster is empty. A ticket
   cannot move from New to In progress without an assigned agent, so an empty roster would
   make that rule impossible to try out.

You will land on an empty ticket list. Click **New ticket** to create one, then open it to
try comments, status transitions, assignment and delete.

### Ports

| What                                   | URL                                     |
| -------------------------------------- | --------------------------------------- |
| **Web app** — open this one            | <http://localhost:4200>                 |
| API (`http` launch profile)            | <http://localhost:5184>                 |
| API (`https` launch profile, optional) | <https://localhost:7046>                |
| OpenAPI document (Development only)    | <http://localhost:5184/openapi/v1.json> |

The Angular dev server proxies `/api/*` to `http://localhost:5184`
(`PECB-FE/proxy.conf.json`), so the browser only ever talks to `localhost:4200`. Nothing is
cross-origin, which is why **the API needs no CORS policy** for local development.

Either launch profile works. HTTPS redirection is disabled in Development precisely so the
proxied calls are not bounced from `5184` to `7046`; outside Development it is switched
back on.

### Configuration

| Setting           | Where                                         | Default                                   |
| ----------------- | --------------------------------------------- | ----------------------------------------- |
| Connection string | `PECB-BE/appsettings.json`                    | `(localdb)\MSSQLLocalDB`, database `PECB` |
| API port          | `PECB-BE/Properties/launchSettings.json`      | `5184` (http), `7046` (https)             |
| Web port          | `PECB-FE/angular.json` → `serve.options.port` | `4200`                                    |
| Proxy target      | `PECB-FE/proxy.conf.json`                     | `http://localhost:5184`                   |
| API base path     | `PECB-FE/src/environments/environment.ts`     | `/api`                                    |

To point at a different SQL Server instance, create `PECB-BE/appsettings.Development.json`
(it is gitignored, so it stays on your machine):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=.\\SQLEXPRESS;Initial Catalog=PECB;Integrated Security=True;Encrypt=False;TrustServerCertificate=True"
  }
}
```

If you change the API port, update `proxy.conf.json` to match.

### Troubleshooting

| Symptom                                                     | Cause and fix                                                                                                                   |
| ----------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------- |
| `Could not prepare the development database` in the API log | SQL Server is not reachable. Check the instance name in the connection string; run `sqllocaldb info` to list LocalDB instances. |
| The ticket list shows _Could not reach the API_             | The API is not running, or it is not on `5184`. Start Terminal 1, or update `proxy.conf.json`.                                  |
| `Failed to bind to address ... already in use`              | Another instance is still running. Stop it, or change the port in `launchSettings.json` and `proxy.conf.json`.                  |
| `dotnet` fails inside `PECB-BE/`                            | `global.json` is pinned to an SDK you do not have. It uses `rollForward: latestMajor`, so any SDK from 9.0 up works.            |
| The agent dropdown is empty                                 | Seeding only runs when the roster is empty and the database is reachable. Check the API log, or `POST /agent/create`.           |

---

## Architecture

### Backend — vertical slices

Each endpoint is a self-contained folder holding its request record, its FluentValidation
validator, and its Carter module. There is no shared controller, so a change to one
endpoint cannot ripple into another.

```
PECB-BE/
├── Entities/            Ticket, Agent, Comment — the business rules live here
├── Enums/               Status, Priority, Department, AssignerOption
├── Features/            one folder per endpoint
│   ├── Ticket/{Create,Get,GetAll,Update,Delete,Assigner}/
│   ├── Agent/{Create,Get,GetAll}/
│   └── Comment/Create/
├── Infrastructure/      ITicketRepository, IAgentRepository
├── Repository/          EF Core implementations
├── Database/            DbContext + IEntityTypeConfiguration classes
├── Shared/              ResultResponse envelope, Paginate<T>, DTOs, Mapster config,
│                        GlobalExceptionHandler
├── Migrations/
└── Tests/               xUnit test project (references PECB-BE.csproj)
```

Two things make the nested `Tests/` project work, and both are easy to break:

- `PECB-BE.csproj` excludes `Tests/**` from its item globs. The Web SDK compiles `**/*.cs`
  by default, so without that the test files would be built into the API assembly.
- `global.json` uses `rollForward: latestMajor`. With `latestMinor` and only a newer SDK
  installed, every `dotnet` command fails inside `PECB-BE/` — including the test run.

**Request flow:** `Carter module → FluentValidation → Repository → Entity (enforces the
rules) → Mapster → TicketDto → ResultResponse`.

**The domain rules live on the entity, not in a service.** `Ticket` guards its own
invariants: `UpdateStatus` refuses illegal transitions, `AssignAgent` refuses inactive
agents, `AddComment` refuses closed tickets, and the `Priority` setter recalculates
`DueDate`. `DueDate` and `AssignedAgent` have private setters so they cannot be set from
outside. That is why the unit tests target the entity directly and need no database.

**Errors** are normalised by `GlobalExceptionHandler`: an `InvalidOperationException`
thrown by the entity becomes `400 Server.InvalidOperation` with the rule message intact,
so a violated business rule reaches the user as a readable sentence.

### Frontend — layered standalone Angular

Angular 21: standalone components, signals, `@if`/`@for`, `OnPush` everywhere,
zoneless change detection, lazy-loaded routes.

```
PECB-FE/src/app/
├── core/
│   ├── domain/status-transitions.ts     client mirror of Ticket.UpdateStatus
│   ├── http/api-error.ts                backend error envelope → renderable error
│   ├── http/http-error.interceptor.ts   every failure becomes an ApiError
│   ├── models/                          DTO-shaped interfaces; enums as string unions
│   └── services/                        ticket, agent, comment, notification
├── shared/
│   ├── components/                      badge, pagination, spinner, empty-state,
│   │                                    confirm-dialog, field-error, error-summary, toasts
│   ├── pipes/due-date.pipe.ts           "in 3h" / "2d overdue"
│   └── validators.ts                    notBlank (mirrors FluentValidation NotEmpty)
└── features/tickets/
    ├── ticket-list/                     table, filters, paging
    ├── ticket-detail/                   info, comments, status, assignment
    └── ticket-form-dialog/              create + edit reactive form
```

**HTTP is isolated in services.** No component imports `HttpClient`; every call goes
through `TicketService`, `AgentService` or `CommentService`.

**State is signal-based.** The list page holds one `query` signal as the single source of
truth. It is converted to an observable and piped through `switchMap`, so a criteria change
cancels the request in flight and a slow response for an old search term can never
overwrite a newer one:

```
searchControl.valueChanges → debounceTime(350) ─┐
filterForm.valueChanges ───────────────────────►│ query signal ──► switchMap ──► API
pagination / page size ────────────────────────┘
```

**Search and filtering are server-side.** `TicketService.list()` puts every criterion on
the query string. Nothing is filtered in the browser.

**The transition table is duplicated deliberately.** `core/domain/status-transitions.ts`
mirrors the server's rules so the UI only ever offers legal transitions, but the server
stays the authority and re-validates every one. A rejection returns
`Server.InvalidOperation` and is shown in place.

**Closed tickets are read-only.** `readOnly()` gates the whole detail page. Form controls
are disabled through the form model rather than the `disabled` attribute, which reactive
forms warn against.

### How the two connect

`ng serve` proxies `/api/*` to `http://localhost:5184` via `proxy.conf.json`, so the
browser never makes a cross-origin request and **the backend needs no CORS policy for
local development**. Deploying the two separately would require adding CORS on the server
and pointing `src/environments/environment.ts` at the absolute API URL.

---

## Domain rules

**Reference** is a stored computed column: `TCK-{year}-{0000-padded TicketNumber}`,
e.g. `TCK-2026-0001`.

**Due date** is derived from priority at creation and recalculated whenever priority
changes — always measured from the creation time, so re-prioritising never restarts the
clock:

| Priority | Due      |
| -------- | -------- |
| Critical | +4 hours |
| High     | +1 day   |
| Normal   | +3 days  |
| Low      | +7 days  |

**Status transitions:**

```
New ──(requires an assigned agent)──► InProgress ──► Resolved ──► Closed
                                          ▲              │       (terminal,
                                          └──────────────┘        read-only)
```

Anything else is rejected. `Resolved` stamps `ResolvedDate`, `Closed` stamps `ClosedDate`
and seals the ticket: no status change, no edit, no new comments.

**Overdue** = the due date has passed _and_ the ticket is neither Resolved nor Closed.
Overdue rows are highlighted in the list with a red rule, a tinted background and a chip.

---

## API reference

Base URL `http://localhost:5184`. Enums are sent and received as **strings**
(`"InProgress"`, `"Critical"`, `"Technical"`, `"Assign"`).

Most endpoints wrap their payload in `ResultResponse<T>`:

```json
{ "data": {}, "description": "optional message" }
```

Errors use:

```json
{ "description": "CreateTicket.Validation", "customObject": "msg\r\nmsg" }
```

`description` is a machine code; `customObject` carries the human-readable detail
(FluentValidation joins its messages with newlines).

### Tickets

| Method   | Route              | Input                                                                                         |
| -------- | ------------------ | --------------------------------------------------------------------------------------------- |
| `GET`    | `/ticket/get-all`  | query: `search`, `status`, `priority`, `agentId`, `isTicketOverdue`, `pageNumber`, `pageSize` |
| `GET`    | `/ticket/get`      | query: `ticketId`                                                                             |
| `POST`   | `/ticket/create`   | body: `title, description, customerName, customerEmail, priority`                             |
| `PATCH`  | `/ticket/update`   | body: `id, title, description, customerName, customerEmail, priority, status`                 |
| `PATCH`  | `/ticket/status`   | body: `ticketId, status`                                                                      |
| `PATCH`  | `/ticket/assigner` | body: `ticketId, agentId, assignerOption`                                                     |
| `DELETE` | `/ticket/delete`   | query: `ticketId`                                                                             |

`GET /ticket/get-all` is the one endpoint that returns a **bare `Paginate<T>`** rather than
the envelope — `search` matches reference, title or customer name (OR'd, case-insensitive
substring), `pageSize` is clamped to 100, and results are ordered newest-first:

```json
{ "data": [], "currentPage": 1, "pageSize": 10, "total": 42, "lastPage": 5 }
```

### Comments and agents

| Method | Route                    | Input                              |
| ------ | ------------------------ | ---------------------------------- |
| `POST` | `/comment/ticket/create` | body: `ticketId, authorName, body` |
| `GET`  | `/agent/get-all`         | query: `department` (optional)     |
| `GET`  | `/agent/get`             | query: `agentId`                   |
| `POST` | `/agent/create`          | body: `fullName, department`       |

### Field limits

Mirrored by the frontend validators: title 100, description 250, customer name 50,
customer email 250, comment author 100, comment body 200.

---

## Tests

**Backend — 32 xUnit tests:**

```bash
dotnet test PECB-BE/Tests/PECB-BE.Tests.csproj
```

| File                                 | Covers                                                                               |
| ------------------------------------ | ------------------------------------------------------------------------------------ |
| `TicketStatusTransitionTests.cs`     | every legal and illegal transition, the agent precondition, the read-only seal       |
| `TicketDueDateTests.cs`              | the SLA per priority, recalculation, that re-prioritising does not restart the clock |
| `TicketAssignmentAndCommentTests.cs` | inactive-agent and wrong-assignee guards, comments on closed tickets                 |

They live in `PECB-BE/Tests/` and are part of `PECB-BE.sln`. They exercise the `Ticket`
entity directly, so they need no database and run in ~90 ms.

**Frontend — 23 Jasmine/Karma tests:**

```bash
cd PECB-FE
npm test
```

| File                         | Covers                                                                          |
| ---------------------------- | ------------------------------------------------------------------------------- |
| `status-transitions.spec.ts` | the client mirror of the transition rules                                       |
| `api-error.spec.ts`          | validation blob → per-field messages                                            |
| `ticket.service.spec.ts`     | _(a service)_ query params, envelope unwrap, rule violations                    |
| `ticket-list.spec.ts`        | _(a component)_ loading/empty/error states, overdue highlight, debounced search |

---
