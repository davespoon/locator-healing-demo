using LocatorHealing.Agent.Contracts;
using LocatorHealing.Agent.Policies;

Console.WriteLine("LocatorHealing.Agent bootstrap");

var sample = new RepairWorkflowState
{
    Incident = new LocatorRepairIncident(
        TestName: "StandardUserCanLogin",
        Url: "https://www.saucedemo.com/",
        PageObjectClass: "LoginPage",
        MemberName: "UserName",
        LocatorName: "UserName",
        LocatorValue: "input[data-test='username']",
        ExceptionType: "OpenQA.Selenium.NoSuchElementException",
        ErrorTracePath: "artifacts/error-traces/sample.txt",
        DomSnapshotPath: "artifacts/dom-snapshots/sample.html")
};

var loopGuard = new LoopGuardPolicy();

if (!loopGuard.CanProceed(sample, DateTimeOffset.UtcNow, out var reason))
{
    Console.WriteLine($"Stopped: {reason}");
}
else
{
    Console.WriteLine("Ready to proceed to workflow execution.");
}