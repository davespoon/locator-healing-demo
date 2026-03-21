using LocatorHealing.Agent.Contracts;
using LocatorHealing.Agent.Infrastructure;
using LocatorHealing.Agent.Policies;
using LocatorHealing.Agent.Workflow;
using Microsoft.Agents.AI.Workflows;
using AgentWorkflow = Microsoft.Agents.AI.Workflows.Workflow;

namespace LocatorHealing.Agent.Cli;

public sealed class AnalyzeFailureCommandHandler
{
    public async Task<int> InvokeAsync(FileInfo diagnosticsFile)
    {
        try
        {
            var workflow = BuildWorkflow();
            return await ExecuteWorkflowAsync(workflow, diagnosticsFile.FullName);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Unhandled error:");
            Console.Error.WriteLine(ex);
            return 1;
        }
    }

    private static AgentWorkflow BuildWorkflow()
    {
        var diagnosticsReader = new DiagnosticsArtifactReader();
        var loopGuardPolicy = new LoopGuardPolicy();
        var openAiAgentFactory = new OpenAiCandidateAgentFactory();
        var domSnapshotValidator = new DomSnapshotValidator();

        var failureIngest = new FailureIngestExecutor(diagnosticsReader);
        var loopGuard = new LoopGuardExecutor(loopGuardPolicy);
        var locatorFailureCheck = new LocatorFailureCheckExecutor();
        var stop = new StopExecutor();
        var candidateGeneration = new CandidateGenerationExecutor(openAiAgentFactory.Create());
        var candidateValidation = new CandidateValidationExecutor(domSnapshotValidator);

        return new WorkflowBuilder(failureIngest)
            .AddEdge(failureIngest, loopGuard)
            .AddEdge(loopGuard, stop, condition: ShouldStop())
            .AddEdge(loopGuard, locatorFailureCheck, condition: ShouldContinue())
            .AddEdge(locatorFailureCheck, stop, condition: ShouldStop())
            .AddEdge(locatorFailureCheck, candidateGeneration, condition: ShouldContinue())
            .AddEdge(candidateGeneration, candidateValidation)
            .WithOutputFrom(stop, candidateValidation)
            .Build();
    }

    private static async Task<int> ExecuteWorkflowAsync(AgentWorkflow workflow, string diagnosticsFilePath)
    {
        await using var run = await InProcessExecution
            .RunStreamingAsync(workflow, input: diagnosticsFilePath);

        await foreach (var evt in run.WatchStreamAsync())
        {
            switch (evt)
            {
                case WorkflowOutputEvent output when output.Data is RepairWorkflowState state:
                    RepairWorkflowStatePrinter.Print(state);
                    return state.IsStopped ? 2 : 0;

                case WorkflowErrorEvent error:
                    Console.Error.WriteLine("Workflow error:");
                    Console.Error.WriteLine(error.Data);
                    return 1;
            }
        }

        Console.Error.WriteLine("Workflow completed without output.");
        return 1;
    }

    private static bool HasStopReason(object? data) =>
        data is RepairWorkflowState state && state.IsStopped;

    private static Func<object?, bool> ShouldStop() => HasStopReason;

    private static Func<object?, bool> ShouldContinue() => data => !HasStopReason(data);
}