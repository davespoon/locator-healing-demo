using LocatorHealing.Agent.Contracts;
using LocatorHealing.Agent.Infrastructure;
using LocatorHealing.Agent.Policies;
using LocatorHealing.Agent.Workflow;
using Microsoft.Agents.AI.Workflows;

if (args.Length == 0)
{
    Console.WriteLine("Usage:");
    Console.WriteLine("  dotnet run --project src/LocatorHealing.Agent -- <path-to-failure-diagnostics.json>");
    return;
}

var diagnosticsPath = args[0];

var repoRootResolver = new RepoRootResolver();
var diagnosticsReader = new DiagnosticsArtifactReader(repoRootResolver);
var loopGuardPolicy = new LoopGuardPolicy();

var failureIngest = new FailureIngestExecutor(diagnosticsReader);
var loopGuard = new LoopGuardExecutor(loopGuardPolicy);

var workflow = new WorkflowBuilder(failureIngest)
    .AddEdge(failureIngest, loopGuard)
    .WithOutputFrom(loopGuard)
    .Build();

await using var run = await InProcessExecution.RunStreamingAsync(workflow, input: diagnosticsPath);

await foreach (var evt in run.WatchStreamAsync())
{
    switch (evt)
    {
        case WorkflowOutputEvent output when output.Data is RepairWorkflowState state:
            PrintState(state);
            break;

        case WorkflowErrorEvent error:
            Console.WriteLine("Workflow error:");
            Console.WriteLine(error.Data);
            break;
    }
}

static void PrintState(RepairWorkflowState state)
{
    Console.WriteLine("Repair workflow state");
    Console.WriteLine($"  Test: {state.Incident.TestName}");
    Console.WriteLine($"  Page object: {state.Incident.RepoRelativePageObjectPath}");
    Console.WriteLine($"  Selector: {state.Incident.LocatorSelector}");
    Console.WriteLine($"  Root cause: {state.Incident.RootCauseExceptionType}");
    Console.WriteLine($"  DOM snapshot: {state.ResolvedDomSnapshotPath}");

    if (!string.IsNullOrWhiteSpace(state.StopReason))
    {
        Console.WriteLine($"  Stopped: {state.StopReason}");
    }
    else
    {
        Console.WriteLine("  Ready for next workflow step.");
    }
}