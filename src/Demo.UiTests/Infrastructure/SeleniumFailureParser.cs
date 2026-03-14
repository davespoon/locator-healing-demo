using System.Text.RegularExpressions;

namespace Demo.UiTests.Infrastructure;

public sealed class SeleniumFailureParser
{
    private static readonly Regex NestedExceptionRegex =
        new(@"---->\s*(?<type>[\w\.]+)\s*:", RegexOptions.Compiled);

    private static readonly Regex TopLevelExceptionRegex =
        new(@"^\s*(?<type>[\w\.]+)\s*:", RegexOptions.Compiled);

    private static readonly Regex LocatorRegex =
        new(
            """Unable to locate element:\s*\{"method":"(?<method>[^"]+)","selector":"(?<selector>[^"]+)"\}""",
            RegexOptions.Compiled);

    private static readonly Regex SourceLocationRegex =
        new(
            @"in\s+(?<file>.*?\.cs):line\s+(?<line>\d+)",
            RegexOptions.Compiled);

    public FailureDiagnostics Parse(
        string testName,
        string testFullName,
        string url,
        string outcomeStatus,
        string outcomeLabel,
        string message,
        string stackTrace)
    {
        var exceptionType = ExtractExceptionType(message);
        var locatorHint = ExtractLocatorHint(message);
        var sourceLocations = ExtractSourceLocations(stackTrace);

        var pageObjectLocation = sourceLocations
            .FirstOrDefault(location => location.FilePath.EndsWith("Page.cs", StringComparison.OrdinalIgnoreCase)
                                        || location.FilePath.Contains(
                                            $"{Path.DirectorySeparatorChar}Pages{Path.DirectorySeparatorChar}",
                                            StringComparison.OrdinalIgnoreCase)
                                        || location.FilePath.Contains("/Pages/", StringComparison.OrdinalIgnoreCase)
                                        || location.FilePath.Contains("\\Pages\\", StringComparison.OrdinalIgnoreCase));

        var testLocation = sourceLocations
            .FirstOrDefault(location => location.FilePath.EndsWith("Tests.cs", StringComparison.OrdinalIgnoreCase)
                                        || location.FilePath.Contains(
                                            $"{Path.DirectorySeparatorChar}Tests{Path.DirectorySeparatorChar}",
                                            StringComparison.OrdinalIgnoreCase)
                                        || location.FilePath.Contains("/Tests/", StringComparison.OrdinalIgnoreCase)
                                        || location.FilePath.Contains("\\Tests\\", StringComparison.OrdinalIgnoreCase));

        return new FailureDiagnostics(
            SchemaVersion: "1.0",
            GeneratedAtUtc: DateTimeOffset.UtcNow,
            TestName: testName,
            TestFullName: testFullName,
            Url: url,
            OutcomeStatus: outcomeStatus,
            OutcomeLabel: outcomeLabel,
            Message: message,
            StackTrace: stackTrace,
            ExceptionType: exceptionType,
            LocatorHint: locatorHint,
            PageObjectLocation: pageObjectLocation,
            TestLocation: testLocation);
    }

    private static string? ExtractExceptionType(string message)
    {
        var nestedMatch = NestedExceptionRegex.Match(message);
        if (nestedMatch.Success)
        {
            return nestedMatch.Groups["type"].Value;
        }

        var topLevelMatch = TopLevelExceptionRegex.Match(message);
        return topLevelMatch.Success
            ? topLevelMatch.Groups["type"].Value
            : null;
    }

    private static LocatorHint? ExtractLocatorHint(string message)
    {
        var match = LocatorRegex.Match(message);
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

        foreach (Match match in SourceLocationRegex.Matches(stackTrace))
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
}