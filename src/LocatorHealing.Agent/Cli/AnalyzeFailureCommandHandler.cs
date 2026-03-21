using LocatorHealing.Agent.Contracts;
using Microsoft.Agents.AI.Workflows;
using AgentWorkflow = Microsoft.Agents.AI.Workflows.Workflow;

namespace LocatorHealing.Agent.Cli;

public sealed class AnalyzeFailureCommandHandler(AgentWorkflow workflow)
{
    public async Task<int> InvokeAsync(FileInfo diagnosticsFile)
    {
        try
        {
            return await ExecuteWorkflowAsync(workflow, diagnosticsFile.FullName);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Unhandled error:");
            Console.Error.WriteLine(ex);
            return 1;
        }
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
}