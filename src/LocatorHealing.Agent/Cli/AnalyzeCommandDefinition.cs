using System.CommandLine;
using System.CommandLine.Parsing;

namespace LocatorHealing.Agent.Cli;

internal static class AnalyzeCommandDefinition
{
    public static Command Create()
    {
        var diagnosticsFileArgument = CreateDiagnosticsFileArgument();

        var analyzeCommand =
            new Command("analyze", "Analyze a failure bundle and generate validated locator candidates.")
            {
                diagnosticsFileArgument
            };

        BindHandler(analyzeCommand, diagnosticsFileArgument);

        return analyzeCommand;
    }

    private static Argument<FileInfo> CreateDiagnosticsFileArgument()
    {
        var argument = new Argument<FileInfo>("failure-diagnostics-file")
        {
            Description = "Path to a failure diagnostics JSON file produced by the test framework."
        };

        argument.Validators.Add(result => ValidateDiagnosticsFile(result, argument));

        return argument;
    }

    private static void ValidateDiagnosticsFile(ArgumentResult result, Argument<FileInfo> argument)
    {
        var file = result.GetValue(argument);

        if (file is null)
        {
            result.AddError("A diagnostics JSON file path is required.");
            return;
        }

        if (!file.Exists)
        {
            result.AddError($"Diagnostics file does not exist: {file.FullName}");
            return;
        }

        if (!string.Equals(file.Extension, ".json", StringComparison.OrdinalIgnoreCase))
        {
            result.AddError("Diagnostics file must be a .json file.");
        }
    }

    private static void BindHandler(Command command, Argument<FileInfo> diagnosticsFileArgument)
    {
        var handler = new AnalyzeFailureCommandHandler();

        command.SetAction(async parseResult =>
        {
            var diagnosticsFile = parseResult.GetValue(diagnosticsFileArgument);
            return await handler.InvokeAsync(diagnosticsFile!);
        });
    }
}