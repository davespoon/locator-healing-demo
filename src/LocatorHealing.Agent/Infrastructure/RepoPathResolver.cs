namespace LocatorHealing.Agent.Infrastructure;

internal sealed class RepoPathResolver
{
    private readonly string _repositoryRoot;

    public RepoPathResolver(string repositoryRoot)
    {
        if (string.IsNullOrWhiteSpace(repositoryRoot))
        {
            throw new ArgumentException("Repository root is required.", nameof(repositoryRoot));
        }

        var fullPath = Path.GetFullPath(repositoryRoot);

        if (!Directory.Exists(fullPath))
        {
            throw new DirectoryNotFoundException($"Repository root was not found: {fullPath}");
        }

        _repositoryRoot = fullPath;
    }

    public string? ToRepoRelativePath(string? fullPath)
    {
        if (string.IsNullOrWhiteSpace(fullPath))
        {
            return null;
        }

        var normalizedRoot = EnsureTrailingSeparator(Path.GetFullPath(_repositoryRoot));
        var normalizedPath = Path.GetFullPath(fullPath);

        if (!normalizedPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return normalizedPath[normalizedRoot.Length..]
            .Replace(Path.DirectorySeparatorChar, '/')
            .Replace(Path.AltDirectorySeparatorChar, '/');
    }

    public string? ToAbsolutePath(string? repoRelativePath)
    {
        if (string.IsNullOrWhiteSpace(repoRelativePath))
        {
            return null;
        }

        if (Path.IsPathRooted(repoRelativePath))
        {
            return Path.GetFullPath(repoRelativePath);
        }

        var normalized = repoRelativePath.Replace('/', Path.DirectorySeparatorChar);
        return Path.GetFullPath(Path.Combine(_repositoryRoot, normalized));
    }

    private static string EnsureTrailingSeparator(string path)
    {
        if (path.EndsWith(Path.DirectorySeparatorChar) || path.EndsWith(Path.AltDirectorySeparatorChar))
        {
            return path;
        }

        return path + Path.DirectorySeparatorChar;
    }
}