using LocatorHealing.Agent.Contracts;
using LocatorHealing.Agent.Policies;
using Microsoft.Agents.AI.Workflows;

namespace LocatorHealing.Agent.Workflow;

internal sealed partial class LoopGuardExecutor(LoopGuardPolicy loopGuardPolicy) : Executor("LoopGuard")
{
    [MessageHandler]
    private ValueTask<RepairWorkflowState> HandleAsync(RepairWorkflowState state, IWorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        if (!loopGuardPolicy.CanProceed(state, out var reason))
        {
            state.StopReason = reason;
        }

        return ValueTask.FromResult(state);
    }
}