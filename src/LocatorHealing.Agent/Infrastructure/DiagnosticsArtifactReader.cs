using System.Text.Json;
using LocatorHealing.Agent.Contracts;

namespace LocatorHealing.Agent.Infrastructure;

public sealed class DiagnosticsArtifactReader(RepoRootResolver repoRootResolver)
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public RepairWorkflowState Read(string diagnosticsFilePath)
    {
        if (string.IsNullOrWhiteSpace(diagnosticsFilePath))
        {
            throw new ArgumentException("Diagnostics file path is required.", nameof(diagnosticsFilePath));
        }

        var resolvedDiagnosticsPath = ResolvePath(diagnosticsFilePath);

        if (!File.Exists(resolvedDiagnosticsPath))
        {
            throw new FileNotFoundException("Diagnostics file was not found.", resolvedDiagnosticsPath);
        }

        var json = File.ReadAllText(resolvedDiagnosticsPath);
        var artifact = JsonSerializer.Deserialize<FailureDiagnosticsArtifact>(json, SerializerOptions)
                       ?? throw new InvalidOperationException("Failed to deserialize failure diagnostics JSON.");

        if (string.IsNullOrWhiteSpace(artifact.TestName))
        {
            throw new InvalidOperationException("Diagnostics payload is missing TestName.");
        }

        if (string.IsNullOrWhiteSpace(artifact.RepoRelativePageObjectPath))
        {
            throw new InvalidOperationException("Diagnostics payload is missing RepoRelativePageObjectPath.");
        }

        var incident = new LocatorRepairIncident(
            TestName: artifact.TestName,
            TestFullName: artifact.TestFullName,
            Url: artifact.Url,
            OuterExceptionType: artifact.OuterExceptionType,
            RootCauseExceptionType: artifact.RootCauseExceptionType,
            LocatorStrategy: artifact.LocatorHint?.Strategy,
            LocatorSelector: artifact.LocatorHint?.Selector,
            RepoRelativePageObjectPath: artifact.RepoRelativePageObjectPath,
            RepoRelativeTestPath: artifact.RepoRelativeTestPath,
            RepoRelativeDomSnapshotPath: artifact.DomSnapshotPath,
            RepoRelativeScreenshotPath: artifact.ScreenshotPath);

        return new RepairWorkflowState
        {
            DiagnosticsFilePath = NormalizeToRepoRelativeOrAbsolute(resolvedDiagnosticsPath),
            Incident = incident,
            ResolvedDomSnapshotPath = ResolveOptionalPath(artifact.DomSnapshotPath),
            ResolvedScreenshotPath = ResolveOptionalPath(artifact.ScreenshotPath),
            AttemptCount = 0
        };
    }

    private string ResolvePath(string path)
    {
        if (Path.IsPathRooted(path))
        {
            return Path.GetFullPath(path);
        }

        var repoRoot = repoRootResolver.ResolveRepositoryRoot();
        return Path.GetFullPath(Path.Combine(repoRoot, path));
    }

    private string? ResolveOptionalPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        return ResolvePath(path);
    }

    private string NormalizeToRepoRelativeOrAbsolute(string resolvedPath)
    {
        var repoRoot = repoRootResolver.ResolveRepositoryRoot();
        var normalizedRoot = EnsureTrailingSeparator(Path.GetFullPath(repoRoot));
        var normalizedPath = Path.GetFullPath(resolvedPath);

        if (normalizedPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
        {
            return normalizedPath[normalizedRoot.Length..]
                .Replace(Path.DirectorySeparatorChar, '/')
                .Replace(Path.AltDirectorySeparatorChar, '/');
        }

        return normalizedPath;
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