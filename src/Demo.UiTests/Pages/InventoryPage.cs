using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;

namespace Demo.UiTests.Pages;

public sealed class InventoryPage
{
    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;

    public InventoryPage(IWebDriver driver)
    {
        _driver = driver;
        _wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
    }

    public bool IsLoaded()
    {
        _wait.Until(ExpectedConditions.UrlContains("inventory.html"));
        return _driver.Url.Contains("inventory.html", StringComparison.OrdinalIgnoreCase);
    }

    public string GetTitle()
    {
        var title = _wait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector("[data-test='title']")));
        return title.Text.Trim();
    }
}