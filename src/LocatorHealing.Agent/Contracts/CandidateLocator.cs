namespace LocatorHealing.Agent.Contracts;

public sealed record CandidateLocator(
    string Strategy,
    string Value,
    string Reason,
    decimal Confidence);