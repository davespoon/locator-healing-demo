using System.Text;
using OpenQA.Selenium;

namespace Demo.UiTests.Infrastructure;

public sealed class ArtifactWriter(ArtifactPathProvider pathProvider)
{
    private readonly ArtifactPathProvider _pathProvider = pathProvider;

    public string WriteDomSnapshot(string testName, string html)
    {
        var path = _pathProvider.CreateArtifactPath(testName, "html");
        File.WriteAllText(path, html, Encoding.UTF8);
        return path;
    }

    public string WriteScreenshot(string testName, IWebDriver driver)
    {
        if (driver is not ITakesScreenshot screenshotDriver)
        {
            throw new InvalidOperationException("Driver does not support screenshots.");
        }

        var path = _pathProvider.CreateArtifactPath(testName, "png");
        screenshotDriver.GetScreenshot().SaveAsFile(path);
        return path;
    }
}