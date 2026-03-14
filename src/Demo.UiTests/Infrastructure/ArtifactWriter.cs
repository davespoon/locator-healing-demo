using System.Text;
using OpenQA.Selenium;

namespace Demo.UiTests.Infrastructure;

public sealed class ArtifactWriter(ArtifactPathProvider pathProvider)
{
    private readonly ArtifactPathProvider _pathProvider = pathProvider;

    public string WriteDomSnapshot(string testName, string html)
    {
        var path = _pathProvider.CreateArtifactPath("dom-snapshots", testName, "html");
        File.WriteAllText(path, html, Encoding.UTF8);
        return path;
    }

    public string? WriteScreenshot(string testName, IWebDriver driver)
    {
        if (driver is not ITakesScreenshot screenshotDriver)
        {
            return null;
        }

        var path = _pathProvider.CreateArtifactPath("screenshots", testName, "png");
        screenshotDriver.GetScreenshot().SaveAsFile(path);
        return path;
    }
}