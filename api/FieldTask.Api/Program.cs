using System.Text.Json.Serialization;
using FieldTask.Api.Data;
using FieldTask.Api.Middleware;
using FieldTask.Api.Services;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Serilog;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

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
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

builder.Services.AddHttpClient();
builder.Services.AddScoped<IAiSuggestionService, AiSuggestionService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

// Configure (rather than set options eagerly above): this resolves IConfiguration from DI the
// moment the token handler actually needs it, after the host has finished building - so it sees
// the final configuration, including anything WebApplicationFactory overrides for tests. Reading
// builder.Configuration directly here instead would capture whatever value existed before
// builder.Build() ran, which test overrides applied during Build() would never reach.
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IConfiguration>((options, config) =>
    {
        var signingKey = config["Jwt:SigningKey"]
            ?? throw new InvalidOperationException("Jwt:SigningKey is not configured.");

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "FieldTask.Api",
            ValidateAudience = true,
            ValidAudience = "FieldTask.Web",
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
        };
    });

builder.Services.AddAuthorization(options =>
    options.AddPolicy("ManagerOnly", policy => policy.RequireRole("Manager")));

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
app.UseAuthentication();                            // 5. who are you
app.UseAuthorization();                             // 6. are you allowed
app.MapControllers();                               // 7. endpoints

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(); // interactive API docs at /scalar/v1
}

// Send the bare root URL to the API docs while developing.
app.MapGet("/", () => Results.Redirect("/scalar/v1"))
   .ExcludeFromDescription();

app.Run();
