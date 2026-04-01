using LocatorHealing.Agent.Contracts;
using LocatorHealing.Agent.Infrastructure;
using Microsoft.Agents.AI.Workflows;
using AgentWorkflow = Microsoft.Agents.AI.Workflows.Workflow;

namespace LocatorHealing.Agent.Cli;

internal sealed class RunCommandHandler(
    NUnitResultParser resultParser,
    SeleniumFailureParser failureParser,
    JsonFailureDiagnosticsWriter diagnosticsWriter,
    AgentWorkflow workflow)
{
    public async Task<int> InvokeAsync(FileInfo resultsFile, DirectoryInfo? outputDir)
    {
        IReadOnlyList<TestFailureInfo> failures;

        try
        {
            failures = resultParser.ParseFailures(resultsFile.FullName);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Error parsing test results:");
            Console.Error.WriteLine(ex.Message);
            return 1;
        }

        if (failures.Count == 0)
        {
            Console.WriteLine("No test failures found in the results file.");
            return 0;
        }

        var targetDir = outputDir?.FullName
                        ?? Path.Combine(Path.GetDirectoryName(resultsFile.FullName)!, "error-traces");

        var exitCode = 0;

        foreach (var failure in failures)
        {
            var artifact = failureParser.Parse(failure);
            var diagnosticsPath = diagnosticsWriter.Write(artifact, targetDir);

            var failureExitCode = await AnalyzeAsync(diagnosticsPath);
            if (failureExitCode > exitCode)
            {
                exitCode = failureExitCode;
            }
        }

        return exitCode;
    }

    private async Task<int> AnalyzeAsync(string diagnosticsFilePath)
    {
        try
        {
            await using var run = await InProcessExecution.RunStreamingAsync(workflow, input: diagnosticsFilePath);

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
        catch (Exception ex)
        {
            Console.Error.WriteLine("Unhandled error:");
            Console.Error.WriteLine(ex);
            return 1;
        }
    }
}