using System.Text;
using OpenQA.Selenium;

namespace Demo.UiTests.Infrastructure;

public sealed class ArtifactWriter(string? root = null)
{
    private readonly string _root = root ?? Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "artifacts");

    public string WriteDomSnapshot(string testName, string html)
    {
        var path = BuildPath("dom-snapshots", testName, "html");
        File.WriteAllText(path, html, Encoding.UTF8);
        return path;
    }

    public string WriteErrorTrace(string testName, Exception ex, string url, string locatorName, string locatorValue)
    {
        var path = BuildPath("error-traces", testName, "txt");

        var content = $"""
                       Test: {testName}
                       Url: {url}
                       LocatorName: {locatorName}
                       LocatorValue: {locatorValue}
                       ExceptionType: {ex.GetType().FullName}
                       Message: {ex.Message}

                       StackTrace:
                       {ex.StackTrace}
                       """;

        File.WriteAllText(path, content, Encoding.UTF8);
        return path;
    }

    public string? WriteScreenshot(string testName, IWebDriver driver)
    {
        if (driver is not ITakesScreenshot screenshotDriver)
        {
            return null;
        }

        var path = BuildPath("screenshots", testName, "png");
        screenshotDriver.GetScreenshot().SaveAsFile(path);
        return path;
    }

    private string BuildPath(string folder, string testName, string extension)
    {
        var directory = Path.Combine(_root, folder);
        Directory.CreateDirectory(directory);

        var safeTestName = string.Concat(testName.Select(ch => Path.GetInvalidFileNameChars().Contains(ch) ? '_' : ch));
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");

        return Path.Combine(directory, $"{safeTestName}_{timestamp}.{extension}");
    }
}