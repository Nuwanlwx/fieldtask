using FieldTask.Api.Services;

namespace FieldTask.Tests.TestSupport;

// IClock exists specifically so tests can hand TaskService a fixed point in time
// instead of the real DateTime.UtcNow.
public class FakeClock(DateTime utcNow) : IClock
{
    public DateTime UtcNow { get; } = utcNow;
}
