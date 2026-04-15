using LocatorHealing.Agent.Application;
using LocatorHealing.Agent.Contracts;
using LocatorHealing.Agent.Infrastructure;

namespace LocatorHealing.Agent.Cli;

internal sealed class RunCommandHandler(LocatorHealingPipelineFactory pipelineFactory)
{
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

        var pipeline = pipelineFactory.Create(repoRoot.FullName);

        IReadOnlyList<TestFailureInfo> failures;

        try
        {
            failures = pipeline.ResultParser.ParseFailures(resultsFile.FullName);
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
            WriteReportIfRequested(pipeline, reportFile, []);
            return 0;
        }

        var targetDir = outputDir?.FullName
                        ?? Path.Combine(Path.GetDirectoryName(resultsFile.FullName)!, "error-traces");

        var exitCode = 0;
        var analyzedStates = new List<RepairWorkflowState>();

        foreach (var failure in failures)
        {
            var artifact = pipeline.FailureParser.Parse(failure);
            var diagnosticsPath = pipeline.DiagnosticsWriter.Write(artifact, targetDir);

            var result = await pipeline.FailureAnalyzer.AnalyzeAsync(diagnosticsPath);

            if (result.ErrorMessage is not null)
            {
                Console.Error.WriteLine(result.ErrorMessage);
            }

            if (result.State is not null)
            {
                RepairWorkflowStatePrinter.Print(result.State);
                analyzedStates.Add(result.State);
            }

            if (result.ExitCode > exitCode)
            {
                exitCode = result.ExitCode;
            }
        }

        WriteReportIfRequested(pipeline, reportFile, analyzedStates);

        return exitCode;
    }

    private static void WriteReportIfRequested(
        LocatorHealingPipeline pipeline,
        FileInfo? reportFile,
        IReadOnlyList<RepairWorkflowState> states)
    {
        if (reportFile is null)
        {
            return;
        }

        try
        {
            pipeline.ReportWriter.Write(reportFile.FullName, states);
            Console.WriteLine($"Locator healing report written to: {reportFile.FullName}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Failed to write locator healing report:");
            Console.Error.WriteLine(ex.Message);
        }
    }
}