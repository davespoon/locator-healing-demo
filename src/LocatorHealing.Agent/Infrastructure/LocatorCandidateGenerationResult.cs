namespace LocatorHealing.Agent.Infrastructure;

internal sealed class LocatorCandidateGenerationResult
{
    public string SchemaVersion { get; set; } = "1.0";
    public string Decision { get; set; } = string.Empty; // candidates | insufficient_evidence
    public string Summary { get; set; } = string.Empty;
    public List<LocatorCandidateProposal> Candidates { get; set; } = [];
}

internal sealed class LocatorCandidateProposal
{
    public string Strategy { get; set; } = string.Empty; // css selector | xpath | id | name
    public string Value { get; set; } = string.Empty;
    public decimal Confidence { get; set; }
    public string Reason { get; set; } = string.Empty;
    public LocatorSemanticChecks SemanticChecks { get; set; } = new();
    public List<string> RiskFlags { get; set; } = [];
}

internal sealed class LocatorSemanticChecks
{
    public string? ExpectedTag { get; set; }
    public string? ExpectedText { get; set; }
    public string? ExpectedRole { get; set; }
    public string? ExpectedNearbyLabel { get; set; }
}
