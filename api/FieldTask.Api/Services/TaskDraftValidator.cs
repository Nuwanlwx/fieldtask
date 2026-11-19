namespace FieldTask.Api.Services;

// Checks an AI-generated task draft before anything trusts it.
// Static + no dependencies = trivially unit-testable, no HTTP or Gemini needed.
public static class TaskDraftValidator
{
    public const int MaxTitleLength = 200;
    public const int MaxSummaryLength = 1000;
    public const int MinPriority = 1;
    public const int MaxPriority = 5;

    public static List<string> Validate(string? title, int priority, string? summary)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(title))
            errors.Add("Title is empty.");
        else if (title.Length > MaxTitleLength)
            errors.Add($"Title is {title.Length} characters; the maximum is {MaxTitleLength}.");

        if (priority < MinPriority || priority > MaxPriority)
            errors.Add($"Priority {priority} is outside {MinPriority}-{MaxPriority}.");

        if (summary?.Length > MaxSummaryLength)
            errors.Add($"Summary is {summary.Length} characters; the maximum is {MaxSummaryLength}.");

        return errors;
    }
}
