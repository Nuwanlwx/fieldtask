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
6. **Authentication / authorization**: added in Phase 3.
7. **Endpoints**

Every `ProblemDetails` body the API returns (400, 404, 409, 500) includes the same `correlationId`, so an error a user reports can be traced to its log lines.

## Logging

Serilog writes to two sinks, configured in `appsettings.json`:

- **Console**: what you see in the terminal while developing.
- **File**: `Logs/fieldtask-<date>.log`, one file per day (`rollingInterval: Day`), so history survives closing the terminal or restarting the app. `Logs/` is gitignored; it's runtime output, not source.

Every line carries the request's `{CorrelationId}`, so debugging a reported issue is: get the ID from the error response (or, once the frontend surfaces it, from the user), then search the log file for it, e.g. `Select-String "7a3f..." Logs\fieldtask-20260922.log`. That single search returns the full timeline for that one request, including any warnings logged from inside the service layer and the full stack trace if it threw.

**What I'd add for production**: this app is a single instance writing local files, fine for a demo but not for anything running on more than one machine. A real deployment would keep the Console sink as-is, since containerized apps are expected to write structured logs to stdout, and let the hosting platform (Docker/Kubernetes/App Service) collect it. The File sink would be replaced or supplemented by a centralized log aggregator, e.g. Serilog.Sinks.Seq, or Application Insights, so a correlation ID can be searched across every instance and every service at once, rather than one server's disk. Production would also add alerting on `ERR`-level volume (Sentry, Application Insights, or platform-native alerts) instead of relying on someone watching logs.
