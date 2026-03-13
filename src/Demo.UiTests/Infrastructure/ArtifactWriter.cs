using System.Text;
using OpenQA.Selenium;

namespace Demo.UiTests.Infrastructure;

public sealed class ArtifactWriter(string? root = null)
{
    private readonly string _root = root ?? ResolveArtifactsRoot();

    public string WriteDomSnapshot(string testName, string html)
    {
        var path = BuildPath("dom-snapshots", testName, "html");
        File.WriteAllText(path, html, Encoding.UTF8);
        return path;
    }

    public string WriteFailureMetadata(
        string testName,
        string url,
        string outcomeStatus,
        string outcomeLabel,
        string message,
        string stackTrace)
    {
        var path = BuildPath("error-traces", testName, "txt");

        var content = $"""
                       Test: {testName}
                       Url: {url}
                       OutcomeStatus: {outcomeStatus}
                       OutcomeLabel: {outcomeLabel}

                       Message:
                       {message}

                       StackTrace:
                       {stackTrace}
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

        var safeTestName = string.Concat(
            testName.Select(ch => Path.GetInvalidFileNameChars().Contains(ch) ? '_' : ch));

        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");

        return Path.Combine(directory, $"{safeTestName}_{timestamp}.{extension}");
    }

    private static string ResolveArtifactsRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            var hasGit = Directory.Exists(Path.Combine(current.FullName, ".git"));
            var hasSolution = File.Exists(Path.Combine(current.FullName, "LocatorHealingDemo.slnx"));

            if (hasGit || hasSolution)
            {
                return Path.Combine(current.FullName, "artifacts");
            }

            current = current.Parent;
        }

        return Path.Combine(Directory.GetCurrentDirectory(), "artifacts");
    }
}