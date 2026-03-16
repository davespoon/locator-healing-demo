using LocatorHealing.Agent.Contracts;
using LocatorHealing.Agent.Infrastructure;
using Microsoft.Agents.AI.Workflows;

namespace LocatorHealing.Agent.Workflow;

internal sealed class FailureIngestExecutor(DiagnosticsArtifactReader reader)
    : Executor<string, RepairWorkflowState>("FailureIngest")
{
    public override ValueTask<RepairWorkflowState> HandleAsync(
        string diagnosticsFilePath,
        IWorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        var state = reader.Read(diagnosticsFilePath);
        return ValueTask.FromResult(state);
    }
}