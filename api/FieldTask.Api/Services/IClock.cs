namespace FieldTask.Api.Services;

public interface IClock
{
    DateTime UtcNow { get; }
}
