using System.Globalization;
using System.Text;
using LocatorHealing.Agent.Contracts;

namespace LocatorHealing.Agent.Infrastructure;

internal sealed class LocatorHealingReportWriter
{
    public void Write(string reportFilePath, IReadOnlyList<RepairWorkflowState> states)
    {
        if (string.IsNullOrWhiteSpace(reportFilePath))
        {
            throw new ArgumentException("Report file path is required.", nameof(reportFilePath));
        }

        var resolvedPath = Path.GetFullPath(reportFilePath);
        var directory = Path.GetDirectoryName(resolvedPath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(resolvedPath, Build(states), Encoding.UTF8);
    }

    private static string Build(IReadOnlyList<RepairWorkflowState> states)
    {
        var builder = new StringBuilder();

        builder.AppendLine("# Locator healing report");
        builder.AppendLine();
        builder.AppendLine($"Generated at: {DateTimeOffset.UtcNow:O}");
        builder.AppendLine();

        if (states.Count == 0)
        {
            builder.AppendLine("No failed locator incidents were analyzed.");
            return builder.ToString();
        }

        var patchedCount = states.Count(state => state.AppliedPatch is not null);
        var stoppedCount = states.Count(state => state.IsStopped);

        builder.AppendLine($"Analyzed failures: {states.Count}");
        builder.AppendLine($"Applied patches: {patchedCount}");
        builder.AppendLine($"Stopped without patch: {stoppedCount}");
        builder.AppendLine();

        for (var i = 0; i < states.Count; i++)
        {
            AppendState(builder, states[i], i + 1);
        }

        return builder.ToString();
    }

    private static void AppendState(StringBuilder builder, RepairWorkflowState state, int index)
    {
        var incident = state.Incident;

        builder.AppendLine($"## {index}. {ValueOrUnknown(incident.TestName)}");
        builder.AppendLine();
        builder.AppendLine($"- Test full name: `{ValueOrUnknown(incident.TestFullName)}`");
        builder.AppendLine($"- Page object: `{ValueOrUnknown(incident.RepoRelativePageObjectPath)}`");
        builder.AppendLine($"- Page object line: `{ValueOrUnknown(incident.PageObjectLineNumber)}`");
        builder.AppendLine($"- Test file: `{ValueOrUnknown(incident.RepoRelativeTestPath)}`");
        builder.AppendLine($"- Test line: `{ValueOrUnknown(incident.TestLineNumber)}`");
        builder.AppendLine($"- Root cause: `{ValueOrUnknown(incident.RootCauseExceptionType)}`");
        builder.AppendLine($"- Old locator strategy: `{ValueOrUnknown(incident.LocatorStrategy)}`");
        builder.AppendLine($"- Old locator selector: `{ValueOrUnknown(incident.LocatorSelector)}`");
        builder.AppendLine($"- DOM snapshot: `{ValueOrUnknown(incident.RepoRelativeDomSnapshotPath)}`");
        builder.AppendLine();

        AppendPatch(builder, state.AppliedPatch, incident);
        AppendCandidates(builder, state.Candidates);

        if (!string.IsNullOrWhiteSpace(state.StopReason))
        {
            builder.AppendLine($"Stopped reason: {state.StopReason}");
            builder.AppendLine();
        }
    }

    private static void AppendPatch(
        StringBuilder builder,
        PageObjectPatch? patch,
        LocatorRepairIncident incident)
    {
        if (patch is null)
        {
            builder.AppendLine("Patch: not applied.");
            builder.AppendLine();
            return;
        }

        builder.AppendLine("Patch applied:");
        builder.AppendLine();
        builder.AppendLine($"- File: `{ValueOrUnknown(incident.RepoRelativePageObjectPath)}`");
        builder.AppendLine($"- Strategy: `{ValueOrUnknown(patch.Strategy)}`");
        builder.AppendLine($"- Old selector: `{ValueOrUnknown(patch.OldSelector)}`");
        builder.AppendLine($"- New selector: `{ValueOrUnknown(patch.NewSelector)}`");
        builder.AppendLine();
    }

    private static void AppendCandidates(StringBuilder builder, IReadOnlyList<CandidateLocator> candidates)
    {
        if (candidates.Count == 0)
        {
            builder.AppendLine("Candidates: none.");
            builder.AppendLine();
            return;
        }

        builder.AppendLine("Candidates:");
        builder.AppendLine();

        foreach (var candidate in candidates.OrderByDescending(candidate => candidate.Confidence))
        {
            builder.AppendLine($"- `{candidate.Strategy}` `{candidate.Value}` " +
                               $"confidence `{candidate.Confidence.ToString("0.00", CultureInfo.InvariantCulture)}`");

            if (!string.IsNullOrWhiteSpace(candidate.Reason))
            {
                builder.AppendLine($"  - Reason: {candidate.Reason}");
            }

            if (candidate.RiskFlags.Count > 0)
            {
                builder.AppendLine($"  - Risk flags: {string.Join(", ", candidate.RiskFlags)}");
            }
        }

        builder.AppendLine();
    }

    private static string ValueOrUnknown(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "unknown" : value;

    private static string ValueOrUnknown(int? value) =>
        value?.ToString(CultureInfo.InvariantCulture) ?? "unknown";
}