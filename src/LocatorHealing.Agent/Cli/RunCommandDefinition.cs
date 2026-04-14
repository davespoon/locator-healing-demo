using System.CommandLine;

namespace LocatorHealing.Agent.Cli;

internal static class RunCommandDefinition
{
    public static Command Create(RunCommandHandler handler)
    {
        var resultsFileArgument = new Argument<FileInfo>("test-results-file")
        {
            Description = "Path to an NUnit3 XML test results file."
        };

        resultsFileArgument.Validators.Add(result =>
            FileArgumentValidator.Validate(result, resultsFileArgument, ".xml", "test results file"));

        var repoRootOption = new Option<DirectoryInfo>("--repo-root")
        {
            Description = "Repository root that contains the page objects to patch.",
            Required = true
        };

        var outputDirOption = new Option<DirectoryInfo?>("--output-dir")
        {
            Description =
                "Directory to write diagnostics JSON files. Defaults to 'error-traces' next to the results file."
        };

        var reportFileOption = new Option<FileInfo?>("--report-file")
        {
            Description = "Optional Markdown report file to write for CI logs or a pull request body."
        };

        var command = new Command("run",
            "Parse NUnit XML test results and generate validated locator repair candidates.")
        {
            resultsFileArgument,
            repoRootOption,
            outputDirOption,
            reportFileOption
        };

        command.SetAction(async parseResult =>
        {
            var resultsFile = parseResult.GetValue(resultsFileArgument);
            var repoRoot = parseResult.GetValue(repoRootOption);
            var outputDir = parseResult.GetValue(outputDirOption);
            var reportFile = parseResult.GetValue(reportFileOption);

            return await handler.InvokeAsync(resultsFile!, repoRoot!, outputDir, reportFile);
        });

        return command;
    }
}