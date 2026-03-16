using LocatorHealing.Agent.Contracts;
using LocatorHealing.Agent.Policies;
using Microsoft.Agents.AI.Workflows;

namespace LocatorHealing.Agent.Workflow;

internal sealed class LoopGuardExecutor(LoopGuardPolicy loopGuardPolicy)
    : Executor<RepairWorkflowState, RepairWorkflowState>("LoopGuard")
{
    public override ValueTask<RepairWorkflowState> HandleAsync(
        RepairWorkflowState state,
        IWorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        if (!loopGuardPolicy.CanProceed(state, DateTimeOffset.UtcNow, out var reason))
        {
            state.StopReason = reason;
        }

        return ValueTask.FromResult(state);
    }
}