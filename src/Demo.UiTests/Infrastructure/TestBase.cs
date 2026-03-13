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
        Driver = WebDriverFactory.CreateChrome();
        Artifacts = new ArtifactWriter();
    }

    [TearDown]
    public void BaseTearDown()
    {
        try
        {
            var status = TestContext.CurrentContext.Result.Outcome.Status;
            if (status == TestStatus.Failed)
            {
                Artifacts.WriteScreenshot(TestContext.CurrentContext.Test.Name, Driver);
                Artifacts.WriteDomSnapshot(TestContext.CurrentContext.Test.Name, Driver.PageSource);
            }
        }
        finally
        {
            Driver.Quit();
            Driver.Dispose();
        }
    }

    public T CaptureLocatorFailure<T>(
        string locatorName,
        string locatorValue,
        Func<T> action)
    {
        try
        {
            return action();
        }
        catch (NoSuchElementException ex)
        {
            Artifacts.WriteDomSnapshot(TestContext.CurrentContext.Test.Name, Driver.PageSource);
            Artifacts.WriteScreenshot(TestContext.CurrentContext.Test.Name, Driver);
            Artifacts.WriteErrorTrace(
                TestContext.CurrentContext.Test.Name,
                ex,
                Driver.Url,
                locatorName,
                locatorValue);

            throw;
        }
    }
}