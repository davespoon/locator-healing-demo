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

        if (pageObjectLocation is not null && File.Exists(pageObjectLocation.FilePath))
        {
            var definitionLine = ResolveLocatorDefinitionLine(
                File.ReadAllText(pageObjectLocation.FilePath), pageObjectLocation.LineNumber);

            if (definitionLine is not null)
            {
                pageObjectLocation = pageObjectLocation with { LineNumber = definitionLine.Value };
            }
        }

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

    private static int? ResolveLocatorDefinitionLine(string source, int usageLineNumber)
    {
        var lines = source.Split('\n');
        var usageIndex = usageLineNumber - 1;

        if (usageIndex < 0 || usageIndex >= lines.Length)
        {
            return null;
        }

        var windowStart = Math.Max(0, usageIndex - 2);
        var windowEnd = Math.Min(lines.Length - 1, usageIndex + 2);

        for (var i = windowStart; i <= windowEnd; i++)
        {
            if (ByInlineSelectorRegex().IsMatch(lines[i]))
            {
                return i + 1;
            }

            var match = ByIdentifierArgRegex().Match(lines[i]);
            if (match.Success)
            {
                return FindFieldAssignment(lines, match.Groups["id"].Value);
            }
        }

        return null;
    }

    private static int? FindFieldAssignment(string[] lines, string fieldName)
    {
        for (var i = 0; i < lines.Length; i++)
        {
            var idx = lines[i].IndexOf(fieldName, StringComparison.Ordinal);
            if (idx < 0)
            {
                continue;
            }

            var afterField = lines[i].AsSpan()[(idx + fieldName.Length)..].TrimStart();
            if (afterField.StartsWith("="))
            {
                return i + 1;
            }
        }

        return null;
    }

    [GeneratedRegex(@"By\.\w+\(\s*""")]
    private static partial Regex ByInlineSelectorRegex();

    [GeneratedRegex(@"By\.\w+\(\s*(?<id>[A-Za-z_]\w*)\s*\)")]
    private static partial Regex ByIdentifierArgRegex();

    [GeneratedRegex(@"---->\s*(?<type>[\w\.]+)\s*:")]
    private static partial Regex NestedExceptionRegex();

    [GeneratedRegex(@"^\s*(?<type>[\w\.]+)\s*:")]
    private static partial Regex TopLevelExceptionRegex();

    [GeneratedRegex("""Unable to locate element:\s*\{"method":"(?<method>[^"]+)","selector":"(?<selector>[^"]+)"\}""")]
    private static partial Regex LocatorRegex();

    [GeneratedRegex(@"in\s+(?<file>.*?\.cs):line\s+(?<line>\d+)")]
    private static partial Regex SourceLocationRegex();
}
