using LocatorHealing.Agent.Contracts;
using LocatorHealing.Agent.Infrastructure;
using Microsoft.Agents.AI.Workflows;

namespace LocatorHealing.Agent.Workflow;

internal sealed partial class PageObjectPatchExecutor(RepoPathResolver repoPathResolver)
    : Executor("PageObjectPatch")
{
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

        var best = state.Candidates.MaxBy(c => c.Confidence);
        if (best is null)
        {
            state.StopReason = "No candidates available for patching.";
            return state;
        }

        if (state.Incident.PageObjectLineNumber is null)
        {
            state.StopReason = "Cannot patch page object: line number is unknown.";
            return state;
        }

        var pageObjectPath = repoPathResolver.ToAbsolutePath(state.Incident.RepoRelativePageObjectPath);
        if (pageObjectPath is null || !File.Exists(pageObjectPath))
        {
            state.StopReason = "Could not resolve page object file path for patching.";
            return state;
        }

        var original = await File.ReadAllTextAsync(pageObjectPath, cancellationToken);
        var patched = PageObjectPatcher.Patch(
            original,
            state.Incident.LocatorSelector!,
            state.Incident.LocatorStrategy!,
            best,
            state.Incident.PageObjectLineNumber.Value);

        if (patched == original)
        {
            state.StopReason = $"Broken selector '{state.Incident.LocatorSelector}' was not found in page object file.";
            return state;
        }

        await File.WriteAllTextAsync(pageObjectPath, patched, cancellationToken);

        state.AppliedPatch = new PageObjectPatch(
            PageObjectPath: pageObjectPath,
            OldSelector: state.Incident.LocatorSelector!,
            NewSelector: best.Value,
            Strategy: best.Strategy);

        return state;
    }
}