using System.Text.RegularExpressions;

namespace Demo.UiTests.Infrastructure;

public sealed partial class SeleniumFailureParser(RepoPathResolver repoPathResolver)
{
    public FailureDiagnostics Parse(
        string testName,
        string testFullName,
        string url,
        string outcomeStatus,
        string outcomeLabel,
        string message,
        string stackTrace,
        string? domSnapshotPath,
        string? screenshotPath)
    {
        var outerExceptionType = ExtractOuterExceptionType(message);
        var rootCauseExceptionType = ExtractRootCauseExceptionType(message);
        var locatorHint = ExtractLocatorHint(message);
        var sourceLocations = ExtractSourceLocations(stackTrace);

        var pageObjectLocation = sourceLocations.FirstOrDefault(location =>
            location.FilePath.EndsWith("Page.cs", StringComparison.OrdinalIgnoreCase) ||
            location.FilePath.Contains("/Pages/", StringComparison.OrdinalIgnoreCase) ||
            location.FilePath.Contains("\\Pages\\", StringComparison.OrdinalIgnoreCase));

        var testLocation = sourceLocations.FirstOrDefault(location =>
            location.FilePath.EndsWith("Tests.cs", StringComparison.OrdinalIgnoreCase) ||
            location.FilePath.Contains("/Tests/", StringComparison.OrdinalIgnoreCase) ||
            location.FilePath.Contains("\\Tests\\", StringComparison.OrdinalIgnoreCase));

        return new FailureDiagnostics(
            SchemaVersion: "1.1",
            GeneratedAtUtc: DateTimeOffset.UtcNow,
            TestName: testName,
            TestFullName: testFullName,
            Url: url,
            OutcomeStatus: outcomeStatus,
            OutcomeLabel: outcomeLabel,
            Message: message,
            StackTrace: stackTrace,
            OuterExceptionType: outerExceptionType,
            RootCauseExceptionType: rootCauseExceptionType,
            LocatorHint: locatorHint,
            PageObjectLocation: pageObjectLocation,
            TestLocation: testLocation,
            RepoRelativePageObjectPath: repoPathResolver.ToRepoRelativePath(pageObjectLocation?.FilePath),
            RepoRelativeTestPath: repoPathResolver.ToRepoRelativePath(testLocation?.FilePath),
            DomSnapshotPath: repoPathResolver.ToRepoRelativePath(domSnapshotPath) ?? domSnapshotPath,
            ScreenshotPath: repoPathResolver.ToRepoRelativePath(screenshotPath) ?? screenshotPath);
    }

    private static string? ExtractOuterExceptionType(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return null;
        }

        var match = TopLevelExceptionRegex().Match(message);
        return match.Success ? match.Groups["type"].Value : null;
    }

    private static string? ExtractRootCauseExceptionType(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return null;
        }

        var nestedMatch = NestedExceptionRegex().Match(message);
        if (nestedMatch.Success)
        {
            return nestedMatch.Groups["type"].Value;
        }

        var topLevelMatch = TopLevelExceptionRegex().Match(message);
        return topLevelMatch.Success ? topLevelMatch.Groups["type"].Value : null;
    }

    private static LocatorHint? ExtractLocatorHint(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return null;
        }

        var match = LocatorRegex().Match(message);
        if (!match.Success)
        {
            return null;
        }

        return new LocatorHint(
            Strategy: match.Groups["method"].Value,
            Selector: match.Groups["selector"].Value);
    }

    private static IReadOnlyList<SourceLocation> ExtractSourceLocations(string stackTrace)
    {
        if (string.IsNullOrWhiteSpace(stackTrace))
        {
            return [];
        }

        var locations = new List<SourceLocation>();

        foreach (Match match in SourceLocationRegex().Matches(stackTrace))
        {
            var filePath = match.Groups["file"].Value;

            if (!int.TryParse(match.Groups["line"].Value, out var lineNumber))
            {
                continue;
            }

            locations.Add(new SourceLocation(filePath, lineNumber));
        }

        return locations;
    }

    [GeneratedRegex(@"---->\s*(?<type>[\w\.]+)\s*:", RegexOptions.Compiled)]
    private static partial Regex NestedExceptionRegex();

    [GeneratedRegex(@"^\s*(?<type>[\w\.]+)\s*:", RegexOptions.Compiled)]
    private static partial Regex TopLevelExceptionRegex();

    [GeneratedRegex("""Unable to locate element:\s*\{"method":"(?<method>[^"]+)","selector":"(?<selector>[^"]+)"\}""",
        RegexOptions.Compiled)]
    private static partial Regex LocatorRegex();

    [GeneratedRegex(@"in\s+(?<file>.*?\.cs):line\s+(?<line>\d+)", RegexOptions.Compiled)]
    private static partial Regex SourceLocationRegex();
}