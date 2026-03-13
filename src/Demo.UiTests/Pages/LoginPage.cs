using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;

namespace Demo.UiTests.Pages;

public sealed class LoginPage
{
    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;

    private const string UserNameLocatorValue = "input[data-test='usern@me']";
    private const string PasswordLocatorValue = "input[data-test='password']";
    private const string LoginButtonLocatorValue = "input[data-test='login-button']";

    public LoginPage(IWebDriver driver)
    {
        _driver = driver;
        _wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
    }

    public void Open()
    {
        _driver.Navigate().GoToUrl("https://www.saucedemo.com/");
    }

    public void Login(string userName, string password)
    {
        var userNameInput = _wait.Until(
            ExpectedConditions.ElementIsVisible(By.CssSelector(UserNameLocatorValue)));

        userNameInput.Clear();
        userNameInput.SendKeys(userName);

        var passwordInput = _wait.Until(
            ExpectedConditions.ElementIsVisible(By.CssSelector(PasswordLocatorValue)));

        passwordInput.Clear();
        passwordInput.SendKeys(password);

        var loginButton = _wait.Until(
            ExpectedConditions.ElementToBeClickable(By.CssSelector(LoginButtonLocatorValue)));

        loginButton.Click();
    }
}