using LocatorHealing.Agent.Infrastructure;
using Microsoft.Agents.AI.Workflows;

namespace LocatorHealing.Agent.Executors;

internal sealed partial class DiagnosticsPreparationExecutor(
    SeleniumFailureParser failureParser,
    JsonFailureDiagnosticsWriter diagnosticsWriter,
    string outputDirectory) : Executor("DiagnosticsPreparation")
{
    [MessageHandler]
    private ValueTask<string> HandleAsync(TestFailureInfo failure, IWorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        var artifact = failureParser.Parse(failure);
        var diagnosticsPath = diagnosticsWriter.Write(artifact, outputDirectory);

        return ValueTask.FromResult(diagnosticsPath);
    }
}