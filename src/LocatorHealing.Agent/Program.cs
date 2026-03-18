using LocatorHealing.Agent.Cli;
using System.CommandLine;

RootCommand rootCommand = BuildRootCommand();
return rootCommand.Parse(args).Invoke();

static RootCommand BuildRootCommand()
{
    var rootCommand = new RootCommand("Locator healing worker");
    rootCommand.Subcommands.Add(BuildAnalyzeCommand());
    return rootCommand;
}

static Command BuildAnalyzeCommand()
{
    var diagnosticsFileArgument = new Argument<FileInfo>("failure-diagnostics-file")
    {
        Description = "Path to a failure diagnostics JSON file produced by the test framework."
    };

    diagnosticsFileArgument.Validators.Add(result =>
    {
        var file = result.GetValue(diagnosticsFileArgument);

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
    });

    var analyzeCommand = new Command("analyze", "Analyze a failure bundle and generate validated locator candidates.")
    {
        diagnosticsFileArgument
    };

    var handler = new AnalyzeFailureCommandHandler();

    analyzeCommand.SetAction(parseResult =>
    {
        var diagnosticsFile = parseResult.GetValue(diagnosticsFileArgument);
        return handler.Invoke(diagnosticsFile!);
    });

    return analyzeCommand;
}