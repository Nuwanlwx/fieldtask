using System.Text;
using System.Text.Json;
using FieldTask.Api.Dtos;

namespace FieldTask.Api.Services;

public class AiSuggestionService(IHttpClientFactory httpClientFactory, IConfiguration config) : IAiSuggestionService
{
    private const string SystemPrompt =
        "You are an assistant that converts a customer's free-text complaint into a structured " +
        "maintenance task for a field service company. Always call the draft_task function exactly once. " +
        "Keep the title under 200 characters. Choose priority 1 (low/cosmetic) to 5 (urgent/safety risk) " +
        "based on what the complaint implies.";

    public async Task<TaskSuggestionDto> SuggestTaskAsync(string complaintText, CancellationToken ct = default)
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
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        using var response = await client.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Gemini API call failed ({response.StatusCode}): {body}");

        using var doc = JsonDocument.Parse(body);
        var parts = doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts");

        var functionCallPart = parts.EnumerateArray().First(p => p.TryGetProperty("functionCall", out _));
        var args = functionCallPart.GetProperty("functionCall").GetProperty("args");

        return new TaskSuggestionDto(
            Title: args.GetProperty("title").GetString() ?? string.Empty,
            Priority: args.GetProperty("priority").GetInt32(),
            Summary: args.GetProperty("summary").GetString() ?? string.Empty);
    }
}
