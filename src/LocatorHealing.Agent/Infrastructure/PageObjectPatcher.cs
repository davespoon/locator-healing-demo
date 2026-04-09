using LocatorHealing.Agent.Contracts;

namespace LocatorHealing.Agent.Infrastructure;

internal static class PageObjectPatcher
{
    public static string Patch(
        string source,
        LocatorRepairIncident incident,
        CandidateLocator candidate)
    {
        if (incident.PageObjectLineNumber is not { } lineNumber)
        {
            return source;
        }

        var lines = source.Split('\n');
        var lineIndex = lineNumber - 1;

        if (lineIndex < 0 || lineIndex >= lines.Length)
        {
            return source;
        }

        lines[lineIndex] = lines[lineIndex].Replace(
            $"\"{incident.LocatorSelector}\"", $"\"{candidate.Value}\"", StringComparison.Ordinal);

        if (!string.Equals(incident.LocatorStrategy, candidate.Strategy, StringComparison.OrdinalIgnoreCase)
            && GetByMethodName(incident.LocatorStrategy!) is { } oldMethod
            && GetByMethodName(candidate.Strategy) is { } newMethod)
        {
            lines[lineIndex] = lines[lineIndex].Replace(
                $"By.{oldMethod}(", $"By.{newMethod}(", StringComparison.Ordinal);
        }

        return string.Join('\n', lines);
    }

    private static string? GetByMethodName(string strategy) =>
        strategy.ToLowerInvariant() switch
        {
            "css selector" => "CssSelector",
            "id" => "Id",
            "name" => "Name",
            "xpath" => "XPath",
            "class name" => "ClassName",
            "tag name" => "TagName",
            "link text" => "LinkText",
            "partial link text" => "PartialLinkText",
            _ => null
        };
}