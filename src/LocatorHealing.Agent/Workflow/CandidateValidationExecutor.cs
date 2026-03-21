using LocatorHealing.Agent.Contracts;
using LocatorHealing.Agent.Infrastructure;
using Microsoft.Agents.AI.Workflows;

namespace LocatorHealing.Agent.Workflow;

internal sealed class CandidateValidationExecutor(DomSnapshotValidator validator)
    : Executor<RepairWorkflowState, RepairWorkflowState>("CandidateValidation")
{
    public override async ValueTask<RepairWorkflowState> HandleAsync(
        RepairWorkflowState state,
        IWorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        if (state.IsStopped)
        {
            return state;
        }

        if (state.Candidates.Count == 0)
        {
            state.StopReason = "No candidate locators were available for deterministic validation.";
            return state;
        }

        if (!IsDomSnapshotAvailable(state))
        {
            state.StopReason = "DOM snapshot was not available for deterministic validation.";
            return state;
        }

        var domSnapshotHtml = await File.ReadAllTextAsync(state.ResolvedDomSnapshotPath!, cancellationToken);
        var validationResults = await validator.ValidateAsync(
            domSnapshotHtml,
            state.Incident.LocatorSelector,
            state.Candidates,
            cancellationToken);

        PopulateValidationResults(state, validationResults);
        SelectBestCandidate(state);

        return state;
    }

    private static void PopulateValidationResults(
        RepairWorkflowState state,
        IReadOnlyList<CandidateValidationResult> validationResults)
    {
        state.ValidationResults.Clear();

        foreach (var result in validationResults)
        {
            state.ValidationResults.Add(result);
        }
    }

    private static void SelectBestCandidate(RepairWorkflowState state)
    {
        state.SelectedCandidate = state.ValidationResults
            .Where(result => result.IsValid)
            .OrderByDescending(result => result.Candidate.Confidence)
            .ThenBy(result => StrategyRank(result.Candidate.Strategy))
            .FirstOrDefault();

        if (state.SelectedCandidate is null)
        {
            state.StopReason = "No generated locator candidates passed deterministic validation.";
        }
    }

    private static int StrategyRank(string strategy) =>
        strategy.Trim().ToLowerInvariant() switch
        {
            "css selector" => 0,
            "id" => 1,
            "name" => 2,
            "xpath" => 3,
            _ => 4
        };

    private static bool IsDomSnapshotAvailable(RepairWorkflowState state) =>
        !string.IsNullOrWhiteSpace(state.ResolvedDomSnapshotPath) && File.Exists(state.ResolvedDomSnapshotPath);
}