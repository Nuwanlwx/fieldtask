using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FieldTask.Api.Dtos;
using FieldTask.Api.Services;
using FieldTask.Tests.TestSupport;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FieldTask.Tests;

// End-to-end through the real pipeline (auth -> rate limiter -> controller), with the AI swapped
// for a fake and the limit lowered to 1 via configuration - the reason the limits live in config.
public class RateLimitTests(AuthApiFactory baseFactory) : IClassFixture<AuthApiFactory>
{
    private readonly WebApplicationFactory<Program> _factory = baseFactory.WithWebHostBuilder(builder =>
    {
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["RateLimiting:Suggest:TokenLimit"] = "1",
            ["RateLimiting:Suggest:TokensPerPeriod"] = "1",
            ["RateLimiting:Suggest:ReplenishmentPeriodSeconds"] = "3600", // no refill during the test
        }));
        builder.ConfigureTestServices(services =>
            services.AddScoped<IAiSuggestionService, FakeAiSuggestionService>());
    });

    // Each test uses its own users: the limiter's buckets live as long as the test server does.

    [Fact]
    public async Task SecondCall_SameUser_Returns429_WithRetryAfter()
    {
        var client = await LoggedInClient("alice", "manager123");

        var first = await Suggest(client);
        var second = await Suggest(client);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, second.StatusCode);
        Assert.NotNull(second.Headers.RetryAfter);

        var problem = await second.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
        Assert.Equal(429, problem!.Status);
        Assert.True(problem.Extensions.ContainsKey("correlationId"));
    }

    [Fact]
    public async Task OneUserHittingTheLimit_DoesNotBlockAnotherUser()
    {
        var bruno = await LoggedInClient("bruno", "tech123");
        var chidi = await LoggedInClient("chidi", "tech123");

        await Suggest(bruno);
        var brunoBlocked = await Suggest(bruno);
        var chidiAllowed = await Suggest(chidi);

        Assert.Equal(HttpStatusCode.TooManyRequests, brunoBlocked.StatusCode);
        Assert.Equal(HttpStatusCode.OK, chidiAllowed.StatusCode);
    }

    [Fact]
    public async Task OtherEndpoints_AreNotRateLimited()
    {
        var client = await LoggedInClient("alice", "manager123");

        for (var i = 0; i < 5; i++)
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/tasks")).StatusCode);
    }

    private static Task<HttpResponseMessage> Suggest(HttpClient client) =>
        client.PostAsJsonAsync("/api/v1/tasks/suggest", new SuggestTaskRequestDto("The pump in building B is leaking."));

    private async Task<HttpClient> LoggedInClient(string username, string password)
    {
        var client = _factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginDto(username, password));
        var result = await login.Content.ReadFromJsonAsync<LoginResultDto>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result!.Token);
        return client;
    }
}
