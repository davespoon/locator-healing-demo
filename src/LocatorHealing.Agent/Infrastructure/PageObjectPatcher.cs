using LocatorHealing.Agent.Contracts;

namespace LocatorHealing.Agent.Infrastructure;

internal static class PageObjectPatcher
{
    public static string Patch(
        string source,
        string oldSelector,
        string oldStrategy,
        CandidateLocator candidate,
        int pageObjectLineNumber)
    {
        var lines = source.Split('\n');
        var lineIndex = pageObjectLineNumber - 1;

        if (lineIndex < 0 || lineIndex >= lines.Length)
        {
            return source;
        }

        lines[lineIndex] = lines[lineIndex].Replace(
            $"\"{oldSelector}\"", $"\"{candidate.Value}\"", StringComparison.Ordinal);

        if (!string.Equals(oldStrategy, candidate.Strategy, StringComparison.OrdinalIgnoreCase)
            && GetByMethodName(oldStrategy) is { } oldMethod
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
