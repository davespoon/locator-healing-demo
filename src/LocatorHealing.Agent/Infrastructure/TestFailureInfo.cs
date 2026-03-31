namespace LocatorHealing.Agent.Infrastructure;

internal sealed record TestFailureInfo(
    string TestName,
    string TestFullName,
    string Message,
    string StackTrace,
    string? Output,
    string? DomSnapshotPath,
    string? ScreenshotPath);
