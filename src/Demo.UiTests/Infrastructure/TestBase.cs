using NUnit.Framework.Interfaces;
using OpenQA.Selenium;

namespace Demo.UiTests.Infrastructure;

public abstract class TestBase
{
    protected IWebDriver Driver = null!;
    protected ArtifactWriter Artifacts = null!;

    private IFailureDiagnosticsWriter _failureDiagnosticsWriter = null!;
    private SeleniumFailureParser _failureParser = null!;

    [SetUp]
    public void BaseSetUp()
    {
        var pathProvider = new ArtifactPathProvider();
        var repoPathResolver = new RepoPathResolver();

        Artifacts = new ArtifactWriter(pathProvider);
        _failureDiagnosticsWriter = new JsonFailureDiagnosticsWriter(pathProvider);
        _failureParser = new SeleniumFailureParser(repoPathResolver);

        Driver = WebDriverFactory.CreateChrome();
    }

    [TearDown]
    public void BaseTearDown()
    {
        try
        {
            var result = TestContext.CurrentContext.Result;
            var status = result.Outcome.Status;

            if (status == TestStatus.Failed)
            {
                var testName = TestContext.CurrentContext.Test.Name;
                var testFullName = TestContext.CurrentContext.Test.FullName;
                var message = result.Message ?? string.Empty;
                var stackTrace = result.StackTrace ?? string.Empty;

                var screenshotPath = TryWriteScreenshot(testName);
                var domPath = TryWriteDomSnapshot(testName);

                var diagnostics = _failureParser.Parse(
                    testName: testName,
                    testFullName: testFullName,
                    url: SafeGetUrl(),
                    outcomeStatus: result.Outcome.Status.ToString(),
                    outcomeLabel: result.Outcome.Label ?? string.Empty,
                    message: message,
                    stackTrace: stackTrace,
                    domSnapshotPath: domPath,
                    screenshotPath: screenshotPath);

                var diagnosticsPath = _failureDiagnosticsWriter.Write(diagnostics);

                AddAttachmentIfExists(screenshotPath, "Failure screenshot");
                AddAttachmentIfExists(domPath, "DOM snapshot");
                AddAttachmentIfExists(diagnosticsPath, "Failure diagnostics");
            }
        }
        finally
        {
            Driver.Quit();
            Driver.Dispose();
        }
    }

    private string? TryWriteScreenshot(string testName)
    {
        try
        {
            return Artifacts.WriteScreenshot(testName, Driver);
        }
        catch
        {
            return null;
        }
    }

    private string? TryWriteDomSnapshot(string testName)
    {
        try
        {
            return Artifacts.WriteDomSnapshot(testName, Driver.PageSource);
        }
        catch
        {
            return null;
        }
    }

    private static void AddAttachmentIfExists(string? path, string description)
    {
        if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
        {
            TestContext.AddTestAttachment(path, description);
        }
    }

    private string SafeGetUrl()
    {
        try
        {
            return Driver.Url;
        }
        catch
        {
            return string.Empty;
        }
    }
}