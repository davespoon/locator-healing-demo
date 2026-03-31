using LocatorHealing.Agent.Cli;
using System.CommandLine;

namespace LocatorHealing.Agent.Application;

public sealed class LocatorHealingAgentApplication(
    AnalyzeFailureCommandHandler analyzeFailureCommandHandler,
    IngestCommandHandler ingestCommandHandler)
{
    public static Task<int> RunAsync(string[] args)
    {
        var application = Create();
        return application.InvokeAsync(args);
    }

    private static LocatorHealingAgentApplication Create()
    {
        var workflow = LocatorHealingWorkflow.Create();
        var analyzeFailureCommandHandler = new AnalyzeFailureCommandHandler(workflow);
        var ingestCommandHandler = new IngestCommandHandler();
        return new LocatorHealingAgentApplication(analyzeFailureCommandHandler, ingestCommandHandler);
    }

    private Task<int> InvokeAsync(string[] args)
    {
        var rootCommand = BuildRootCommand();
        return rootCommand.Parse(args).InvokeAsync();
    }

    private RootCommand BuildRootCommand()
    {
        var rootCommand = new RootCommand("Locator healing tool")
        {
            AnalyzeCommandDefinition.Create(analyzeFailureCommandHandler),
            IngestCommandDefinition.Create(ingestCommandHandler)
        };

        return rootCommand;
    }
}