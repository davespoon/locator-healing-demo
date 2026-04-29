using LocatorHealing.Agent.Executors;
using LocatorHealing.Agent.Infrastructure;
using LocatorHealing.Agent.Policies;

namespace LocatorHealing.Agent.Application;

internal sealed class ExecutorFactory(string repoRoot, string outputDirectory)
{
    public ResultsParseExecutor ResultsParse { get; } =
        new(new NUnitResultParser());

    public DiagnosticsPreparationExecutor DiagnosticsPreparation { get; } =
        new(new SeleniumFailureParser(new RepoPathResolver(repoRoot)),
            new JsonFailureDiagnosticsWriter(),
            outputDirectory);

    public FailureIngestExecutor FailureIngest { get; } =
        new(new DiagnosticsArtifactReader());

    public LoopGuardExecutor LoopGuard { get; } =
        new(new LoopGuardPolicy(TimeProvider.System));

    public LocatorFailureCheckExecutor LocatorFailureCheck { get; } = new();

    public StopExecutor Stop { get; } = new();

    public CandidateGenerationExecutor CandidateGeneration { get; } =
        new(new CandidateAgentFactory().Create());

    public PageObjectPatchExecutor PageObjectPatch { get; } =
        new(new RepoPathResolver(repoRoot));
}