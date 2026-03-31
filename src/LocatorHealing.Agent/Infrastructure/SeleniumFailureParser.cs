using System.Text.RegularExpressions;

namespace LocatorHealing.Agent.Infrastructure;

internal sealed partial class SeleniumFailureParser(RepoPathResolver repoPathResolver)
{
    public FailureDiagnosticsArtifact Parse(TestFailureInfo failure)
    {
        var outerExceptionType = ExtractOuterExceptionType(failure.Message);
        var rootCauseExceptionType = ExtractRootCauseExceptionType(failure.Message);
        var locatorHint = ExtractLocatorHint(failure.Message);
        var sourceLocations = ExtractSourceLocations(failure.StackTrace);

        var pageObjectLocation = sourceLocations.FirstOrDefault(location =>
            location.FilePath.EndsWith("Page.cs", StringComparison.OrdinalIgnoreCase) ||
            location.FilePath.Contains("/Pages/", StringComparison.OrdinalIgnoreCase) ||
            location.FilePath.Contains("\\Pages\\", StringComparison.OrdinalIgnoreCase));

        var testLocation = sourceLocations.FirstOrDefault(location =>
            location.FilePath.EndsWith("Tests.cs", StringComparison.OrdinalIgnoreCase) ||
            location.FilePath.Contains("/Tests/", StringComparison.OrdinalIgnoreCase) ||
            location.FilePath.Contains("\\Tests\\", StringComparison.OrdinalIgnoreCase));

        return new FailureDiagnosticsArtifact(
            SchemaVersion: "1.2",
            GeneratedAtUtc: DateTimeOffset.UtcNow,
            TestName: failure.TestName,
            TestFullName: failure.TestFullName,
            Url: string.Empty,
            OutcomeStatus: "Failed",
            OutcomeLabel: string.Empty,
            Message: failure.Message,
            StackTrace: failure.StackTrace,
            OuterExceptionType: outerExceptionType,
            RootCauseExceptionType: rootCauseExceptionType,
            LocatorHint: locatorHint,
            PageObjectLocation: pageObjectLocation,
            TestLocation: testLocation,
            RepoRelativePageObjectPath: repoPathResolver.ToRepoRelativePath(pageObjectLocation?.FilePath),
            RepoRelativeTestPath: repoPathResolver.ToRepoRelativePath(testLocation?.FilePath),
            DomSnapshotPath: repoPathResolver.ToRepoRelativePath(failure.DomSnapshotPath) ?? failure.DomSnapshotPath,
            ScreenshotPath: repoPathResolver.ToRepoRelativePath(failure.ScreenshotPath) ?? failure.ScreenshotPath);
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

    private static LocatorHintArtifact? ExtractLocatorHint(string message)
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

        return new LocatorHintArtifact(
            Strategy: match.Groups["method"].Value,
            Selector: match.Groups["selector"].Value);
    }

    private static IReadOnlyList<SourceLocationArtifact> ExtractSourceLocations(string stackTrace)
    {
        if (string.IsNullOrWhiteSpace(stackTrace))
        {
            return [];
        }

        var locations = new List<SourceLocationArtifact>();

        foreach (Match match in SourceLocationRegex().Matches(stackTrace))
        {
            var filePath = match.Groups["file"].Value;

            if (!int.TryParse(match.Groups["line"].Value, out var lineNumber))
            {
                continue;
            }

            locations.Add(new SourceLocationArtifact(filePath, lineNumber));
        }

        return locations;
    }

    [GeneratedRegex(@"---->\s*(?<type>[\w\.]+)\s*:")]
    private static partial Regex NestedExceptionRegex();

    [GeneratedRegex(@"^\s*(?<type>[\w\.]+)\s*:")]
    private static partial Regex TopLevelExceptionRegex();

    [GeneratedRegex("""Unable to locate element:\s*\{"method":"(?<method>[^"]+)","selector":"(?<selector>[^"]+)"\}""")]
    private static partial Regex LocatorRegex();

    [GeneratedRegex(@"in\s+(?<file>.*?\.cs):line\s+(?<line>\d+)")]
    private static partial Regex SourceLocationRegex();
}
