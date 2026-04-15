using LocatorHealing.Agent.Infrastructure;
using LocatorHealing.Agent.Policies;
using LocatorHealing.Agent.Executors;
using Microsoft.Agents.AI.Workflows;

namespace LocatorHealing.Agent.Application;

internal static class LocatorHealingWorkflow
{
    public static Workflow Create(string repoRoot, string outputDirectory)
    {
        var resultParser = new NUnitResultParser();
        var repoPathResolver = new RepoPathResolver(repoRoot);
        var failureParser = new SeleniumFailureParser(repoPathResolver);
        var diagnosticsWriter = new JsonFailureDiagnosticsWriter();
        var diagnosticsReader = new DiagnosticsArtifactReader();
        var loopGuardPolicy = new LoopGuardPolicy(TimeProvider.System);
        var openAiAgentFactory = new CandidateAgentFactory();

        var resultsParse = new ResultsParseExecutor(resultParser);
        var diagnosticsPreparation = new DiagnosticsPreparationExecutor(
            failureParser, diagnosticsWriter, outputDirectory);
        var failureIngest = new FailureIngestExecutor(diagnosticsReader);
        var loopGuard = new LoopGuardExecutor(loopGuardPolicy);
        var locatorFailureCheck = new LocatorFailureCheckExecutor();
        var stop = new StopExecutor();
        var healerAgent = new CandidateGenerationExecutor(openAiAgentFactory.Create());
        var pageObjectPatch = new PageObjectPatchExecutor(repoPathResolver);

        return new WorkflowBuilder(resultsParse)
            .AddEdge(resultsParse, diagnosticsPreparation)
            .AddEdge(diagnosticsPreparation, failureIngest)
            .AddEdge(failureIngest, loopGuard)
            .AddEdge(loopGuard, stop, condition: ShouldStop())
            .AddEdge(loopGuard, locatorFailureCheck, condition: ShouldContinue())
            .AddEdge(locatorFailureCheck, stop, condition: ShouldStop())
            .AddEdge(locatorFailureCheck, healerAgent, condition: ShouldContinue())
            .AddEdge(healerAgent, stop, condition: ShouldStop())
            .AddEdge(healerAgent, pageObjectPatch, condition: ShouldContinue())
            .WithOutputFrom(stop, pageObjectPatch)
            .Build();
    }

    private static bool HasStopReason(object? data) =>
        data is Contracts.RepairWorkflowState state && state.IsStopped;

    private static Func<object?, bool> ShouldStop() => HasStopReason;

    private static Func<object?, bool> ShouldContinue() => data => !HasStopReason(data);
}