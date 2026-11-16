using System.Text;
using System.Text.Json;

namespace FieldTask.Api.Services;

public class EmbeddingService(IHttpClientFactory httpClientFactory, IConfiguration config) : IEmbeddingService
{
    public async Task<float[]> GetEmbeddingAsync(string text, CancellationToken ct = default)
    {
        var apiKey = config["Gemini:ApiKey"]
            ?? throw new InvalidOperationException("Gemini:ApiKey is not configured.");

        var requestBody = new
        {
            model = "models/gemini-embedding-001",
            content = new { parts = new[] { new { text } } }
        };

        using var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(20);
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-embedding-001:embedContent?key={apiKey}";
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        using var response = await client.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Gemini embedding call failed ({response.StatusCode}): {body}");

        using var doc = JsonDocument.Parse(body);
        var values = doc.RootElement.GetProperty("embedding").GetProperty("values");

        return values.EnumerateArray().Select(v => v.GetSingle()).ToArray();
    }
}
