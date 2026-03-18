using System.Text;

namespace Demo.UiTests.Infrastructure;

public sealed class ArtifactWriter(ArtifactPathProvider pathProvider)
{
    private readonly ArtifactPathProvider _pathProvider = pathProvider;

    public string WriteDomSnapshot(string testName, string html)
    {
        var path = _pathProvider.CreateArtifactPath("dom-snapshots", testName, "html");
        File.WriteAllText(path, html, Encoding.UTF8);
        return path;
    }
}