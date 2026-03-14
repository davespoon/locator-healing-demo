namespace Demo.UiTests.Infrastructure;

public interface IFailureDiagnosticsWriter
{
    string Write(FailureDiagnostics diagnostics);
}