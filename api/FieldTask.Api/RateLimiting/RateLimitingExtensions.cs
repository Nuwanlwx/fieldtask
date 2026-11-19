using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;

namespace FieldTask.Api.RateLimiting;

public static class RateLimitPolicies
{
    // One constant, used by both the registration and [EnableRateLimiting] - no magic strings drifting apart.
    public const string AiSuggest = "ai-suggest";
}

public static class RateLimitingExtensions
{
    public static IServiceCollection AddFieldTaskRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            // The default rejection status is 503, which tells clients "server broken", not "slow down".
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.OnRejected = async (context, ct) =>
            {
                var http = context.HttpContext;

                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    http.Response.Headers.RetryAfter =
                        ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
                }

                // Go through IProblemDetailsService so the body matches every other error
                // and picks up the correlationId from the CustomizeProblemDetails hook in Program.cs.
                var problemDetails = http.RequestServices.GetRequiredService<IProblemDetailsService>();
                await problemDetails.WriteAsync(new ProblemDetailsContext
                {
                    HttpContext = http,
                    ProblemDetails = new ProblemDetails
                    {
                        Status = StatusCodes.Status429TooManyRequests,
                        Title = "Too many requests",
                        Detail = "You have reached the AI suggestion limit. Try again shortly."
                    }
                });
            };

            options.AddPolicy(RateLimitPolicies.AiSuggest, httpContext =>
            {
                // Partition per user, not per IP: behind the Container Apps ingress (Envoy) or carrier NAT,
                // many users can share one IP. The endpoint is [Authorize], so Name should always be set;
                // the fallback just avoids a crash.
                var partitionKey = httpContext.User.Identity?.Name ?? "anonymous";

                return RateLimitPartition.GetTokenBucketLimiter(partitionKey, _ =>
                {
                    // Read config here (lazily, once per partition), not at startup - same reason as the
                    // JwtBearer options in Program.cs: test overrides applied during Build() must be visible.
                    var config = httpContext.RequestServices.GetRequiredService<IConfiguration>();

                    return new TokenBucketRateLimiterOptions
                    {
                        TokenLimit = config.GetValue("RateLimiting:Suggest:TokenLimit", 5),        // burst size
                        TokensPerPeriod = config.GetValue("RateLimiting:Suggest:TokensPerPeriod", 5),
                        ReplenishmentPeriod = TimeSpan.FromSeconds(
                            config.GetValue("RateLimiting:Suggest:ReplenishmentPeriodSeconds", 60)),
                        QueueLimit = 0,          // fail fast - a queued AI call just holds a connection open
                        AutoReplenishment = true
                    };
                });
            });
        });

        return services;
    }
}
