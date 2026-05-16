public sealed class ScheduleValidationResult
{
    public List<string> Errors { get; } = new();

    public bool IsValid => Errors.Count == 0;
}
