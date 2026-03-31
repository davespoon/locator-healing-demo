using LocatorHealing.Agent.Contracts;

namespace LocatorHealing.Agent.Workflow;

internal static class CandidatePromptBuilder
{
    public static string Build(RepairWorkflowState state, string domSnapshotHtml)
    {
        var incident = state.Incident;

        return $"""
                Confirmed locator failure.
                Use only the provided failure diagnostics and DOM snapshot.
                Do not assume access to repository source files or page object code beyond the provided diagnostics.

                Failure diagnostics:
                - TestName: {incident.TestName}
                - TestFullName: {incident.TestFullName}
                - Url: {incident.Url}
                - OuterExceptionType: {incident.OuterExceptionType}
                - RootCauseExceptionType: {incident.RootCauseExceptionType}
                - LocatorStrategy: {incident.LocatorStrategy}
                - BrokenSelector: {incident.LocatorSelector}
                - PageObjectPath: {incident.RepoRelativePageObjectPath}
                - PageObjectLineNumber: {incident.PageObjectLineNumber}
                - TestPath: {incident.RepoRelativeTestPath}
                - TestLineNumber: {incident.TestLineNumber}

                Return structured output only.

                DOM snapshot:
                {domSnapshotHtml}
                """;
    }
}