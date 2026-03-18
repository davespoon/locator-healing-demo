namespace LocatorHealing.Agent.Contracts;

public sealed record CandidateValidationResult(
    CandidateLocator Candidate,
    bool IsValid,
    int MatchCount,
    string Summary,
    string? MatchedTag,
    string? MatchedText,
    IReadOnlyList<string> Issues);