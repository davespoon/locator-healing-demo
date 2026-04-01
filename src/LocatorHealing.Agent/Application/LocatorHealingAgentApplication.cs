using LocatorHealing.Agent.Cli;
using LocatorHealing.Agent.Infrastructure;
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
        var workflow = LocatorHealingWorkflow.Create();
        var resultParser = new NUnitResultParser();
        var repoPathResolver = new RepoPathResolver();
        var failureParser = new SeleniumFailureParser(repoPathResolver);
        var diagnosticsWriter = new JsonFailureDiagnosticsWriter();
        var runHandler = new RunCommandHandler(resultParser, failureParser, diagnosticsWriter, workflow);

        return new RootCommand("Locator healing tool")
        {
            RunCommandDefinition.Create(runHandler)
        };
    }
}