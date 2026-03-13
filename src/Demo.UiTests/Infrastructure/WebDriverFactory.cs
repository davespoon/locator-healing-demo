using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace Demo.UiTests.Infrastructure;

public static class WebDriverFactory
{
    public static IWebDriver CreateChrome()
    {
        var options = new ChromeOptions();

        if (Environment.GetEnvironmentVariable("CI") == "true")
        {
            options.AddArgument("--headless=new");
            options.AddArgument("--no-sandbox");
            options.AddArgument("--disable-dev-shm-usage");
            options.AddArgument("--window-size=1920,1080");
        }

        return new ChromeDriver(options);
    }
}