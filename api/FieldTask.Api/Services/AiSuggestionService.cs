using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using FieldTask.Api.Dtos;

namespace FieldTask.Api.Services;

public class AiSuggestionService(IHttpClientFactory httpClientFactory, IConfiguration config, ITaskSearchService taskSearchService) : IAiSuggestionService
{
    private const string SystemPrompt =
        "You are an assistant that converts a customer's free-text complaint into a structured " +
        "maintenance task for a field service company. Always call the draft_task function exactly once. " +
        "Keep the title under 200 characters. Choose priority 1 (low/cosmetic) to 5 (urgent/safety risk) " +
        "based on what the complaint implies.";

    public async Task<TaskSuggestionDto> SuggestTaskAsync(string complaintText, CancellationToken ct = default)
    {
        // Independent of each other - drafting doesn't need the search results, and the search
        // doesn't need the draft - so run them concurrently instead of one after the other.
        var draftTask = DraftTaskAsync(complaintText, ct);
        var searchTask = taskSearchService.FindSimilarTasksAsync(complaintText, topN: 3, ct: ct);

        await Task.WhenAll(draftTask, searchTask);

        var (title, priority, summary) = await draftTask;
        var matches = await searchTask;

        return new TaskSuggestionDto(
            title,
            priority,
            summary,
            matches.Select(m => new PossibleDuplicateDto(m.TaskId, m.Title, m.Similarity)).ToList());
    }

    private async Task<(string Title, int Priority, string Summary)> DraftTaskAsync(string complaintText, CancellationToken ct)
    {
        var apiKey = config["Gemini:ApiKey"]
            ?? throw new InvalidOperationException("Gemini:ApiKey is not configured.");
        var model = config["Gemini:Model"] ?? "gemini-flash-latest";

        var requestBody = new
        {
            system_instruction = new { parts = new[] { new { text = SystemPrompt } } },
            contents = new object[] { new { role = "user", parts = new[] { new { text = complaintText } } } },
            tools = new object[]
            {
                new
                {
                    function_declarations = new object[]
                    {
                        new
                        {
                            name = "draft_task",
                            description = "Draft a structured maintenance task from a free-text complaint.",
                            parameters = new
                            {
                                type = "OBJECT",
                                properties = new
                                {
                                    title = new { type = "STRING", description = "Short task title, under 200 characters" },
                                    priority = new { type = "INTEGER", description = "1 (low) to 5 (urgent)" },
                                    summary = new { type = "STRING", description = "One or two sentence summary for the technician" }
                                },
                                required = new[] { "title", "priority", "summary" }
                            }
                        }
                    }
                }
            },
            tool_config = new
            {
                function_calling_config = new { mode = "ANY", allowed_function_names = new[] { "draft_task" } }
            }
        };

        using var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(20);
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";
        var requestJson = JsonSerializer.Serialize(requestBody);

        var body = await SendWithRetryAsync(client, url, requestJson, ct);

        using var doc = JsonDocument.Parse(body);
        var parts = doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts");

        var functionCallPart = parts.EnumerateArray().First(p => p.TryGetProperty("functionCall", out _));
        var args = functionCallPart.GetProperty("functionCall").GetProperty("args");

        return (
            args.GetProperty("title").GetString() ?? string.Empty,
            args.GetProperty("priority").GetInt32(),
            args.GetProperty("summary").GetString() ?? string.Empty);
    }

    // Gemini occasionally returns 503/429 under load, or the call just times out mid-queue -
    // both are transient, worth a couple of retries rather than failing the whole suggestion.
    private static readonly HttpStatusCode[] TransientStatusCodes =
    [
        HttpStatusCode.TooManyRequests,
        HttpStatusCode.ServiceUnavailable,
        HttpStatusCode.GatewayTimeout
    ];

    private static async Task<string> SendWithRetryAsync(HttpClient client, string url, string requestJson, CancellationToken ct)
    {
        const int maxAttempts = 3;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(requestJson, Encoding.UTF8, "application/json")
            };

            HttpResponseMessage response;
            try
            {
                response = await client.SendAsync(request, ct);
            }
            catch (TaskCanceledException) when (!ct.IsCancellationRequested && attempt < maxAttempts)
            {
                // HttpClient.Timeout elapsed, not caller cancellation - treat like a transient failure.
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), ct);
                continue;
            }

            using (response)
            {
                var body = await response.Content.ReadAsStringAsync(ct);

                if (response.IsSuccessStatusCode)
                    return body;

                if (!TransientStatusCodes.Contains(response.StatusCode) || attempt == maxAttempts)
                    throw new InvalidOperationException($"Gemini API call failed ({response.StatusCode}): {body}");

                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), ct);
            }
        }

        throw new UnreachableException();
    }
}
