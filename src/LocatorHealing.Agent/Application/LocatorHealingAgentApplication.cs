using LocatorHealing.Agent.Cli;
using System.CommandLine;

namespace LocatorHealing.Agent.Application;

public static class LocatorHealingAgentApplication
{
    public static Task<int> RunAsync(string[] args)
    {
        var rootCommand = BuildRootCommand();
        return rootCommand.Parse(args).InvokeAsync();
    }

    private static RootCommand BuildRootCommand()
    {
        var runHandler = new RunCommandHandler();

        return new RootCommand("Locator healing tool")
        {
            RunCommandDefinition.Create(runHandler)
        };
    }
}