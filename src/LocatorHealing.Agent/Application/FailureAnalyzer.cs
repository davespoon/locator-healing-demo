using LocatorHealing.Agent.Contracts;
using Microsoft.Agents.AI.Workflows;
using AgentWorkflow = Microsoft.Agents.AI.Workflows.Workflow;

namespace LocatorHealing.Agent.Application;

internal sealed class FailureAnalyzer(AgentWorkflow workflow)
{
    public async Task<AnalysisResult> AnalyzeAsync(string diagnosticsFilePath)
    {
        try
        {
            await using var run = await InProcessExecution.RunStreamingAsync(workflow, input: diagnosticsFilePath);

            await foreach (var evt in run.WatchStreamAsync())
            {
                switch (evt)
                {
                    case WorkflowOutputEvent output when output.Data is RepairWorkflowState state:
                        return new AnalysisResult(state.IsStopped ? 2 : 0, state);

                    case WorkflowErrorEvent error:
                        return new AnalysisResult(1, null, $"Workflow error:{Environment.NewLine}{error.Data}");
                }
            }

            return new AnalysisResult(1, null, "Workflow completed without output.");
        }
        catch (Exception ex)
        {
            return new AnalysisResult(1, null, $"Unhandled error:{Environment.NewLine}{ex}");
        }
    }
}

internal sealed record AnalysisResult(int ExitCode, RepairWorkflowState? State, string? ErrorMessage = null);
