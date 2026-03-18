using LocatorHealing.Agent.Contracts;
using LocatorHealing.Agent.Infrastructure;
using LocatorHealing.Agent.Policies;
using LocatorHealing.Agent.Workflow;
using Microsoft.Agents.AI.Workflows;

namespace LocatorHealing.Agent.Cli;

public sealed class AnalyzeFailureCommandHandler
{
    public int Invoke(FileInfo diagnosticsFile)
    {
        try
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

            var workflow = new WorkflowBuilder(failureIngest)
                .AddEdge(failureIngest, loopGuard)
                .AddEdge(loopGuard, stop, condition: ShouldStop())
                .AddEdge(loopGuard, locatorFailureCheck, condition: ShouldContinue())
                .AddEdge(locatorFailureCheck, stop, condition: ShouldStop())
                .AddEdge(locatorFailureCheck, candidateGeneration, condition: ShouldContinue())
                .AddEdge(candidateGeneration, candidateValidation)
                .WithOutputFrom(stop, candidateValidation)
                .Build();

            var run = InProcessExecution
                .RunStreamingAsync(workflow, input: diagnosticsFile.FullName)
                .GetAwaiter()
                .GetResult();

            try
            {
                var enumerator = run.WatchStreamAsync().GetAsyncEnumerator();

                try
                {
                    while (enumerator.MoveNextAsync().AsTask().GetAwaiter().GetResult())
                    {
                        var evt = enumerator.Current;

                        switch (evt)
                        {
                            case WorkflowOutputEvent output when output.Data is RepairWorkflowState state:
                                PrintState(state);
                                return string.IsNullOrWhiteSpace(state.StopReason) ? 0 : 2;

                            case WorkflowErrorEvent error:
                                Console.Error.WriteLine("Workflow error:");
                                Console.Error.WriteLine(error.Data);
                                return 1;
                        }
                    }
                }
                finally
                {
                    enumerator.DisposeAsync().AsTask().GetAwaiter().GetResult();
                }

                Console.Error.WriteLine("Workflow completed without output.");
                return 1;
            }
            finally
            {
                run.DisposeAsync().AsTask().GetAwaiter().GetResult();
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Unhandled error:");
            Console.Error.WriteLine(ex);
            return 1;
        }
    }

    private static Func<object?, bool> ShouldStop() =>
        data => data is RepairWorkflowState state && !string.IsNullOrWhiteSpace(state.StopReason);

    private static Func<object?, bool> ShouldContinue() =>
        data => data is RepairWorkflowState state && string.IsNullOrWhiteSpace(state.StopReason);

    private static void PrintState(RepairWorkflowState state)
    {
        Console.WriteLine("Repair workflow state");
        Console.WriteLine($"  Test: {state.Incident.TestName}");
        Console.WriteLine($"  Page object: {state.Incident.RepoRelativePageObjectPath}");
        Console.WriteLine($"  Selector: {state.Incident.LocatorSelector}");
        Console.WriteLine($"  Root cause: {state.Incident.RootCauseExceptionType}");
        Console.WriteLine($"  DOM snapshot: {state.ResolvedDomSnapshotPath}");

        if (state.Candidates.Count > 0)
        {
            Console.WriteLine($"  Generated candidates: {state.Candidates.Count}");
            foreach (var candidate in state.Candidates)
            {
                Console.WriteLine(
                    $"    - {candidate.Strategy}: {candidate.Value} (confidence {candidate.Confidence:0.00})");
                Console.WriteLine($"      {candidate.Reason}");
            }
        }

        if (state.ValidationResults.Count > 0)
        {
            Console.WriteLine($"  Validation results: {state.ValidationResults.Count}");
            foreach (var result in state.ValidationResults)
            {
                Console.WriteLine(
                    $"    - {(result.IsValid ? "VALID" : "INVALID")}: {result.Candidate.Value}");
                Console.WriteLine($"      {result.Summary}");
            }
        }

        if (state.SelectedCandidate is not null)
        {
            Console.WriteLine("  Selected candidate:");
            Console.WriteLine(
                $"    {state.SelectedCandidate.Candidate.Strategy}: {state.SelectedCandidate.Candidate.Value}");
        }

        if (!string.IsNullOrWhiteSpace(state.StopReason))
        {
            Console.WriteLine($"  Stopped: {state.StopReason}");
        }
        else
        {
            Console.WriteLine("  Ready for patching step.");
        }
    }
}