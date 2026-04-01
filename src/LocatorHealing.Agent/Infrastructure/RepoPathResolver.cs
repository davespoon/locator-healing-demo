namespace LocatorHealing.Agent.Infrastructure;

internal sealed class RepoPathResolver
{
    private readonly string? _repositoryRoot = FindRepositoryRoot();

    public string? ToRepoRelativePath(string? fullPath)
    {
        if (string.IsNullOrWhiteSpace(fullPath) || string.IsNullOrWhiteSpace(_repositoryRoot))
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
        if (string.IsNullOrWhiteSpace(repoRelativePath) || string.IsNullOrWhiteSpace(_repositoryRoot))
        {
            return null;
        }

        var normalized = repoRelativePath.Replace('/', Path.DirectorySeparatorChar);
        return Path.GetFullPath(Path.Combine(_repositoryRoot, normalized));
    }

    private static string? FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            var hasGit = Directory.Exists(Path.Combine(current.FullName, ".git"));
            var hasSolution = File.Exists(Path.Combine(current.FullName, "LocatorHealingDemo.slnx"));

            if (hasGit || hasSolution)
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        return null;
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
