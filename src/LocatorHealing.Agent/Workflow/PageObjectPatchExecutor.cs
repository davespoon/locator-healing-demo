using LocatorHealing.Agent.Contracts;
using LocatorHealing.Agent.Infrastructure;
using Microsoft.Agents.AI.Workflows;

namespace LocatorHealing.Agent.Workflow;

internal sealed partial class PageObjectPatchExecutor(RepoPathResolver repoPathResolver) : Executor("PageObjectPatch")
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

        var pageObjectPath = repoPathResolver.ToAbsolutePath(state.Incident.RepoRelativePageObjectPath);
        if (pageObjectPath is null || !File.Exists(pageObjectPath))
        {
            state.StopReason = "Could not resolve page object file path for patching.";
            return state;
        }

        var original = await File.ReadAllTextAsync(pageObjectPath, cancellationToken);
        var patched = PatchSource(
            original,
            state.Incident.LocatorSelector!,
            state.Incident.LocatorStrategy!,
            best,
            state.Incident.PageObjectLineNumber);

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

    private static string PatchSource(
        string source,
        string oldSelector,
        string oldStrategy,
        CandidateLocator candidate,
        int? pageObjectLineNumber)
    {
        var patched = source.Replace($"\"{oldSelector}\"", $"\"{candidate.Value}\"", StringComparison.Ordinal);

        if (!string.Equals(oldStrategy, candidate.Strategy, StringComparison.OrdinalIgnoreCase)
            && pageObjectLineNumber.HasValue
            && TryGetByMethodName(oldStrategy, out var oldMethod)
            && TryGetByMethodName(candidate.Strategy, out var newMethod))
        {
            patched = ReplaceByMethodAtLine(patched, oldMethod, newMethod, pageObjectLineNumber.Value);
        }

        return patched;
    }

    private static string ReplaceByMethodAtLine(string source, string oldMethod, string newMethod, int lineNumber)
    {
        var lines = source.Split('\n');
        var lineIndex = lineNumber - 1;

        if (lineIndex < 0 || lineIndex >= lines.Length)
        {
            return source;
        }

        lines[lineIndex] = lines[lineIndex].Replace(
            $"By.{oldMethod}(", $"By.{newMethod}(", StringComparison.Ordinal);

        return string.Join('\n', lines);
    }

    private static bool TryGetByMethodName(string strategy, out string methodName)
    {
        methodName = strategy.ToLowerInvariant() switch
        {
            "css selector" => "CssSelector",
            "id" => "Id",
            "name" => "Name",
            "xpath" => "XPath",
            "class name" => "ClassName",
            "tag name" => "TagName",
            "link text" => "LinkText",
            "partial link text" => "PartialLinkText",
            _ => string.Empty
        };

        return methodName.Length > 0;
    }
}
