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
    string? ExceptionType,
    LocatorHint? LocatorHint,
    SourceLocation? PageObjectLocation,
    SourceLocation? TestLocation);

public sealed record LocatorHint(
    string? Strategy,
    string? Selector);

public sealed record SourceLocation(
    string FilePath,
    int LineNumber);