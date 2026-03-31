using System.CommandLine;

namespace LocatorHealing.Agent.Cli;

internal static class IngestCommandDefinition
{
    public static Command Create(IngestCommandHandler handler)
    {
        var resultsFileArgument = new Argument<FileInfo>("test-results-file")
        {
            Description = "Path to an NUnit3 XML test results file."
        };

        resultsFileArgument.Validators.Add(
            result => FileArgumentValidator.Validate(result, resultsFileArgument, ".xml", "test results file"));

        var outputDirOption = new Option<DirectoryInfo?>("--output-dir")
        {
            Description = "Directory to write diagnostics JSON files. Defaults to 'error-traces' next to the results file."
        };

        var command = new Command("ingest", "Parse NUnit XML test results and generate failure diagnostics JSON files.")
        {
            resultsFileArgument,
            outputDirOption
        };

        command.SetAction(async parseResult =>
        {
            var resultsFile = parseResult.GetValue(resultsFileArgument);
            var outputDir = parseResult.GetValue(outputDirOption);
            return await handler.InvokeAsync(resultsFile!, outputDir);
        });

        return command;
    }
}
