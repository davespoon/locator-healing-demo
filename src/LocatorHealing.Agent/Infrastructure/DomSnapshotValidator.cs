using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using LocatorHealing.Agent.Contracts;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace LocatorHealing.Agent.Infrastructure;

public sealed partial class DomSnapshotValidator
{
    public async ValueTask<IReadOnlyList<CandidateValidationResult>> ValidateAsync(
        string domSnapshotHtml,
        string? brokenSelector,
        IReadOnlyList<CandidateLocator> candidates,
        CancellationToken cancellationToken = default)
    {
        var parser = new HtmlParser();
        var document = await parser.ParseDocumentAsync(domSnapshotHtml, cancellationToken);

        var expectedTagFromBrokenSelector = TryExtractLeadingTag(brokenSelector);
        var results = new List<CandidateValidationResult>();

        foreach (var candidate in candidates)
        {
            results.Add(ValidateCandidate(document, expectedTagFromBrokenSelector, candidate));
        }

        return results;
    }

    private static CandidateValidationResult ValidateCandidate(
        IDocument document,
        string? expectedTagFromBrokenSelector,
        CandidateLocator candidate)
    {
        var issues = new List<string>();

        AddRiskFlagIssues(candidate, issues);
        AddSelectorPatternIssues(candidate, issues);

        var matches = ResolveMatches(document, candidate, issues).ToList();
        var matchCount = matches.Count;

        if (matchCount == 0)
        {
            issues.Add("Selector did not match any element in the DOM snapshot.");
            return BuildResult(candidate, false, matchCount, null, null, issues);
        }

        if (matchCount > 1)
        {
            issues.Add("Selector matched more than one element in the DOM snapshot.");
            return BuildResult(candidate, false, matchCount, null, null, issues);
        }

        var element = matches[0];
        var matchedTag = element.TagName.ToLowerInvariant();
        var matchedText = NormalizeWhitespace(element.TextContent);

        var expectedTag = candidate.SemanticChecks.ExpectedTag ?? expectedTagFromBrokenSelector;
        if (!string.IsNullOrWhiteSpace(expectedTag) &&
            !string.Equals(expectedTag, matchedTag, StringComparison.OrdinalIgnoreCase))
        {
            issues.Add($"Expected tag '{expectedTag}' but matched '{matchedTag}'.");
        }

        if (!string.IsNullOrWhiteSpace(candidate.SemanticChecks.ExpectedText))
        {
            var expectedText = NormalizeWhitespace(candidate.SemanticChecks.ExpectedText);
            if (!string.Equals(expectedText, matchedText, StringComparison.OrdinalIgnoreCase))
            {
                issues.Add($"Expected text '{expectedText}' but matched '{matchedText}'.");
            }
        }

        if (!string.IsNullOrWhiteSpace(candidate.SemanticChecks.ExpectedRole))
        {
            var actualRole = element.GetAttribute("role");
            if (!string.Equals(candidate.SemanticChecks.ExpectedRole, actualRole, StringComparison.OrdinalIgnoreCase))
            {
                issues.Add(
                    $"Expected role '{candidate.SemanticChecks.ExpectedRole}' but matched '{actualRole ?? "<none>"}'.");
            }
        }

        if (!string.IsNullOrWhiteSpace(candidate.SemanticChecks.ExpectedNearbyLabel))
        {
            var actualLabel = FindAssociatedLabel(document, element);
            if (!string.Equals(
                    NormalizeWhitespace(candidate.SemanticChecks.ExpectedNearbyLabel),
                    NormalizeWhitespace(actualLabel),
                    StringComparison.OrdinalIgnoreCase))
            {
                issues.Add(
                    $"Expected nearby label '{candidate.SemanticChecks.ExpectedNearbyLabel}' but matched '{actualLabel ?? "<none>"}'.");
            }
        }

        var isValid = issues.Count == 0;
        return BuildResult(candidate, isValid, matchCount, matchedTag, matchedText, issues);
    }

    private static IEnumerable<IElement> ResolveMatches(
        IDocument document,
        CandidateLocator candidate,
        List<string> issues)
    {
        var strategy = candidate.Strategy.Trim().ToLowerInvariant();

        try
        {
            switch (strategy)
            {
                case "css selector":
                    return document.QuerySelectorAll(candidate.Value);

                case "id":
                {
                    var idValue = NormalizeIdValue(candidate.Value);
                    return document.All.Where(e => string.Equals(e.Id, idValue, StringComparison.Ordinal));
                }

                case "name":
                {
                    var nameValue = NormalizeNameValue(candidate.Value);
                    return document.All.Where(e =>
                        string.Equals(e.GetAttribute("name"), nameValue, StringComparison.Ordinal));
                }

                case "xpath":
                    issues.Add("XPath candidates are not supported by the current deterministic validator.");
                    return [];

                default:
                    issues.Add($"Unsupported locator strategy '{candidate.Strategy}' for deterministic validation.");
                    return [];
            }
        }
        catch (Exception ex)
        {
            issues.Add($"Selector evaluation failed: {ex.Message}");
            return [];
        }
    }

    private static void AddRiskFlagIssues(CandidateLocator candidate, List<string> issues)
    {
        foreach (var riskFlag in candidate.RiskFlags)
        {
            switch (riskFlag)
            {
                case "index_based":
                    issues.Add("Candidate is index-based, which is treated as brittle.");
                    break;

                case "dynamic_class":
                    issues.Add("Candidate relies on dynamic class semantics, which is treated as brittle.");
                    break;

                case "semantic_mismatch_risk":
                    issues.Add("Candidate carries a semantic mismatch risk.");
                    break;
            }
        }
    }

    private static void AddSelectorPatternIssues(CandidateLocator candidate, List<string> issues)
    {
        var strategy = candidate.Strategy.Trim().ToLowerInvariant();

        if (strategy == "css selector")
        {
            if (candidate.Value.Contains(":nth-child(", StringComparison.OrdinalIgnoreCase) ||
                candidate.Value.Contains(":nth-of-type(", StringComparison.OrdinalIgnoreCase))
            {
                issues.Add("Candidate uses a positional CSS selector, which is treated as brittle.");
            }
        }

        if (strategy == "xpath")
        {
            if (candidate.Value.StartsWith("/", StringComparison.Ordinal))
            {
                issues.Add("Absolute XPath is not allowed.");
            }

            if (candidate.Value.Contains("[1]", StringComparison.Ordinal) ||
                candidate.Value.Contains("[2]", StringComparison.Ordinal) ||
                candidate.Value.Contains("[3]", StringComparison.Ordinal))
            {
                issues.Add("XPath appears to rely on positional indexing, which is treated as brittle.");
            }
        }
    }

    private static CandidateValidationResult BuildResult(
        CandidateLocator candidate,
        bool isValid,
        int matchCount,
        string? matchedTag,
        string? matchedText,
        IReadOnlyList<string> issues)
    {
        var summary = isValid
            ? "Candidate passed deterministic DOM validation."
            : string.Join(" ", issues);

        return new CandidateValidationResult(
            Candidate: candidate,
            IsValid: isValid,
            MatchCount: matchCount,
            Summary: summary,
            MatchedTag: matchedTag,
            MatchedText: matchedText,
            Issues: issues);
    }

    private static string? FindAssociatedLabel(IDocument document, IElement element)
    {
        if (element.ParentElement is not null &&
            string.Equals(element.ParentElement.TagName, "LABEL", StringComparison.OrdinalIgnoreCase))
        {
            return element.ParentElement.TextContent;
        }

        var id = element.Id;
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        var label = document.QuerySelector($"label[for='{id}']");
        return label?.TextContent;
    }

    private static string NormalizeWhitespace(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return MultiWhitespaceRegex().Replace(value.Trim(), " ");
    }

    private static string NormalizeIdValue(string value)
    {
        return value.StartsWith("#", StringComparison.Ordinal) ? value[1..] : value;
    }

    private static string NormalizeNameValue(string value)
    {
        var match = NameSelectorRegex().Match(value);
        return match.Success ? match.Groups["value"].Value : value;
    }

    private static string? TryExtractLeadingTag(string? selector)
    {
        if (string.IsNullOrWhiteSpace(selector))
        {
            return null;
        }

        var match = LeadingTagRegex().Match(selector);
        return match.Success ? match.Groups["tag"].Value.ToLowerInvariant() : null;
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex MultiWhitespaceRegex();

    [GeneratedRegex(@"^\[name=(['""]?)(?<value>[^'""]+)\1\]$", RegexOptions.IgnoreCase)]
    private static partial Regex NameSelectorRegex();

    [GeneratedRegex(@"^(?<tag>[a-zA-Z][\w\-]*)")]
    private static partial Regex LeadingTagRegex();
}