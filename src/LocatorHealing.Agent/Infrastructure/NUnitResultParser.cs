using System.Xml.Linq;

namespace LocatorHealing.Agent.Infrastructure;

internal sealed class NUnitResultParser
{
    public IReadOnlyList<TestFailureInfo> ParseFailuresFromDirectory(string directoryPath)
    {
        var xmlFiles = Directory.GetFiles(directoryPath, "*.xml");
        var allFailures = new List<TestFailureInfo>();

        foreach (var file in xmlFiles)
        {
            allFailures.AddRange(ParseFailures(file));
        }

        return allFailures;
    }

    public IReadOnlyList<TestFailureInfo> ParseFailures(string resultsFilePath)
    {
        var doc = XDocument.Load(resultsFilePath);
        var failures = new List<TestFailureInfo>();

        foreach (var testCase in doc.Descendants("test-case"))
        {
            var result = testCase.Attribute("result")?.Value;

            if (!string.Equals(result, "Failed", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var name = testCase.Attribute("name")?.Value ?? string.Empty;
            var fullName = testCase.Attribute("fullname")?.Value ?? string.Empty;

            var failure = testCase.Element("failure");
            var message = failure?.Element("message")?.Value ?? string.Empty;
            var stackTrace = failure?.Element("stack-trace")?.Value ?? string.Empty;

            var output = testCase.Element("output")?.Value;

            var attachments = ParseAttachments(testCase);

            var domSnapshotPath = attachments
                .FirstOrDefault(a => a.Description.Contains("DOM", StringComparison.OrdinalIgnoreCase))
                ?.FilePath;

            var screenshotPath = attachments
                .FirstOrDefault(a => a.Description.Contains("Screenshot", StringComparison.OrdinalIgnoreCase))
                ?.FilePath;

            failures.Add(new TestFailureInfo(
                TestName: name,
                TestFullName: fullName,
                Message: message,
                StackTrace: stackTrace,
                Output: output,
                DomSnapshotPath: domSnapshotPath,
                ScreenshotPath: screenshotPath));
        }

        return failures;
    }

    private static List<AttachmentInfo> ParseAttachments(XElement testCase)
    {
        var attachments = new List<AttachmentInfo>();

        var attachmentsElement = testCase.Element("attachments");

        if (attachmentsElement is null)
        {
            return attachments;
        }

        foreach (var attachment in attachmentsElement.Elements("attachment"))
        {
            var filePath = attachment.Element("filePath")?.Value;
            var description = attachment.Element("description")?.Value ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(filePath))
            {
                attachments.Add(new AttachmentInfo(filePath, description));
            }
        }

        return attachments;
    }

    private sealed record AttachmentInfo(string FilePath, string Description);
}
