namespace LocatorHealing.Agent.Contracts;

public sealed record PageObjectPatch(
    string PageObjectPath,
    string OldSelector,
    string NewSelector,
    string Strategy);
