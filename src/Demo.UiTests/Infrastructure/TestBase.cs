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
        var pathProvider = new ArtifactPathProvider();
        Artifacts = new ArtifactWriter(pathProvider);
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

                var screenshotPath = TryWriteScreenshot(testName);
                var domPath = TryWriteDomSnapshot(testName);

                AddAttachmentIfExists(screenshotPath, "Screenshot");
                AddAttachmentIfExists(domPath, "DOM snapshot");
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
}