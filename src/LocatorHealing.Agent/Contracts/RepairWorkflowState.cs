namespace LocatorHealing.Agent.Contracts;

public sealed class RepairWorkflowState
{
    public required string DiagnosticsFilePath { get; init; }
    public required LocatorRepairIncident Incident { get; init; }

    public string? ResolvedDomSnapshotPath { get; init; }

    public int AttemptCount { get; set; }
    public string? ExistingPullRequestUrl { get; set; }
    public DateTimeOffset? CooldownUntilUtc { get; set; }
    public string? StopReason { get; set; }

    public bool IsStopped => !string.IsNullOrWhiteSpace(StopReason);

    public List<CandidateLocator> Candidates { get; } = [];
    public List<CandidateValidationResult> ValidationResults { get; } = [];
    public CandidateValidationResult? SelectedCandidate { get; set; }
}