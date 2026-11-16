using System.ComponentModel.DataAnnotations;

namespace FieldTask.Api.Dtos;

public record SuggestTaskRequestDto(
    [Required, StringLength(2000, MinimumLength = 5)] string ComplaintText);

public record TaskSuggestionDto(string Title, int Priority, string Summary, IReadOnlyList<PossibleDuplicateDto> PossibleDuplicates);

public record PossibleDuplicateDto(int TaskId, string Title, double Similarity);

