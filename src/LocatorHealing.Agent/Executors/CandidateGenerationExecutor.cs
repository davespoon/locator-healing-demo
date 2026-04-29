using LocatorHealing.Agent.Contracts;
using LocatorHealing.Agent.Infrastructure;
using LocatorHealing.Agent.Prompts;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;

namespace LocatorHealing.Agent.Executors;

internal sealed partial class CandidateGenerationExecutor(AIAgent agent) : Executor("CandidateGeneration")
{
    private const int MaxCandidates = 3;

    [MessageHandler]
    private async ValueTask<RepairWorkflowState> HandleAsync(
        RepairWorkflowState state,
        IWorkflowContext context,
        CancellationToken cancellationToken = default)
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

        var domSnapshotHtml = await File.ReadAllTextAsync(state.ResolvedDomSnapshotPath!, cancellationToken);
        var result = await InvokeAgentAsync(state, domSnapshotHtml, cancellationToken);

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

        state.ApplyCandidates(result.Candidates.Select(MapToCandidate), MaxCandidates);

        if (state.Candidates.Count == 0)
        {
            state.StopReason = "Agent returned no usable locator candidates.";
        }

        return state;
    }

    private async Task<LocatorCandidateGenerationResult?> InvokeAgentAsync(
        RepairWorkflowState state,
        string domSnapshotHtml,
        CancellationToken cancellationToken)
    {
        var prompt = CandidatePromptBuilder.Build(state, domSnapshotHtml);
        var response =
            await agent.RunAsync<LocatorCandidateGenerationResult>(prompt, cancellationToken: cancellationToken);
        return response.Result;
    }

    private static CandidateLocator MapToCandidate(LocatorCandidateProposal proposal) =>
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

    private static bool IsDomSnapshotAvailable(RepairWorkflowState state) =>
        !string.IsNullOrWhiteSpace(state.ResolvedDomSnapshotPath) && File.Exists(state.ResolvedDomSnapshotPath);
}