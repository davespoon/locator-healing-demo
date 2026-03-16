namespace LocatorHealing.Agent.Contracts;

public sealed record FailureDiagnosticsArtifact(
    string SchemaVersion,
    DateTimeOffset GeneratedAtUtc,
    string TestName,
    string TestFullName,
    string Url,
    string OutcomeStatus,
    string OutcomeLabel,
    string Message,
    string StackTrace,
    string? OuterExceptionType,
    string? RootCauseExceptionType,
    LocatorHintArtifact? LocatorHint,
    SourceLocationArtifact? PageObjectLocation,
    SourceLocationArtifact? TestLocation,
    string? RepoRelativePageObjectPath,
    string? RepoRelativeTestPath,
    string? DomSnapshotPath,
    string? ScreenshotPath);

public sealed record LocatorHintArtifact(
    string? Strategy,
    string? Selector);

public sealed record SourceLocationArtifact(
    string FilePath,
    int LineNumber);