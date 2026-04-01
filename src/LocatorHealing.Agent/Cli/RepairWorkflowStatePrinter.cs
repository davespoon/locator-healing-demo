using LocatorHealing.Agent.Contracts;

namespace LocatorHealing.Agent.Cli;

internal static class RepairWorkflowStatePrinter
{
    public static void Print(RepairWorkflowState state, TextWriter? writer = null)
    {
        var output = writer ?? Console.Out;

        PrintIncident(output, state);
        PrintCandidates(output, state.Candidates);
        PrintPatch(output, state.AppliedPatch);
        PrintOutcome(output, state.StopReason);
    }

    private static void PrintIncident(TextWriter output, RepairWorkflowState state)
    {
        output.WriteLine("Repair workflow state");
        output.WriteLine($"  Test: {state.Incident.TestName}");
        output.WriteLine($"  Page object: {state.Incident.RepoRelativePageObjectPath}");
        output.WriteLine($"  Selector: {state.Incident.LocatorSelector}");
        output.WriteLine($"  Root cause: {state.Incident.RootCauseExceptionType}");
        output.WriteLine($"  DOM snapshot: {state.ResolvedDomSnapshotPath}");
    }

    private static void PrintCandidates(TextWriter output, List<CandidateLocator> candidates)
    {
        if (candidates.Count == 0) return;

        output.WriteLine($"  Generated candidates: {candidates.Count}");
        foreach (var candidate in candidates)
        {
            output.WriteLine(
                $"    - {candidate.Strategy}: {candidate.Value} (confidence {candidate.Confidence:0.00})");
            output.WriteLine($"      {candidate.Reason}");
        }
    }

    private static void PrintPatch(TextWriter output, PageObjectPatch? patch)
    {
        if (patch is null) return;

        output.WriteLine($"  Patched: {patch.OldSelector} → {patch.NewSelector}");
        output.WriteLine($"    Strategy: {patch.Strategy}");
        output.WriteLine($"    File: {patch.PageObjectPath}");
    }

    private static void PrintOutcome(TextWriter output, string? stopReason)
    {
        if (!string.IsNullOrWhiteSpace(stopReason))
        {
            output.WriteLine($"  Stopped: {stopReason}");
        }
    }
}