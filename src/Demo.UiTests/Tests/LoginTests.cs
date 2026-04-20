using Demo.UiTests.Infrastructure;
using Demo.UiTests.Pages;

namespace Demo.UiTests.Tests;

public sealed class LoginTests : TestBase
{
    [Test]
    public void StandardUserCanLogin()
    {
        var loginPage = new LoginPage(Driver);
        var inventoryPage = new InventoryPage(Driver);

        loginPage.Open();
        loginPage.Login("standard_user", "secret_sauce");

        Assert.That(inventoryPage.IsLoaded(), Is.True);
        Assert.That(inventoryPage.GetTitle(), Is.EqualTo("Products"));
    }

    [Test]
    public void LockedOutUserSeesExpectedError()
    {
        var loginPage = new LoginPage(Driver);

        loginPage.Open();
        loginPage.Login("locked_out_user", "secret_sauce");

        Assert.That(loginPage.GetErrorMessage(),
            Is.EqualTo("Epic sadface: Sorry, this user has been locked out."));
    }

    [Test]
    public void PerformanceGlitchUserCanLogin()
    {
        var loginPage = new LoginPage(Driver);
        var inventoryPage = new InventoryPage(Driver);

        loginPage.Open();
        loginPage.Login("performance_glitch_user", "secret_sauce");

        Assert.That(inventoryPage.IsLoaded(), Is.True);
        Assert.That(inventoryPage.GetTitle(), Is.EqualTo("Products"));
    }
}