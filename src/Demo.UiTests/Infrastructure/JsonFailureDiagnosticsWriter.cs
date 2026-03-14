using System.Text;
using System.Text.Json;

namespace Demo.UiTests.Infrastructure;

public sealed class JsonFailureDiagnosticsWriter(ArtifactPathProvider pathProvider) : IFailureDiagnosticsWriter
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    private readonly ArtifactPathProvider _pathProvider = pathProvider;

    public string Write(FailureDiagnostics diagnostics)
    {
        var path = _pathProvider.CreateArtifactPath("error-traces", diagnostics.TestName, "json");
        var json = JsonSerializer.Serialize(diagnostics, SerializerOptions);

        File.WriteAllText(path, json, Encoding.UTF8);
        return path;
    }
}