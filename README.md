# FieldTask 
 
A mini field-service task manager demonstrating ASP.NET Core Web API (EF Core, SQL) + Angular 17 (NgRx) architecture. 
 
Architecture decisions are documented per section below. 

## Middleware order

The request pipeline is built in this order, and the order is deliberate. A request flows down the list; the response flows back up.

1. **Correlation ID**: reads `X-Correlation-Id` from the request (or creates a GUID), pushes it into Serilog's `LogContext`, and echoes it on the response. The incoming value is validated (max 64 characters, letters/digits/`-_.:` only) so a client cannot inject odd characters into the logs. It is outermost because it is trivial and cannot fail, and every log line below it, including the exception log, then carries the ID.
2. **Serilog request logging**: writes one summary line per request. It sits outside the exception handler so it sees the final status code, and so the same exception is not logged twice.
3. **Exception handler**: catches anything thrown further down the pipeline and returns a generic 500 `ProblemDetails`. The stack trace goes to the log only, never to the client; the response carries the `correlationId` so support can find the log entry.
4. **HTTPS redirection**
5. **CORS**: only the origins listed in `Cors:AllowedOrigins` (the Angular dev server) may call the API from a browser. It runs before authentication and endpoints, and its headers are already on the response if a later step fails.
6. **Authentication / authorization**: JWT bearer auth (`AddJwtBearer`). `POST /api/v1/auth/login` verifies a username/password against the seeded `Users` table and issues a signed token (`Jwt:SigningKey`, read from configuration at the moment the token handler needs it, not eagerly, so test overrides are respected). Every `TasksController` endpoint requires `[Authorize]`; `DELETE /tasks/{id}` additionally requires the `ManagerOnly` policy (`RequireRole("Manager")`). It runs after CORS so a rejected preflight never reaches it, and before endpoints so no handler runs for an unauthenticated/unauthorized request.
7. **Endpoints**

Every `ProblemDetails` body the API returns (400, 404, 409, 500) includes the same `correlationId`, so an error a user reports can be traced to its log lines.

## Logging

Serilog writes to two sinks, configured in `appsettings.json`:

- **Console**: what you see in the terminal while developing.
- **File**: `Logs/fieldtask-<date>.log`, one file per day (`rollingInterval: Day`), so history survives closing the terminal or restarting the app. `Logs/` is gitignored; it's runtime output, not source.

Every line carries the request's `{CorrelationId}`, so debugging a reported issue is: get the ID from the error response (or, once the frontend surfaces it, from the user), then search the log file for it, e.g. `Select-String "7a3f..." Logs\fieldtask-20260922.log`. That single search returns the full timeline for that one request, including any warnings logged from inside the service layer and the full stack trace if it threw.

**What I'd add for production**: this app is a single instance writing local files, fine for a demo but not for anything running on more than one machine. A real deployment would keep the Console sink as-is, since containerized apps are expected to write structured logs to stdout, and let the hosting platform (Docker/Kubernetes/App Service) collect it. The File sink would be replaced or supplemented by a centralized log aggregator, e.g. Serilog.Sinks.Seq, or Application Insights, so a correlation ID can be searched across every instance and every service at once, rather than one server's disk. Production would also add alerting on `ERR`-level volume (Sentry, Application Insights, or platform-native alerts) instead of relying on someone watching logs.

## AI task suggestion

`POST /api/v1/tasks/suggest` turns a free-text complaint into a structured task draft, using Google's Gemini API (`IAiSuggestionService` / `AiSuggestionService`). It's deliberately the first of a staged set of AI-agent features built on this same app — see `Services/AiSuggestionService.cs` for the implementation.

**How it works**: the complaint text is sent to Gemini with a single function declaration (`draft_task`) and `tool_config.function_calling_config.mode = "ANY"` pinned to that function — this forces a structured `{ title, priority, summary }` response instead of letting the model reply in free text. The endpoint sits behind the same `[Authorize]` as the rest of `TasksController`, so it isn't publicly callable.

**Human-confirm gate**: the endpoint only returns a *draft* — it never creates a task itself. The Angular `SuggestTaskPage` shows the draft in an editable form; a real task is only created when the user explicitly clicks "Confirm & Create Task", which calls the existing `POST /tasks` with whatever the form holds at that point (the AI's draft, possibly hand-edited). This is a deliberate design choice, not a missing feature — an AI agent that can write to the database unsupervised is a different (and much riskier) thing to build and review.

**Why Gemini's free tier**: this is a learning/demo project, not a revenue-generating product, so a paid API wasn't worth the ongoing cost. The tradeoff is real and worth stating plainly: the free tier is noticeably less reliable under load than a paid tier — see the gaps below.

**Gaps, found by actually using it, not theorized in advance:**
- **No retry on transient failures.** Gemini's free-tier model occasionally returns a 503 ("high demand") or simply doesn't respond before the `HttpClient` timeout. Right now that just fails the request; a production version would retry once or twice with backoff before giving up.
- **No output schema validation.** The response is parsed assuming Gemini's `functionCall.args` always has `title`/`priority`/`summary` in the expected shape; a malformed or missing field currently throws rather than failing gracefully with a clear error.
- **No rate limiting on the endpoint**, which matters more here than on the rest of the API since each call costs real (if currently free-tier) usage against an external quota.
- **No evaluation set.** There's no automated check that the model's suggestions stay reasonable over time (e.g. if the prompt is edited later) — only manual spot-checking so far.

These are intentionally left as named next steps, not silently ignored.

## Production gaps (what this demo deliberately skips)

This is a focused exercise to keep full-stack patterns sharp, not production software. Some choices below are simplifications appropriate for a small demo; others are things a real deployment would need on top.

**Database**
- Now on SQL Server (Developer Edition) instead of SQLite, with a real `rowversion` column backing optimistic concurrency — but it's still one local instance with no high availability. Production would run it as a managed service (Azure SQL, or SQL Server with an Always On availability group) and add connection resiliency (`EnableRetryOnFailure`) for transient network failures.
- The connection string uses Windows (integrated) authentication with no password, so it's harmless in `appsettings.json` for now. A real deployment authenticating with SQL credentials would need that connection string moved to User Secrets locally and a secret manager (Azure Key Vault, AWS Secrets Manager) once deployed — never in source control.

**Auth & security**
- JWT bearer auth is implemented (see Middleware order above). The signing key lives in User Secrets locally and a Container Apps secret in the deployed environment — never in `appsettings.json`.
- No refresh tokens — a token is valid until it expires, with no way to revoke it early (e.g. if a user's role changes or an account is disabled). A real deployment would add short-lived access tokens plus a refresh-token flow, or a server-side denylist.
- No rate limiting — nothing currently stops a client from hammering an endpoint, and this matters more now that `/api/v1/tasks/suggest` calls a billed-by-usage external AI API (see below). ASP.NET Core's built-in `AddRateLimiter` middleware would be the next step.
- CORS protects browser clients only; it is not authentication, and server-to-server callers would need their own auth story.

**Observability**
- Logs currently go to console and a local rolling file. A deployed system would ship the same structured logs to a centralized sink (Seq, Application Insights, ELK) so a correlation ID can be searched across every instance at once, not one server's disk.
- No `/health` endpoint for readiness/liveness probes (trivial to add with `AddHealthChecks()`), no metrics/dashboards, no distributed tracing (OpenTelemetry) across service calls.

**Reliability & delivery**
- xUnit tests exist (`FieldTask.Tests` — `TaskServiceTests`, `AuthEndpointTests`, a real `WebApplicationFactory` integration test against SQL Server), but there's no CI pipeline running them automatically — nothing currently blocks a regression from being committed and pushed.
- No idempotency handling on POST — a retried request (e.g. after a network blip) would create a duplicate task.
- No graceful shutdown handling for in-flight requests during a deploy.

**Deliberate simplifications** (not gaps so much as scope decisions, worth being able to explain)
- No repository/unit-of-work layer on top of EF Core — `DbContext` already fills both roles, and an extra layer would be unjustified indirection at this scale.
- No AutoMapper — DTO mapping is a few explicit lines per type, which stays easy to read and debug at this size; a much larger DTO surface might justify a mapping library.
- No caching layer (e.g. Redis) — reads are cheap enough at this scale that a cache would add complexity without a measurable benefit yet.
