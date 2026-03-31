using System.Text;
using System.Text.Json;

namespace LocatorHealing.Agent.Infrastructure;

internal sealed class JsonFailureDiagnosticsWriter
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    public string Write(FailureDiagnosticsArtifact artifact, string outputDirectory)
    {
        Directory.CreateDirectory(outputDirectory);

        var safeTestName = SanitizeFileName(artifact.TestName);
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var fileName = $"{safeTestName}_{timestamp}.json";
        var path = Path.Combine(outputDirectory, fileName);

        var json = JsonSerializer.Serialize(artifact, SerializerOptions);
        File.WriteAllText(path, json, Encoding.UTF8);

        return path;
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
