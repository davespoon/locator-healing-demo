using LocatorHealing.Agent.Infrastructure;
using LocatorHealing.Agent.Policies;
using LocatorHealing.Agent.Workflow;
using Microsoft.Agents.AI.Workflows;
using AgentWorkflow = Microsoft.Agents.AI.Workflows.Workflow;

namespace LocatorHealing.Agent.Application;

internal static class LocatorHealingWorkflow
{
    public static AgentWorkflow Create()
    {
        var diagnosticsReader = new DiagnosticsArtifactReader();
        var loopGuardPolicy = new LoopGuardPolicy(TimeProvider.System);
        var openAiAgentFactory = new CandidateAgentFactory();

        var failureIngest = new FailureIngestExecutor(diagnosticsReader);
        var loopGuard = new LoopGuardExecutor(loopGuardPolicy);
        var locatorFailureCheck = new LocatorFailureCheckExecutor();
        var stop = new StopExecutor();
        var candidateGeneration = new CandidateGenerationExecutor(openAiAgentFactory.Create());
        var pageObjectPatch = new PageObjectPatchExecutor(new RepoPathResolver());

        return new WorkflowBuilder(failureIngest)
            .AddEdge(failureIngest, loopGuard)
            .AddEdge(loopGuard, stop, condition: ShouldStop())
            .AddEdge(loopGuard, locatorFailureCheck, condition: ShouldContinue())
            .AddEdge(locatorFailureCheck, stop, condition: ShouldStop())
            .AddEdge(locatorFailureCheck, candidateGeneration, condition: ShouldContinue())
            .AddEdge(candidateGeneration, stop, condition: ShouldStop())
            .AddEdge(candidateGeneration, pageObjectPatch, condition: ShouldContinue())
            .WithOutputFrom(stop, pageObjectPatch)
            .Build();
    }

    private static bool HasStopReason(object? data) =>
        data is Contracts.RepairWorkflowState state && state.IsStopped;

    private static Func<object?, bool> ShouldStop() => HasStopReason;

    private static Func<object?, bool> ShouldContinue() => data => !HasStopReason(data);
}