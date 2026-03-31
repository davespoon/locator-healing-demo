using LocatorHealing.Agent.Infrastructure;

namespace LocatorHealing.Agent.Cli;

internal sealed class IngestCommandHandler(
    NUnitResultParser resultParser,
    SeleniumFailureParser failureParser,
    JsonFailureDiagnosticsWriter diagnosticsWriter)
{
    public Task<int> InvokeAsync(FileInfo resultsFile, DirectoryInfo? outputDir)
    {
        try
        {
            var failures = resultParser.ParseFailures(resultsFile.FullName);

            if (failures.Count == 0)
            {
                Console.WriteLine("No test failures found in the results file.");
                return Task.FromResult(0);
            }

            var targetDir = outputDir?.FullName
                ?? Path.Combine(Path.GetDirectoryName(resultsFile.FullName)!, "error-traces");

            foreach (var failure in failures)
            {
                var artifact = failureParser.Parse(failure);
                var path = diagnosticsWriter.Write(artifact, targetDir);
                Console.WriteLine(path);
            }

            Console.WriteLine($"\nGenerated {failures.Count} diagnostics file(s) in {targetDir}");
            return Task.FromResult(0);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Error processing test results:");
            Console.Error.WriteLine(ex.Message);
            return Task.FromResult(1);
        }
    }
}
