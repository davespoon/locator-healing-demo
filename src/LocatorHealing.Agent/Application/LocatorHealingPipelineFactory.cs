using LocatorHealing.Agent.Infrastructure;

namespace LocatorHealing.Agent.Application;

internal sealed class LocatorHealingPipelineFactory
{
    private readonly NUnitResultParser _resultParser = new();
    private readonly JsonFailureDiagnosticsWriter _diagnosticsWriter = new();
    private readonly LocatorHealingReportWriter _reportWriter = new();

    public LocatorHealingPipeline Create(string repoRoot)
    {
        var repoPathResolver = new RepoPathResolver(repoRoot);
        var failureParser = new SeleniumFailureParser(repoPathResolver);
        var workflow = LocatorHealingWorkflow.Create(repoPathResolver);
        var failureAnalyzer = new FailureAnalyzer(workflow);

        return new LocatorHealingPipeline(
            _resultParser, _diagnosticsWriter, _reportWriter, failureParser, failureAnalyzer);
    }
}
