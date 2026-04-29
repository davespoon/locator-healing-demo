using LocatorHealing.Agent.Infrastructure;
using Microsoft.Agents.AI.Workflows;

namespace LocatorHealing.Agent.Executors;

[SendsMessage(typeof(TestFailureInfo))]
internal sealed partial class ResultsParseExecutor(NUnitResultParser resultParser) : Executor("ResultsParse")
{
    [MessageHandler]
    private async ValueTask HandleAsync(
        string resultsDirectoryPath, IWorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        var failures = resultParser.ParseFailuresFromDirectory(resultsDirectoryPath);

        foreach (var failure in failures)
        {
            await context.SendMessageAsync(failure, cancellationToken);
        }
    }
}