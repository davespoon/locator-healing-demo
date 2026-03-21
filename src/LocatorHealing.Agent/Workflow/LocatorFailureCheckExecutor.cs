using LocatorHealing.Agent.Contracts;
using Microsoft.Agents.AI.Workflows;

namespace LocatorHealing.Agent.Workflow;

internal sealed partial class LocatorFailureCheckExecutor() : Executor("LocatorFailureCheck")
{
    [MessageHandler]
    private ValueTask<RepairWorkflowState> HandleAsync(RepairWorkflowState state, IWorkflowContext context)
    {
        if (state.IsStopped)
        {
            return ValueTask.FromResult(state);
        }

        if (!string.Equals(
                state.Incident.RootCauseExceptionType,
                "OpenQA.Selenium.NoSuchElementException",
                StringComparison.OrdinalIgnoreCase))
        {
            state.StopReason = "Failure is not a supported locator-repair case.";
            return ValueTask.FromResult(state);
        }

        if (string.IsNullOrWhiteSpace(state.Incident.LocatorSelector))
        {
            state.StopReason = "Failure diagnostics did not include a broken selector.";
            return ValueTask.FromResult(state);
        }

        if (string.IsNullOrWhiteSpace(state.Incident.LocatorStrategy))
        {
            state.StopReason = "Failure diagnostics did not include a locator strategy.";
            return ValueTask.FromResult(state);
        }

        if (string.IsNullOrWhiteSpace(state.Incident.RepoRelativePageObjectPath))
        {
            state.StopReason = "Failure diagnostics did not include a page object path.";
            return ValueTask.FromResult(state);
        }

        return ValueTask.FromResult(state);
    }
}