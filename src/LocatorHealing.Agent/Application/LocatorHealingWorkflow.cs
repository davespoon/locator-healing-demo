using LocatorHealing.Agent.Contracts;
using Microsoft.Agents.AI.Workflows;

namespace LocatorHealing.Agent.Application;

internal static class LocatorHealingWorkflow
{
    public static Workflow Create(string repoRoot, string outputDirectory)
    {
        var e = new ExecutorFactory(repoRoot, outputDirectory);

        return new WorkflowBuilder(e.ResultsParse)
            .AddEdge(e.ResultsParse, e.DiagnosticsPreparation)
            .AddEdge(e.DiagnosticsPreparation, e.FailureIngest)
            .AddEdge(e.FailureIngest, e.LoopGuard)
            .AddEdge(e.LoopGuard, e.Stop, condition: RepairWorkflowState.Stopped)
            .AddEdge(e.LoopGuard, e.LocatorFailureCheck, condition: RepairWorkflowState.NotStopped)
            .AddEdge(e.LocatorFailureCheck, e.Stop, condition: RepairWorkflowState.Stopped)
            .AddEdge(e.LocatorFailureCheck, e.CandidateGeneration, condition: RepairWorkflowState.NotStopped)
            .AddEdge(e.CandidateGeneration, e.Stop, condition: RepairWorkflowState.Stopped)
            .AddEdge(e.CandidateGeneration, e.PageObjectPatch, condition: RepairWorkflowState.NotStopped)
            .WithOutputFrom(e.Stop, e.PageObjectPatch)
            .Build();
    }
}