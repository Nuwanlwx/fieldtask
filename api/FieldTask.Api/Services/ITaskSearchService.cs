namespace FieldTask.Api.Services;

public record TaskMatch(int TaskId, string Title, double Similarity);

public interface ITaskSearchService
{
    Task<IReadOnlyList<TaskMatch>> FindSimilarTasksAsync(string queryText, int topN = 3, CancellationToken ct = default);
}
