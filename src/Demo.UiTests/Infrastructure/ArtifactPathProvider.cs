using System.Text;

namespace Demo.UiTests.Infrastructure;

public sealed class ArtifactPathProvider(string? root = null)
{
    private readonly string _root = root ?? ResolveArtifactsRoot();

    public string CreateArtifactPath(string folder, string testName, string extension)
    {
        var directory = Path.Combine(_root, folder);
        Directory.CreateDirectory(directory);

        var safeTestName = SanitizeFileName(testName);
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");

        return Path.Combine(directory, $"{safeTestName}_{timestamp}.{extension}");
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