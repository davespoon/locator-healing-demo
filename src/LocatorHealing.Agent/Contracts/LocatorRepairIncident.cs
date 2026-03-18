namespace LocatorHealing.Agent.Contracts;

public sealed record LocatorRepairIncident(
    string TestName,
    string TestFullName,
    string Url,
    string? OuterExceptionType,
    string? RootCauseExceptionType,
    string? LocatorStrategy,
    string? LocatorSelector,
    string? RepoRelativePageObjectPath,
    int? PageObjectLineNumber,
    string? RepoRelativeTestPath,
    int? TestLineNumber,
    string? RepoRelativeDomSnapshotPath);