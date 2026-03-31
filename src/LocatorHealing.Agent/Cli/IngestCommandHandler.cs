using LocatorHealing.Agent.Infrastructure;

namespace LocatorHealing.Agent.Cli;

public sealed class IngestCommandHandler
{
    public Task<int> InvokeAsync(FileInfo resultsFile, DirectoryInfo? outputDir)
    {
        try
        {
            var parser = new NUnitResultParser();
            var failures = parser.ParseFailures(resultsFile.FullName);

            if (failures.Count == 0)
            {
                Console.WriteLine("No test failures found in the results file.");
                return Task.FromResult(0);
            }

            var repoPathResolver = new RepoPathResolver();
            var failureParser = new SeleniumFailureParser(repoPathResolver);
            var writer = new JsonFailureDiagnosticsWriter();

            var targetDir = outputDir?.FullName
                ?? Path.Combine(Path.GetDirectoryName(resultsFile.FullName)!, "error-traces");

            foreach (var failure in failures)
            {
                var artifact = failureParser.Parse(failure);
                var path = writer.Write(artifact, targetDir);
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
