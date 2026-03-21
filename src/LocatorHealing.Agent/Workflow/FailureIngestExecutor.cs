using LocatorHealing.Agent.Contracts;
using LocatorHealing.Agent.Infrastructure;
using Microsoft.Agents.AI.Workflows;

namespace LocatorHealing.Agent.Workflow;

internal sealed partial class FailureIngestExecutor(DiagnosticsArtifactReader reader) : Executor("FailureIngest")
{
    [MessageHandler]
    private ValueTask<RepairWorkflowState> HandleAsync(string diagnosticsFilePath, IWorkflowContext context)
    {
        RepairWorkflowState state = reader.Read(diagnosticsFilePath);
        return ValueTask.FromResult(state);
    }
}