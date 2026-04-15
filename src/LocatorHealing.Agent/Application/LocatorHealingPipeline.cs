using LocatorHealing.Agent.Infrastructure;

namespace LocatorHealing.Agent.Application;

internal sealed record LocatorHealingPipeline(
    NUnitResultParser ResultParser,
    JsonFailureDiagnosticsWriter DiagnosticsWriter,
    LocatorHealingReportWriter ReportWriter,
    SeleniumFailureParser FailureParser,
    FailureAnalyzer FailureAnalyzer);
