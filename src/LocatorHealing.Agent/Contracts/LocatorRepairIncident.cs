namespace LocatorHealing.Agent.Contracts;

public sealed record LocatorRepairIncident(
    string TestName,
    string Url,
    string PageObjectClass,
    string MemberName,
    string LocatorName,
    string LocatorValue,
    string ExceptionType,
    string ErrorTracePath,
    string DomSnapshotPath);