using FieldTask.Api.Services;

namespace FieldTask.Tests.TestSupport;

// Returns canned duplicate matches (or fails on demand) so AI tests never touch embeddings or the DB.
public class FakeTaskSearchService(IReadOnlyList<TaskMatch>? matches = null, bool shouldFail = false) : ITaskSearchService
{
    public Task<IReadOnlyList<TaskMatch>> FindSimilarTasksAsync(string queryText, int topN = 3, CancellationToken ct = default) =>
        shouldFail
            ? throw new HttpRequestException("Simulated embedding outage")
            : Task.FromResult(matches ?? []);
}
