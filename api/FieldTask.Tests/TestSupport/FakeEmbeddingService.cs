using FieldTask.Api.Services;

namespace FieldTask.Tests.TestSupport;

// Deterministic stand-in for Gemini: TaskService tests must never make a network call.
public class FakeEmbeddingService : IEmbeddingService
{
    public bool ShouldFail { get; init; }

    public Task<float[]> GetEmbeddingAsync(string text, CancellationToken ct = default) =>
        ShouldFail
            ? throw new HttpRequestException("Simulated Gemini outage")
            : Task.FromResult(new[] { 1f, 0f, 0f });
}
