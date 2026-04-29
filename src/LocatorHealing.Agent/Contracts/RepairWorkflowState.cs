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
    public PageObjectPatch? AppliedPatch { get; set; }

    public bool IsStopped => !string.IsNullOrWhiteSpace(StopReason);

    public List<CandidateLocator> Candidates { get; } = [];

    public void ApplyCandidates(IEnumerable<CandidateLocator> candidates, int maxCount)
    {
        Candidates.Clear();

        foreach (var candidate in candidates.Take(maxCount))
        {
            Candidates.Add(candidate);
        }
    }

    public static readonly Func<object?, bool> Stopped =
        data => data is RepairWorkflowState state && state.IsStopped;

    public static readonly Func<object?, bool> NotStopped =
        data => data is not RepairWorkflowState state || !state.IsStopped;
}