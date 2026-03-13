using LocatorHealing.Agent.Contracts;

namespace LocatorHealing.Agent.Policies;

public sealed class LoopGuardPolicy
{
    public const int MaxAttemptsPerFingerprint = 2;

    public bool CanProceed(RepairWorkflowState state, DateTimeOffset nowUtc, out string? reason)
    {
        if (state.AttemptCount >= MaxAttemptsPerFingerprint)
        {
            reason = "Max automated repair attempts reached for this locator fingerprint.";
            return false;
        }

        if (state.CooldownUntilUtc is not null && state.CooldownUntilUtc > nowUtc)
        {
            reason = $"Cooldown active until {state.CooldownUntilUtc:O}.";
            return false;
        }

        reason = null;
        return true;
    }
}