namespace LocatorHealing.Agent.Contracts;

public sealed record CandidateLocator(
    string Strategy,
    string Value,
    string Reason,
    decimal Confidence,
    CandidateSemanticChecks SemanticChecks,
    IReadOnlyList<string> RiskFlags);

public sealed record CandidateSemanticChecks(
    string? ExpectedTag,
    string? ExpectedText,
    string? ExpectedRole,
    string? ExpectedNearbyLabel);