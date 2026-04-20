using LocatorHealing.Agent.Application;
using LocatorHealing.Agent.Contracts;
using LocatorHealing.Agent.Infrastructure;
using Microsoft.Agents.AI.Workflows;

namespace LocatorHealing.Agent.Cli;

internal sealed class RunCommandHandler
{
    private readonly LocatorHealingReportWriter _reportWriter = new();

    public async Task<int> InvokeAsync(
        FileInfo resultsFile,
        DirectoryInfo repoRoot,
        DirectoryInfo? outputDir,
        FileInfo? reportFile)
    {
        if (repoRoot is not null && !repoRoot.Exists)
        {
            Console.Error.WriteLine($"Repository root was not found: {repoRoot.FullName}");
            return 1;
        }

        var targetDir = outputDir?.FullName
                        ?? Path.Combine(Path.GetDirectoryName(resultsFile.FullName)!, "error-traces");

        var workflow = LocatorHealingWorkflow.Create(repoRoot.FullName, targetDir);

        var exitCode = 0;
        var analyzedStates = new List<RepairWorkflowState>();

        try
        {
            await using var run = await InProcessExecution.RunStreamingAsync(
                workflow, input: resultsFile.FullName);

            await foreach (var evt in run.WatchStreamAsync())
            {
                switch (evt)
                {
                    case WorkflowOutputEvent output when output.Data is RepairWorkflowState state:
                        RepairWorkflowStatePrinter.Print(state);
                        analyzedStates.Add(state);

                        if (state.IsStopped && exitCode < 2)
                        {
                            exitCode = 2;
                        }

                        break;

                    case WorkflowErrorEvent error:
                        Console.Error.WriteLine($"Workflow error:{Environment.NewLine}{error.Data}");
                        exitCode = 1;
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Error running workflow:");
            Console.Error.WriteLine(ex.Message);
            return 1;
        }

        if (analyzedStates.Count == 0)
        {
            Console.WriteLine("No test failures found in the results file.");
            return exitCode;
        }

        WriteReportIfRequested(reportFile, analyzedStates);

        return exitCode;
    }

    private void WriteReportIfRequested(
        FileInfo? reportFile,
        IReadOnlyList<RepairWorkflowState> states)
    {
        if (reportFile is null)
        {
            return;
        }

        try
        {
            _reportWriter.Write(reportFile.FullName, states);
            Console.WriteLine($"Locator healing report written to: {reportFile.FullName}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Failed to write locator healing report:");
            Console.Error.WriteLine(ex.Message);
        }
    }
}