using FieldTask.Api.Dtos;
using FieldTask.Api.Services;

namespace FieldTask.Tests.TestSupport;

// Instant, free stand-in for the Gemini-backed service, for endpoint tests that care about the
// HTTP pipeline (auth, rate limiting) rather than the AI itself.
public class FakeAiSuggestionService : IAiSuggestionService
{
    public Task<TaskSuggestionDto> SuggestTaskAsync(string complaintText, CancellationToken ct = default) =>
        Task.FromResult(new TaskSuggestionDto("Fake draft", 3, "Fake summary", []));
}
