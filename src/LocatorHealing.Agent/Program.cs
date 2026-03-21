using LocatorHealing.Agent.Cli;
using System.CommandLine;

var rootCommand = new RootCommand("Locator healing tool");
rootCommand.Subcommands.Add(AnalyzeCommandDefinition.Create());
return rootCommand.Parse(args).Invoke();