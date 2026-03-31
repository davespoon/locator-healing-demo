using System.CommandLine;
using System.CommandLine.Parsing;

namespace LocatorHealing.Agent.Cli;

internal static class IngestCommandDefinition
{
    public static Command Create(IngestCommandHandler handler)
    {
        var resultsFileArgument = CreateResultsFileArgument();

        var outputDirOption = new Option<DirectoryInfo?>("--output-dir")
        {
            Description = "Directory to write diagnostics JSON files. Defaults to 'error-traces' next to the results file."
        };

        var ingestCommand =
            new Command("ingest", "Parse NUnit XML test results and generate failure diagnostics JSON files.")
            {
                resultsFileArgument,
                outputDirOption
            };

        BindHandler(ingestCommand, resultsFileArgument, outputDirOption, handler);

        return ingestCommand;
    }

    private static Argument<FileInfo> CreateResultsFileArgument()
    {
        var argument = new Argument<FileInfo>("test-results-file")
        {
            Description = "Path to an NUnit3 XML test results file."
        };

        argument.Validators.Add(result => ValidateResultsFile(result, argument));

        return argument;
    }

    private static void ValidateResultsFile(ArgumentResult result, Argument<FileInfo> argument)
    {
        var file = result.GetValue(argument);

        if (file is null)
        {
            result.AddError("A test results file path is required.");
            return;
        }

        if (!file.Exists)
        {
            result.AddError($"Test results file does not exist: {file.FullName}");
            return;
        }

        if (!string.Equals(file.Extension, ".xml", StringComparison.OrdinalIgnoreCase))
        {
            result.AddError("Test results file must be an .xml file.");
        }
    }

    private static void BindHandler(
        Command command,
        Argument<FileInfo> resultsFileArgument,
        Option<DirectoryInfo?> outputDirOption,
        IngestCommandHandler handler)
    {
        command.SetAction(async parseResult =>
        {
            var resultsFile = parseResult.GetValue(resultsFileArgument);
            var outputDir = parseResult.GetValue(outputDirOption);
            return await handler.InvokeAsync(resultsFile!, outputDir);
        });
    }
}
