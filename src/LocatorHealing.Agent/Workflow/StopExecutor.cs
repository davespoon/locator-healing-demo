using LocatorHealing.Agent.Contracts;
using Microsoft.Agents.AI.Workflows;

namespace LocatorHealing.Agent.Workflow;

internal sealed partial class StopExecutor() : Executor("Stop")
{
    [MessageHandler]
    private ValueTask<RepairWorkflowState> HandleAsync(RepairWorkflowState state, IWorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(state);
    }
}