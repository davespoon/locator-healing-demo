using Demo.UiTests.Infrastructure;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;

namespace Demo.UiTests.Pages;

public sealed class LoginPage
{
    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;
    private readonly TestBase _testBase;

    private const string UserNameLocatorName = "UserName";
    private const string UserNameLocatorValue = "input[data-test='username']";

    private const string PasswordLocatorName = "Password";
    private const string PasswordLocatorValue = "input[data-test='password']";

    private const string LoginButtonLocatorName = "LoginButton";
    private const string LoginButtonLocatorValue = "input[data-test='login-button']";

    public LoginPage(IWebDriver driver, TestBase testBase)
    {
        _driver = driver;
        _testBase = testBase;
        _wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
    }

    public void Open()
    {
        _driver.Navigate().GoToUrl("https://www.saucedemo.com/");
    }

    public void Login(string userName, string password)
    {
        var userNameInput = _testBase.CaptureLocatorFailure(
            UserNameLocatorName,
            UserNameLocatorValue,
            () => _wait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector(UserNameLocatorValue))));

        userNameInput.Clear();
        userNameInput.SendKeys(userName);

        var passwordInput = _testBase.CaptureLocatorFailure(
            PasswordLocatorName,
            PasswordLocatorValue,
            () => _wait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector(PasswordLocatorValue))));

        passwordInput.Clear();
        passwordInput.SendKeys(password);

        var loginButton = _testBase.CaptureLocatorFailure(
            LoginButtonLocatorName,
            LoginButtonLocatorValue,
            () => _wait.Until(ExpectedConditions.ElementToBeClickable(By.CssSelector(LoginButtonLocatorValue))));

        loginButton.Click();
    }
}