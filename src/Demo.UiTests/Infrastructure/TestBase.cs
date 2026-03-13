using NUnit.Framework.Interfaces;
using OpenQA.Selenium;

namespace Demo.UiTests.Infrastructure;

public abstract class TestBase
{
    protected IWebDriver Driver = null!;
    protected ArtifactWriter Artifacts = null!;

    [SetUp]
    public void BaseSetUp()
    {
        Artifacts = new ArtifactWriter();
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

                var screenshotPath = Artifacts.WriteScreenshot(testName, Driver);
                var domPath = Artifacts.WriteDomSnapshot(testName, Driver.PageSource);
                var metadataPath = Artifacts.WriteFailureMetadata(
                    testName: testName,
                    url: SafeGetUrl(),
                    outcomeStatus: result.Outcome.Status.ToString(),
                    outcomeLabel: result.Outcome.Label ?? string.Empty,
                    message: result.Message ?? string.Empty,
                    stackTrace: result.StackTrace ?? string.Empty);

                if (!string.IsNullOrWhiteSpace(screenshotPath) && File.Exists(screenshotPath))
                {
                    TestContext.AddTestAttachment(screenshotPath, "Failure screenshot");
                }

                if (File.Exists(domPath))
                {
                    TestContext.AddTestAttachment(domPath, "DOM snapshot");
                }

                if (File.Exists(metadataPath))
                {
                    TestContext.AddTestAttachment(metadataPath, "Failure metadata");
                }
            }
        }
        finally
        {
            Driver.Quit();
            Driver.Dispose();
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