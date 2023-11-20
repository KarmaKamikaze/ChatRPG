using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace ChatRPGTests;

[Collection("E2E collection")]
public class CampaignE2ETests : IDisposable
{
    private readonly ChatRPGFixture _fixture;
    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;
    private readonly string _testUsername = "test";
    private readonly string _testPassword = "test";

    public CampaignE2ETests(ChatRPGFixture fixture)
    {
        _fixture = fixture;
        _driver = E2ETestUtility.Setup("/");
        _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));

        // Login and prepare to test Campaign page
        Login(_testUsername, _testPassword);
    }

    public void Dispose()
    {
        E2ETestUtility.Teardown(_driver);
        _driver.Dispose();
    }

    private void Login(string username, string password)
    {
        Thread.Sleep(1000); // wait for Index title animation to finish
        _wait.Until(webDriver => webDriver.FindElement(By.Id("login-button"))).Click();
        IWebElement? usernameForm = _wait.Until(webDriver => webDriver.FindElement(By.Id("username-form")));
        usernameForm.SendKeys(username);
        IWebElement? passwordForm = _wait.Until(webDriver => webDriver.FindElement(By.Id("password-form")));
        passwordForm.SendKeys(password);
        _wait.Until(webDriver => webDriver.FindElement(By.Id("login-submit"))).Submit();
        Thread.Sleep(500); // wait for page to load fully
    }
}
