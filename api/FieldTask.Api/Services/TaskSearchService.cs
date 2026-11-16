using FieldTask.Api.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace FieldTask.Api.Services;

public class TaskSearchService(FieldTaskDbContext db, IEmbeddingService embeddingService) : ITaskSearchService
{
    // Below this, a "match" is just noise, not a real duplicate candidate.
    private const double SimilarityThreshold = 0.80;

    public async Task<IReadOnlyList<TaskMatch>> FindSimilarTasksAsync(string queryText, int topN = 3, CancellationToken ct = default)
    {
        var queryEmbedding = await embeddingService.GetEmbeddingAsync(queryText, ct);

        var candidates = await db.ServiceTasks
            .Where(t => t.EmbeddingJson != null)
            .Select(t => new { t.Id, t.Title, t.EmbeddingJson })
            .ToListAsync(ct);

        return candidates
            .Select(t => new TaskMatch(
                t.Id,
                t.Title,
                CosineSimilarity(queryEmbedding, JsonSerializer.Deserialize<float[]>(t.EmbeddingJson!)!)))
            .Where(m => m.Similarity >= SimilarityThreshold)
            .OrderByDescending(m => m.Similarity)
            .Take(topN)
            .ToList();
    }

    private static double CosineSimilarity(float[] a, float[] b)
    {
        double dot = 0, magA = 0, magB = 0;
        for (var i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            magA += a[i] * a[i];
            magB += b[i] * b[i];
        }
        return dot / (Math.Sqrt(magA) * Math.Sqrt(magB));
    }
}
