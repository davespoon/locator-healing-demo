using Demo.UiTests.Infrastructure;
using Demo.UiTests.Pages;

namespace Demo.UiTests.Tests;

public sealed class LoginTests : TestBase
{
    [Test]
    public void StandardUserCanLogin()
    {
        var loginPage = new LoginPage(Driver, this);
        var inventoryPage = new InventoryPage(Driver);

        loginPage.Open();
        loginPage.Login("standard_user", "secret_sauce");

        Assert.That(inventoryPage.IsLoaded(), Is.True);
        Assert.That(inventoryPage.GetTitle(), Is.EqualTo("Products"));
    }
}