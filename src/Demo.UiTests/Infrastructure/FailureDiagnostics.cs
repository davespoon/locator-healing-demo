namespace Demo.UiTests.Infrastructure;

public sealed record FailureDiagnostics(
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
    LocatorHint? LocatorHint,
    SourceLocation? PageObjectLocation,
    SourceLocation? TestLocation,
    string? RepoRelativePageObjectPath,
    string? RepoRelativeTestPath,
    string? DomSnapshotPath);

public sealed record LocatorHint(
    string? Strategy,
    string? Selector);

public sealed record SourceLocation(
    string FilePath,
    int LineNumber);