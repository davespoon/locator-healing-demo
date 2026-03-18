using LocatorHealing.Agent.Contracts;
using Microsoft.Agents.AI.Workflows;

namespace LocatorHealing.Agent.Workflow;

internal sealed class StopExecutor() : Executor<RepairWorkflowState, RepairWorkflowState>("Stop")
{
    public override ValueTask<RepairWorkflowState> HandleAsync(
        RepairWorkflowState state,
        IWorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(state);
    }
}