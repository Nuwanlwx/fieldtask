using System.Text.Json.Serialization;
using FieldTask.Api.Data;
using FieldTask.Api.Middleware;
using FieldTask.Api.Services;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog replaces the built-in logger. Levels and sinks come from the "Serilog" section of appsettings.json.
builder.Host.UseSerilog((context, services, loggerConfig) => loggerConfig
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenApi();

// Every ProblemDetails body (400, 404, 409, 500...) also carries the request's correlation id.
builder.Services.AddProblemDetails(options =>
    options.CustomizeProblemDetails = ctx =>
        ctx.ProblemDetails.Extensions["correlationId"] =
            ctx.HttpContext.Items[CorrelationIdMiddleware.ItemKey]);

// CORS: only the Angular dev server may call the API from a browser.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddPolicy("AngularDev", policy => policy
    .WithOrigins(allowedOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials() // needed in Phase 3 so the browser sends the auth cookie
    .WithExposedHeaders(CorrelationIdMiddleware.HeaderName))); // lets Angular read the header

builder.Services.AddDbContext<FieldTaskDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddSingleton<IClock, SystemClock>();

var app = builder.Build();

// Apply any pending migrations and seed demo data on startup.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FieldTaskDbContext>();
    db.Database.Migrate();
    DbSeeder.Seed(db);
}

// ---- Middleware pipeline: ORDER MATTERS. A request flows down this list, the response flows back up. ----
app.UseMiddleware<CorrelationIdMiddleware>();       // 1. tag the request first, so everything below is traceable
app.UseSerilogRequestLogging();                     // 2. one summary log line per request, sees the final status
app.UseMiddleware<ExceptionHandlingMiddleware>();   // 3. catches anything thrown below, returns a 500 ProblemDetails
app.UseHttpsRedirection();
app.UseCors("AngularDev");                          // 4. before auth and endpoints
// app.UseAuthentication();                         // 5. Phase 3
// app.UseAuthorization();
app.MapControllers();                               // 6. endpoints

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(); // interactive API docs at /scalar/v1
}

// Send the bare root URL to the API docs while developing.
app.MapGet("/", () => Results.Redirect("/scalar/v1"))
   .ExcludeFromDescription();

app.Run();
