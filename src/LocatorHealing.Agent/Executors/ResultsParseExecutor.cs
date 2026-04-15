using LocatorHealing.Agent.Infrastructure;
using Microsoft.Agents.AI.Workflows;

namespace LocatorHealing.Agent.Executors;

internal sealed partial class ResultsParseExecutor(NUnitResultParser resultParser) : Executor("ResultsParse")
{
    [MessageHandler]
    private async ValueTask HandleAsync(string resultsFilePath, IWorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        var failures = resultParser.ParseFailures(resultsFilePath);

        foreach (var failure in failures)
        {
            await context.SendMessageAsync(failure);
        }
    }
}