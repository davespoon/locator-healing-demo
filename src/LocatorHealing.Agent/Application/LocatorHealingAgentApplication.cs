using LocatorHealing.Agent.Cli;
using System.CommandLine;

namespace LocatorHealing.Agent.Application;

public sealed class LocatorHealingAgentApplication(AnalyzeFailureCommandHandler analyzeFailureCommandHandler)
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
        return new LocatorHealingAgentApplication(analyzeFailureCommandHandler);
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
            AnalyzeCommandDefinition.Create(analyzeFailureCommandHandler)
        };

        return rootCommand;
    }
}