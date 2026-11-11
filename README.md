# Support Desk

A small internal tool for tracking customer support tickets from creation through resolution, built with ASP.NET Core (backend) and Angular (frontend).

## Prerequisites

Before running this project, make sure you have the following installed:

- **.NET SDK 10** (this project was built and tested with SDK `10.0.400`)
- **Node.js** (v20+ recommended; built and tested with `v24.20.0`) and npm (bundled with Node)
- **Docker Desktop** (or Docker Engine), used to run PostgreSQL locally
- **Angular CLI** — not required as a global install; all commands below use `ng` via the project's local dependency, but you can install it globally with `npm install -g @angular/cli@21` if you prefer
- *(Optional)* **EF Core CLI tools**, only needed if you want to run database migrations manually rather than relying on the automatic migration-on-startup described below:
```bash
  dotnet tool install --global dotnet-ef
```

## How to Run the Backend and Frontend Locally

This project has three moving pieces: a PostgreSQL database (Docker), the ASP.NET Core API, and the Angular frontend. The database runs in the background; **the API and the frontend must be running at the same time, in two separate terminal windows**, since both are long-running processes that watch for changes and serve continuously.

### 1. Database Setup (PostgreSQL via Docker)

**First-time setup** — create and start a PostgreSQL container:

```bash
docker run --name supportdesk-db -e POSTGRES_USER=postgres -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=supportdesk -p 5432:5432 -d postgres
```

This creates a container named `supportdesk-db`, running Postgres on the default port `5432`, with a database called `supportdesk` and credentials `postgres` / `postgres`.

**On subsequent runs** (after the first time), don't re-run the command above — it will fail because the container already exists. Instead, just start the existing container:

```bash
docker start supportdesk-db
```

You can confirm it's running with `docker ps`.

> The API applies EF Core migrations and seeds the database (5 agents, 20 tickets across various statuses/priorities, including some pre-overdue) automatically on startup — no manual migration step is required for normal use. If you ever need to apply migrations manually, run this from the `SupportDesk.Api` directory:
> ```bash
> dotnet ef database update
> ```

### 2. Backend (ASP.NET Core API)

In a terminal, navigate to the API project and run it in watch mode:

```bash
cd SupportDesk.Api
dotnet watch run
```

The API will be available at `http://localhost:5191`. Leave this terminal running.

### 3. Frontend (Angular)

In a **second, separate terminal**, navigate to the frontend project and start the dev server:

```bash
cd supportdesk-frontend-v21
ng serve
```

The app will be available at `http://localhost:4200`. Leave this terminal running as well.

With both terminals running simultaneously, open `http://localhost:4200` in your browser to use the app.

### Running Tests

**Backend tests** — from the solution root (the `SupportDesk` folder containing both `SupportDesk.Api` and `SupportDesk.Api.Tests`):

```bash
dotnet test
```

This runs all backend unit tests (13 tests covering the status-transition state machine and due-date calculation logic).

**Frontend tests** — from the `supportdesk-frontend-v21` directory:

```bash
ng test
```

This runs the frontend unit tests (covering `TicketService`'s query-building logic and `TicketListComponent`'s data-loading behavior) via Angular's Vitest-based test runner.

## Where I Placed the Business Rules, and Why

All seven business rules from the assignment live in **`SupportDesk.Api/Services/TicketService.cs`**, rather than in the controllers or the entity models. Specifically:

- **Rule 1 (due date calculation/recalculation)** — `SlaCalculator.GetSla()`, called from `CreateAsync` and `UpdateAsync`
- **Rule 2 (allowed status transitions)** — the `AllowedTransitions` dictionary and the check in `ChangeStatusAsync`
- **Rule 3 (active agent required to move to In Progress)** — enforced inside `ChangeStatusAsync`
- **Rule 4 (inactive agents can't be assigned)** — enforced in `AssignAgentAsync`
- **Rule 5 (closed tickets are read-only)** — the shared `EnsureNotClosed()` guard, called from every mutating method (`UpdateAsync`, `ChangeStatusAsync`, `AssignAgentAsync`, `UnassignAgentAsync`, `AddCommentAsync`)
- **Rule 6 (system-set resolved/closed dates)** — set directly inside `ChangeStatusAsync`, never accepted from the client
- **Rule 7 (overdue calculation)** — the static `TicketService.IsOverdue()` method, reused both in the list-filtering query and in the response DTO mapping, so the definition of "overdue" only exists in one place

I chose to centralize all of this in a single service class rather than spreading it across controllers or putting it on the entity models themselves, for a few reasons:

1. **Controllers stay thin.** They're only responsible for HTTP concerns (routing, status codes, request/response shaping) — not business logic.
2. **Entities stay as plain data.** `Ticket` and `Agent` have no behavior of their own, which keeps EF Core's change tracking predictable and avoids "fat model" complexity.
3. **Testability.** Every rule can be unit tested directly against `TicketService` using an in-memory database, with no need to spin up the actual web host or hit real HTTP endpoints — this is what the 13 backend unit tests do.
4. **Single source of truth.** Since every mutating operation on a ticket funnels through this one service, there's no risk of a rule being enforced in one code path (say, the REST API) but silently skipped in another.

**On the dedicated status-change endpoint:** I used a dedicated `PATCH /api/tickets/{id}/status` endpoint (backed by `ChangeStatusAsync`) rather than allowing status to be set through the generic ticket-update endpoint. Status changes aren't a simple field edit — they carry real side effects (validating the transition is legal, checking agent assignment/active status, and system-setting `ResolvedDate`/`ClosedDate`). Folding that into a generic "update whatever fields the client sent" endpoint would make it easy to accidentally bypass those rules (e.g., a client PATCHing status alongside unrelated fields, or a future change to the generic update path forgetting to re-check transition rules). A dedicated endpoint keeps that logic in one obvious, single-responsibility place, and makes the API's intent explicit: changing a ticket's status is a distinct action, not a field edit.

## Assumptions

- **Reopening a resolved ticket clears its `ResolvedDate`.** The PDF doesn't explicitly say what happens to `ResolvedDate` when a ticket moves back from Resolved to In Progress. Since the ticket is no longer in a resolved state, I clear the timestamp on reopen — it gets set again if the ticket reaches Resolved a second time.
- **"While the ticket is still open" (Rule 1) is interpreted as "not Closed."** Since editing is already blocked entirely once a ticket is Closed (Rule 5), any priority change that's allowed to happen at all is, by definition, happening on a still-open ticket — so the due-date recalculation in `UpdateAsync` applies whenever priority changes, without a separate open/closed check.
- **Ticket reference generation has a known race condition under concurrent creates**, since it reads the "last used" reference before saving rather than using a database sequence or serializable transaction. This is acceptable for the scope of this assignment but would need a proper fix (e.g., a Postgres sequence) before production use with concurrent traffic.
- **Full CRUD for Agents was not implemented**, since the PDF explicitly marks it optional — only list (with search) and get-by-id are provided.
- **Delete confirmation uses the browser's native `confirm()` dialog** rather than a custom modal component. This satisfies the "confirmation step" requirement functionally, though a custom modal would be a nicer UX.

## What I Would Improve or Add With More Time

- Replace the native `confirm()` delete dialog with a proper custom confirmation modal for a more polished UX
- Add a database-backed test (e.g., via Testcontainers) specifically covering `ListAsync`'s Postgres-specific search filter (`EF.Functions.ILike`), which the current in-memory-database tests can't exercise
- Add a guard on the ticket-edit route to redirect away if a user navigates directly to a closed ticket's edit URL, rather than relying solely on the backend to reject the submission
- Introduce a proper toast/notification system for surfacing API errors, instead of inline error text
- Add authentication/authorization (e.g., so only assigned agents or admins can modify certain tickets)
- Add end-to-end tests covering full user flows (create → assign → transition → close) rather than only unit-level coverage
- Containerize the frontend and backend themselves (in addition to the database) for a true one-command local setup

## Time Spent

This assignment took roughly **6–8 hours** of work in total, across data modeling, business-rule implementation, the REST API, the Angular frontend, and testing.