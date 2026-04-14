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
        var resultParser = new NUnitResultParser();
        var diagnosticsWriter = new JsonFailureDiagnosticsWriter();
        var reportWriter = new LocatorHealingReportWriter();
        var runHandler = new RunCommandHandler(resultParser, diagnosticsWriter, reportWriter);

        return new RootCommand("Locator healing tool")
        {
            RunCommandDefinition.Create(runHandler)
        };
    }
}