using FieldTask.Api.Services;

namespace FieldTask.Tests;

// Boundary value analysis: most validation bugs sit exactly at the edges (< vs <=),
// so every limit is tested just inside AND just outside.
public class TaskDraftValidatorTests
{
    private const string ValidTitle = "Fix leaking pump";
    private const string ValidSummary = "Pump in building B is leaking onto the floor.";

    [Fact]
    public void ValidDraft_HasNoErrors()
    {
        var errors = TaskDraftValidator.Validate(ValidTitle, 3, ValidSummary);

        Assert.Empty(errors);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    public void PriorityAtTheEdges_IsAccepted(int priority)
    {
        var errors = TaskDraftValidator.Validate(ValidTitle, priority, ValidSummary);

        Assert.Empty(errors);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(-1)]
    [InlineData(99)]
    public void PriorityOutsideOneToFive_IsRejected(int priority)
    {
        var errors = TaskDraftValidator.Validate(ValidTitle, priority, ValidSummary);

        var error = Assert.Single(errors);
        Assert.Contains(priority.ToString(), error); // the message names the bad value
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void MissingOrBlankTitle_IsRejected(string? title)
    {
        var errors = TaskDraftValidator.Validate(title, 3, ValidSummary);

        Assert.Single(errors);
    }

    [Fact]
    public void TitleAtMaxLength_IsAccepted()
    {
        var title = new string('a', TaskDraftValidator.MaxTitleLength);

        Assert.Empty(TaskDraftValidator.Validate(title, 3, ValidSummary));
    }

    [Fact]
    public void TitleOneOverMaxLength_IsRejected()
    {
        var title = new string('a', TaskDraftValidator.MaxTitleLength + 1);

        Assert.Single(TaskDraftValidator.Validate(title, 3, ValidSummary));
    }

    [Fact]
    public void SummaryAtMaxLength_IsAccepted()
    {
        var summary = new string('s', TaskDraftValidator.MaxSummaryLength);

        Assert.Empty(TaskDraftValidator.Validate(ValidTitle, 3, summary));
    }

    [Fact]
    public void SummaryOneOverMaxLength_IsRejected()
    {
        var summary = new string('s', TaskDraftValidator.MaxSummaryLength + 1);

        Assert.Single(TaskDraftValidator.Validate(ValidTitle, 3, summary));
    }

    [Fact]
    public void NullSummary_IsAllowed()
    {
        Assert.Empty(TaskDraftValidator.Validate(ValidTitle, 3, null));
    }

    [Fact]
    public void EverythingWrong_ReportsEveryProblem()
    {
        var errors = TaskDraftValidator.Validate("", 0, new string('s', TaskDraftValidator.MaxSummaryLength + 1));

        Assert.Equal(3, errors.Count);
    }
}
