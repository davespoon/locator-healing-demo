using System.CommandLine;

namespace LocatorHealing.Agent.Cli;

internal static class AnalyzeCommandDefinition
{
    public static Command Create(AnalyzeFailureCommandHandler handler)
    {
        var diagnosticsFileArgument = new Argument<FileInfo>("failure-diagnostics-file")
        {
            Description = "Path to a failure diagnostics JSON file produced by the test framework."
        };

        diagnosticsFileArgument.Validators.Add(
            result => FileArgumentValidator.Validate(result, diagnosticsFileArgument, ".json", "diagnostics JSON file"));

        var command = new Command("analyze", "Analyze a failure bundle and generate validated locator candidates.")
        {
            diagnosticsFileArgument
        };

        command.SetAction(async parseResult =>
        {
            var diagnosticsFile = parseResult.GetValue(diagnosticsFileArgument);
            return await handler.InvokeAsync(diagnosticsFile!);
        });

        return command;
    }
}