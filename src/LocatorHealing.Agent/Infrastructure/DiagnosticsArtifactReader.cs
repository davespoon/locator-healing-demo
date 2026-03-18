using System.Text.Json;
using LocatorHealing.Agent.Contracts;

namespace LocatorHealing.Agent.Infrastructure;

public sealed class DiagnosticsArtifactReader
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

        var resolvedDiagnosticsPath = Path.GetFullPath(diagnosticsFilePath);

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

        var incident = new LocatorRepairIncident(
            TestName: artifact.TestName,
            TestFullName: artifact.TestFullName,
            Url: artifact.Url,
            OuterExceptionType: artifact.OuterExceptionType,
            RootCauseExceptionType: artifact.RootCauseExceptionType,
            LocatorStrategy: artifact.LocatorHint?.Strategy,
            LocatorSelector: artifact.LocatorHint?.Selector,
            RepoRelativePageObjectPath: artifact.RepoRelativePageObjectPath,
            PageObjectLineNumber: artifact.PageObjectLocation?.LineNumber,
            RepoRelativeTestPath: artifact.RepoRelativeTestPath,
            TestLineNumber: artifact.TestLocation?.LineNumber,
            RepoRelativeDomSnapshotPath: artifact.DomSnapshotPath);

        return new RepairWorkflowState
        {
            DiagnosticsFilePath = resolvedDiagnosticsPath,
            Incident = incident,
            ResolvedDomSnapshotPath = ResolveArtifactPath(artifact.DomSnapshotPath, resolvedDiagnosticsPath),
            AttemptCount = 0
        };
    }

    private static string? ResolveArtifactPath(string? artifactPath, string resolvedDiagnosticsPath)
    {
        if (string.IsNullOrWhiteSpace(artifactPath))
        {
            return null;
        }

        if (Path.IsPathRooted(artifactPath))
        {
            return Path.GetFullPath(artifactPath);
        }

        var diagnosticsDirectory = new DirectoryInfo(
            Path.GetDirectoryName(resolvedDiagnosticsPath)
            ?? throw new InvalidOperationException("Diagnostics file path does not contain a directory."));

        for (DirectoryInfo? current = diagnosticsDirectory; current is not null; current = current.Parent)
        {
            var candidate = Path.GetFullPath(Path.Combine(current.FullName, artifactPath));
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return Path.GetFullPath(Path.Combine(diagnosticsDirectory.FullName, artifactPath));
    }
}