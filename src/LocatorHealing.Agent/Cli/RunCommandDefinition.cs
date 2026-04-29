using System.CommandLine;

namespace LocatorHealing.Agent.Cli;

internal static class RunCommandDefinition
{
    public static Command Create(RunCommandHandler handler)
    {
        var resultsDirectoryArgument = new Argument<DirectoryInfo>("test-results-directory")
        {
            Description = "Path to a directory containing NUnit3 XML test results files."
        };

        resultsDirectoryArgument.Validators.Add(result =>
            FileArgumentValidator.ValidateDirectory(result, resultsDirectoryArgument));

        var repoRootOption = new Option<DirectoryInfo>("--repo-root")
        {
            Description = "Repository root that contains the page objects to patch.",
            Required = true
        };

        var outputDirOption = new Option<DirectoryInfo?>("--output-dir")
        {
            Description =
                "Directory to write diagnostics JSON files. Defaults to 'error-traces' next to the results directory."
        };

        var reportFileOption = new Option<FileInfo?>("--report-file")
        {
            Description = "Optional Markdown report file to write for CI logs or a pull request body."
        };

        var command = new Command("run",
            "Parse NUnit XML test results from a directory and generate validated locator repair candidates.")
        {
            resultsDirectoryArgument,
            repoRootOption,
            outputDirOption,
            reportFileOption
        };

        command.SetAction(async parseResult =>
        {
            var resultsDir = parseResult.GetValue(resultsDirectoryArgument);
            var repoRoot = parseResult.GetValue(repoRootOption);
            var outputDir = parseResult.GetValue(outputDirOption);
            var reportFile = parseResult.GetValue(reportFileOption);

            return await handler.InvokeAsync(resultsDir!, repoRoot!, outputDir, reportFile);
        });

        return command;
    }
}