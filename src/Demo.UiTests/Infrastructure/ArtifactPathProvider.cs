using System.Text;

namespace Demo.UiTests.Infrastructure;

public sealed class ArtifactPathProvider
{
    public string CreateArtifactPath(string testName, string extension)
    {
        var root = ResolveWorkDirectory();

        var safeTestName = SanitizeFileName(testName);
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");

        return Path.Combine(root, $"{safeTestName}_{timestamp}.{extension}");
    }

    private static string ResolveWorkDirectory()
    {
        var workDirectory = TestContext.CurrentContext.WorkDirectory;

        if (string.IsNullOrWhiteSpace(workDirectory))
        {
            throw new InvalidOperationException(
                "NUnit WorkDirectory is not available.");
        }

        return Path.GetFullPath(workDirectory);
    }

    private static string SanitizeFileName(string value)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(value.Length);

        foreach (var ch in value)
        {
            builder.Append(invalidChars.Contains(ch) ? '_' : ch);
        }

        return builder.ToString();
    }
}