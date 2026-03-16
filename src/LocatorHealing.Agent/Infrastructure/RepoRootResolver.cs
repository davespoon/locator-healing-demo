namespace LocatorHealing.Agent.Infrastructure;

public sealed class RepoRootResolver
{
    public string ResolveRepositoryRoot()
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

        return Directory.GetCurrentDirectory();
    }
}