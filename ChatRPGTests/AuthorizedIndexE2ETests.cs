using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace ChatRPGTests;

[Collection("E2E collection")]
public class AuthorizedIndexE2ETests : IDisposable
{
    private readonly ChatRPGFixture _fixture;
    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;
    private readonly string _testUsername = "test";
    private readonly string _testPassword = "test";

    public AuthorizedIndexE2ETests(ChatRPGFixture fixture)
    {
        _fixture = fixture;
        _driver = E2ETestUtility.Setup("/");
        _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));

        // Login and prepare to test Authorized Index
        Login(_testUsername, _testPassword);
        Thread.Sleep(500); // wait for page to load fully
    }

    [Fact]
    public void AuthorizedIndexPage_ContainsNavbarLogo()
    {
        // Act
        IWebElement? actualSlogan = _wait.Until(webDriver => webDriver.FindElement(By.ClassName("site-icon")));

        // Assert
        Assert.True(actualSlogan.Displayed);
    }

    [Fact]
    public void AuthorizedIndexPage_ContainsManageButtonInNavbar()
    {
        // Act
        IWebElement? manageButton = _wait.Until(webDriver => webDriver.FindElement(By.Id("manage-button")));

        // Assert
        Assert.True(manageButton.Displayed);
    }

    [Fact]
    public void AuthorizedIndexPage_ContainsCorrectlyNamedUsernameManageButtonInNavbar()
    {
        // Arrange
        string expectedUsernameOnButton = _testUsername.ToUpper();

        // Act
        IWebElement? manageButton = _wait.Until(webDriver => webDriver.FindElement(By.Id("manage-button")));

        // Assert
        Assert.Equal(expectedUsernameOnButton, manageButton.Text);
    }

    [Fact]
    public void AuthorizedIndexPage_ClickingManageButton_ShouldRedirectToManageUserPage()
    {
        // Arrange
        string expectedUrl = "http://localhost:5111/Manage";

        // Act
        _wait.Until(webDriver => webDriver.FindElement(By.Id("manage-button"))).Click();
        bool urlRedirects = _wait.Until(webDriver => webDriver.Url == expectedUrl);

        // Assert
        Assert.True(urlRedirects);
    }

    [Fact]
    public void AuthorizedIndexPage_ContainsLogoutButtonInNavbar()
    {
        // Act
        IWebElement? manageButton = _wait.Until(webDriver => webDriver.FindElement(By.Id("logout-button")));

        // Assert
        Assert.True(manageButton.Displayed);
    }

    [Fact]
    public void AuthorizedIndexPage_ClickingLogoutButton_ShouldRedirectToUnauthorizedIndexPageWithSlogan()
    {
        // Arranged
        string expectedSlogan = "Immerse yourself in the ultimate AI-powered adventure!";

        // Act
        _wait.Until(webDriver => webDriver.FindElement(By.Id("logout-button"))).Click();
        Thread.Sleep(1200); // wait for typing animation to finish
        IWebElement? actualSlogan = _wait.Until(webDriver => webDriver.FindElement(By.ClassName("slogan-front")));

        // Assert
        Assert.Equal(expectedSlogan, actualSlogan.Text);
    }

    [Fact]
    public void AuthorizedIndexPage_Dashboard_ContainsCorrectDashboardTitle()
    {
        // Arranged
        string expectedDashboardTitle = "Dashboard";

        // Act
        IWebElement? actualDashboardTitle = _wait.Until(webDriver => webDriver.FindElement(By.ClassName("dashboard-title")));

        // Assert
        Assert.Equal(expectedDashboardTitle, actualDashboardTitle.Text);
    }

    [Fact]
    public void AuthorizedIndexPage_StartScenarios_ContainsSameAmountOfScenariosAsDisplayed()
    {
        // Arranged
        IWebElement? startScenariosContainer = _wait.Until(webDriver => webDriver.FindElement(By.Id("start-scenarios")));
        ReadOnlyCollection<IWebElement>? startScenarios = startScenariosContainer.FindElements(By.ClassName("card"));
        string expectedAmountOfScenarios = startScenarios.Count.ToString();

        // Act
        IWebElement? scenariosCounter = _wait.Until(webDriver => webDriver.FindElement(By.Id("scenarios-count")));
        Match match = Regex.Match(scenariosCounter.Text, @"\d+");
        string actualAmountOfScenarios = match.Value;

        // Assert
        Assert.Equal(expectedAmountOfScenarios, actualAmountOfScenarios);
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
    }
}
