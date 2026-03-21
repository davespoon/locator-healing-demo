using LocatorHealing.Agent.Contracts;
using LocatorHealing.Agent.Infrastructure;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;

namespace LocatorHealing.Agent.Workflow;

internal sealed partial class CandidateGenerationExecutor(AIAgent agent) : Executor("CandidateGeneration")
{
    private const int MaxCandidates = 3;

    [MessageHandler]
    private async ValueTask<RepairWorkflowState> HandleAsync(RepairWorkflowState state, IWorkflowContext context)
    {
        if (state.IsStopped)
        {
            return state;
        }

        if (!IsDomSnapshotAvailable(state))
        {
            state.StopReason = "DOM snapshot was not available for candidate generation.";
            return state;
        }

        var domSnapshotHtml = await File.ReadAllTextAsync(state.ResolvedDomSnapshotPath!);
        var result = await InvokeAgentAsync(state, domSnapshotHtml);

        if (result is null)
        {
            state.StopReason = "Agent returned no structured candidate result.";
            return state;
        }

        if (string.Equals(result.Decision, "insufficient_evidence", StringComparison.OrdinalIgnoreCase))
        {
            state.StopReason = "Agent reported insufficient evidence for safe locator repair.";
            return state;
        }

        PopulateCandidates(state, result);

        if (state.Candidates.Count == 0)
        {
            state.StopReason = "Agent returned no usable locator candidates.";
        }

        return state;
    }

    private async Task<LocatorCandidateGenerationResult?> InvokeAgentAsync(
        RepairWorkflowState state,
        string domSnapshotHtml)
    {
        var prompt = BuildPrompt(state, domSnapshotHtml);
        var response = await agent.RunAsync<LocatorCandidateGenerationResult>(prompt);
        return response.Result;
    }

    private static void PopulateCandidates(RepairWorkflowState state, LocatorCandidateGenerationResult result)
    {
        state.Candidates.Clear();

        foreach (var proposal in result.Candidates.Take(MaxCandidates))
        {
            state.Candidates.Add(MapToCandidateLocator(proposal));
        }
    }

    private static CandidateLocator MapToCandidateLocator(LocatorCandidateProposal proposal) =>
        new(
            Strategy: proposal.Strategy,
            Value: proposal.Value,
            Reason: proposal.Reason,
            Confidence: proposal.Confidence,
            SemanticChecks: new CandidateSemanticChecks(
                ExpectedTag: proposal.SemanticChecks.ExpectedTag,
                ExpectedText: proposal.SemanticChecks.ExpectedText,
                ExpectedRole: proposal.SemanticChecks.ExpectedRole,
                ExpectedNearbyLabel: proposal.SemanticChecks.ExpectedNearbyLabel),
            RiskFlags: proposal.RiskFlags);

    private static string BuildPrompt(RepairWorkflowState state, string domSnapshotHtml)
    {
        var incident = state.Incident;

        return $"""
                Confirmed locator failure.
                Use only the provided failure diagnostics and DOM snapshot.
                Do not assume access to repository source files or page object code beyond the provided diagnostics.

                Failure diagnostics:
                - TestName: {incident.TestName}
                - TestFullName: {incident.TestFullName}
                - Url: {incident.Url}
                - OuterExceptionType: {incident.OuterExceptionType}
                - RootCauseExceptionType: {incident.RootCauseExceptionType}
                - LocatorStrategy: {incident.LocatorStrategy}
                - BrokenSelector: {incident.LocatorSelector}
                - PageObjectPath: {incident.RepoRelativePageObjectPath}
                - PageObjectLineNumber: {incident.PageObjectLineNumber}
                - TestPath: {incident.RepoRelativeTestPath}
                - TestLineNumber: {incident.TestLineNumber}

                Return structured output only.

                DOM snapshot:
                {domSnapshotHtml}
                """;
    }

    private static bool IsDomSnapshotAvailable(RepairWorkflowState state) =>
        !string.IsNullOrWhiteSpace(state.ResolvedDomSnapshotPath) && File.Exists(state.ResolvedDomSnapshotPath);
}