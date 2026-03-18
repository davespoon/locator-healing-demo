using System.Text;
using LocatorHealing.Agent.Contracts;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;

namespace LocatorHealing.Agent.Workflow;

internal sealed class CandidateGenerationExecutor(AIAgent agent)
    : Executor<RepairWorkflowState, RepairWorkflowState>("CandidateGeneration")
{
    private readonly AIAgent _agent = agent;

    public override async ValueTask<RepairWorkflowState> HandleAsync(
        RepairWorkflowState state,
        IWorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(state.StopReason))
        {
            return state;
        }

        if (string.IsNullOrWhiteSpace(state.ResolvedDomSnapshotPath) || !File.Exists(state.ResolvedDomSnapshotPath))
        {
            state.StopReason = "DOM snapshot was not available for candidate generation.";
            return state;
        }

        var domSnapshotHtml = await File.ReadAllTextAsync(state.ResolvedDomSnapshotPath, cancellationToken);
        var prompt = BuildPrompt(state, domSnapshotHtml);

        AgentResponse<LocatorCandidateGenerationResult> response =
            await _agent.RunAsync<LocatorCandidateGenerationResult>(prompt, cancellationToken: cancellationToken);

        var result = response.Result;

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

        state.Candidates.Clear();

        foreach (var candidate in result.Candidates.Take(3))
        {
            state.Candidates.Add(new CandidateLocator(
                Strategy: candidate.Strategy,
                Value: candidate.Value,
                Reason: candidate.Reason,
                Confidence: candidate.Confidence,
                SemanticChecks: new CandidateSemanticChecks(
                    ExpectedTag: candidate.SemanticChecks.ExpectedTag,
                    ExpectedText: candidate.SemanticChecks.ExpectedText,
                    ExpectedRole: candidate.SemanticChecks.ExpectedRole,
                    ExpectedNearbyLabel: candidate.SemanticChecks.ExpectedNearbyLabel),
                RiskFlags: candidate.RiskFlags));
        }

        if (state.Candidates.Count == 0)
        {
            state.StopReason = "Agent returned no usable locator candidates.";
        }

        return state;
    }

    private static string BuildPrompt(
        RepairWorkflowState state,
        string domSnapshotHtml)
    {
        var sb = new StringBuilder();

        sb.AppendLine("Confirmed locator failure.");
        sb.AppendLine("Use only the provided failure diagnostics and DOM snapshot.");
        sb.AppendLine(
            "Do not assume access to repository source files or page object code beyond the provided diagnostics.");
        sb.AppendLine();
        sb.AppendLine("Failure diagnostics:");
        sb.AppendLine($"- TestName: {state.Incident.TestName}");
        sb.AppendLine($"- TestFullName: {state.Incident.TestFullName}");
        sb.AppendLine($"- Url: {state.Incident.Url}");
        sb.AppendLine($"- OuterExceptionType: {state.Incident.OuterExceptionType}");
        sb.AppendLine($"- RootCauseExceptionType: {state.Incident.RootCauseExceptionType}");
        sb.AppendLine($"- LocatorStrategy: {state.Incident.LocatorStrategy}");
        sb.AppendLine($"- BrokenSelector: {state.Incident.LocatorSelector}");
        sb.AppendLine($"- PageObjectPath: {state.Incident.RepoRelativePageObjectPath}");
        sb.AppendLine($"- PageObjectLineNumber: {state.Incident.PageObjectLineNumber}");
        sb.AppendLine($"- TestPath: {state.Incident.RepoRelativeTestPath}");
        sb.AppendLine($"- TestLineNumber: {state.Incident.TestLineNumber}");
        sb.AppendLine();
        sb.AppendLine("Return structured output only.");
        sb.AppendLine();
        sb.AppendLine("DOM snapshot:");
        sb.AppendLine(domSnapshotHtml);

        return sb.ToString();
    }
}