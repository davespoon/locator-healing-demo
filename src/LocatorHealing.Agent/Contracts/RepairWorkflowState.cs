namespace LocatorHealing.Agent.Contracts;

public sealed class RepairWorkflowState
{
    public required LocatorRepairIncident Incident { get; init; }

    public int AttemptCount { get; set; }
    public string? ExistingPullRequestUrl { get; set; }
    public DateTimeOffset? CooldownUntilUtc { get; set; }
    public string? StopReason { get; set; }

    public List<CandidateLocator> Candidates { get; } = [];
}