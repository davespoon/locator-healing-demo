using LocatorHealing.Agent.Contracts;
using LocatorHealing.Agent.Infrastructure;
using Microsoft.Agents.AI.Workflows;

namespace LocatorHealing.Agent.Workflow;

internal sealed class CandidateValidationExecutor(DomSnapshotValidator validator)
    : Executor<RepairWorkflowState, RepairWorkflowState>("CandidateValidation")
{
    private readonly DomSnapshotValidator _validator = validator;

    public override async ValueTask<RepairWorkflowState> HandleAsync(
        RepairWorkflowState state,
        IWorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(state.StopReason))
        {
            return state;
        }

        if (state.Candidates.Count == 0)
        {
            state.StopReason = "No candidate locators were available for deterministic validation.";
            return state;
        }

        if (string.IsNullOrWhiteSpace(state.ResolvedDomSnapshotPath) || !File.Exists(state.ResolvedDomSnapshotPath))
        {
            state.StopReason = "DOM snapshot was not available for deterministic validation.";
            return state;
        }

        var domSnapshotHtml = await File.ReadAllTextAsync(state.ResolvedDomSnapshotPath, cancellationToken);
        var validationResults = await _validator.ValidateAsync(
            domSnapshotHtml,
            state.Incident.LocatorSelector,
            state.Candidates,
            cancellationToken);

        state.ValidationResults.Clear();
        foreach (var result in validationResults)
        {
            state.ValidationResults.Add(result);
        }

        state.SelectedCandidate = state.ValidationResults
            .Where(result => result.IsValid)
            .OrderByDescending(result => result.Candidate.Confidence)
            .ThenBy(result => StrategyRank(result.Candidate.Strategy))
            .FirstOrDefault();

        if (state.SelectedCandidate is null)
        {
            state.StopReason = "No generated locator candidates passed deterministic validation.";
        }

        return state;
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
}