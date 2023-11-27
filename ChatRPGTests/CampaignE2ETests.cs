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

        // Login, create test campaign and prepare to test Campaign page
        E2ETestUtility.Login(_wait, _testUsername, _testPassword);
        E2ETestUtility.CreateTestCampaign(_driver, _wait, true);
        Thread.Sleep(500); // Ensure page has fully loaded
    }

    [Fact]
    public void CampaignPage_ContainsNavbarLogo()
    {
        // Act
        IWebElement? actualSlogan = _wait.Until(webDriver => webDriver.FindElement(By.ClassName("site-icon")));

        // Assert
        Assert.True(actualSlogan.Displayed);
    }

    [Fact]
    public void CampaignPage_ContainsManageButtonInNavbar()
    {
        // Act
        IWebElement? manageButton = _wait.Until(webDriver => webDriver.FindElement(By.Id("manage-button")));

        // Assert
        Assert.True(manageButton.Displayed);
    }

    [Fact]
    public void CampaignPage_ContainsCorrectlyNamedUsernameManageButtonInNavbar()
    {
        // Arrange
        string expectedUsernameOnButton = _testUsername.ToUpper();

        // Act
        IWebElement? manageButton = _wait.Until(webDriver => webDriver.FindElement(By.Id("manage-button")));

        // Assert
        Assert.Equal(expectedUsernameOnButton, manageButton.Text);
    }

    [Fact]
    public void CampaignPage_ClickingManageButton_ShouldRedirectToManageUserPage()
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
    public void CampaignPage_ContainsLogoutButtonInNavbar()
    {
        // Act
        IWebElement? manageButton = _wait.Until(webDriver => webDriver.FindElement(By.Id("logout-button")));

        // Assert
        Assert.True(manageButton.Displayed);
    }

    [Fact]
    public void CampaignPage_ClickingLogoutButton_ShouldRedirectToUnauthorizedIndexPageWithSlogan()
    {
        // Arranged
        string expectedSlogan = "Immerse yourself in the ultimate AI-powered adventure!";

        // Act
        Thread.Sleep(200);
        _wait.Until(webDriver => webDriver.FindElement(By.Id("logout-button"))).Click();
        Thread.Sleep(1200); // wait for typing animation to finish
        IWebElement? actualSlogan = _wait.Until(webDriver => webDriver.FindElement(By.ClassName("slogan-front")));

        // Assert
        Assert.Equal(expectedSlogan, actualSlogan.Text);
        E2ETestUtility.Login(_wait, _testUsername, _testPassword); // Log back in to teardown
    }

    [Fact]
    public void CampaignPage_Conversation_ContainsCorrectCampaignTitle()
    {
        // Arranged
        string expectedDashboardTitle = "Test Title";

        // Act
        IWebElement? actualDashboardTitle =
            _wait.Until(webDriver => webDriver.FindElement(By.ClassName("campaign-title")));

        // Assert
        Assert.Equal(expectedDashboardTitle, actualDashboardTitle.Text);
    }

    public void Dispose()
    {
        E2ETestUtility.RemoveTestCampaign(_driver, _wait);
        E2ETestUtility.Teardown(_driver);
        _driver.Dispose();
    }
}
