using FieldTask.Api.Dtos;

namespace FieldTask.Api.Services;

public interface IAiSuggestionService
{
    Task<TaskSuggestionDto> SuggestTaskAsync(string complaintText, CancellationToken ct = default);
}
