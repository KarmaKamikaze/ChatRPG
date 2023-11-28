using System.Collections.ObjectModel;
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
        // Arrange
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
    public void CampaignPage_GameStats_StartScenarioTitleIsDisplayed()
    {
        // Arrange
        IWebElement? gameStatsContainer =
            _wait.Until(webDriver => webDriver.FindElement(By.ClassName("game-stats")));
        ReadOnlyCollection<IWebElement>? gameStatsTitles = gameStatsContainer.FindElements(By.TagName("h3"));

        // Act
        IWebElement startScenarioTitle = gameStatsTitles[0];

        // Assert
        Assert.True(startScenarioTitle.Displayed);
    }

    [Fact]
    public void CampaignPage_GameStats_StartScenarioTitleDisplaysCorrectTitle()
    {
        // Arrange
        string expectedTitle = "Start Scenario";
        IWebElement? gameStatsContainer =
            _wait.Until(webDriver => webDriver.FindElement(By.ClassName("game-stats")));
        ReadOnlyCollection<IWebElement>? gameStatsTitles = gameStatsContainer.FindElements(By.TagName("h3"));

        // Act
        IWebElement actualStartScenarioTitle = gameStatsTitles[0];

        // Assert
        Assert.Equal(expectedTitle, actualStartScenarioTitle.Text);
    }

    [Fact]
    public void CampaignPage_GameStats_LocationTitleIsDisplayed()
    {
        // Arrange
        IWebElement? gameStatsContainer =
            _wait.Until(webDriver => webDriver.FindElement(By.ClassName("game-stats")));
        ReadOnlyCollection<IWebElement>? gameStatsTitles = gameStatsContainer.FindElements(By.TagName("h3"));

        // Act
        IWebElement locationTitle = gameStatsTitles[1];

        // Assert
        Assert.True(locationTitle.Displayed);
    }

    [Fact]
    public void CampaignPage_GameStats_LocationTitleDisplaysCorrectTitle()
    {
        // Arrange
        string expectedTitle = "Location";
        IWebElement? gameStatsContainer =
            _wait.Until(webDriver => webDriver.FindElement(By.ClassName("game-stats")));
        ReadOnlyCollection<IWebElement>? gameStatsTitles = gameStatsContainer.FindElements(By.TagName("h3"));

        // Act
        IWebElement actualLocationTitle = gameStatsTitles[1];

        // Assert
        Assert.Equal(expectedTitle, actualLocationTitle.Text);
    }

    [Fact]
    public void CampaignPage_GameStats_CharactersTitleIsDisplayed()
    {
        // Arrange
        IWebElement? gameStatsContainer =
            _wait.Until(webDriver => webDriver.FindElement(By.ClassName("game-stats")));
        ReadOnlyCollection<IWebElement>? gameStatsTitles = gameStatsContainer.FindElements(By.TagName("h3"));

        // Act
        IWebElement charactersTitle = gameStatsTitles[2];

        // Assert
        Assert.True(charactersTitle.Displayed);
    }

    [Fact]
    public void CampaignPage_GameStats_CharactersTitleDisplaysCorrectTitle()
    {
        // Arrange
        string expectedTitle = "Characters";
        IWebElement? gameStatsContainer =
            _wait.Until(webDriver => webDriver.FindElement(By.ClassName("game-stats")));
        ReadOnlyCollection<IWebElement>? gameStatsTitles = gameStatsContainer.FindElements(By.TagName("h3"));

        // Act
        IWebElement actualCharactersTitle = gameStatsTitles[2];

        // Assert
        Assert.Equal(expectedTitle, actualCharactersTitle.Text);
    }

    [Fact]
    public void CampaignPage_GameStats_StartScenarioHasCorrectScenarioTitle()
    {
        // Arrange
        string expectedScenarioTitle = "Test Title";
        IWebElement? gameStatsContainer =
            _wait.Until(webDriver => webDriver.FindElement(By.ClassName("game-stats")));

        // Act
        IWebElement actualScenarioTitle = gameStatsContainer.FindElement(By.Id("scenario-title"));

        // Assert
        Assert.Equal(expectedScenarioTitle, actualScenarioTitle.Text);
    }

    [Fact]
    public void CampaignPage_GameStats_StartScenarioHasCorrectScenarioDescription()
    {
        // Arrange
        string expectedScenarioDescription = "Test Scenario";
        IWebElement? gameStatsContainer =
            _wait.Until(webDriver => webDriver.FindElement(By.ClassName("game-stats")));

        // Act
        IWebElement actualScenarioDescription = gameStatsContainer.FindElement(By.Id("scenario-description"));

        // Assert
        Assert.Equal(expectedScenarioDescription, actualScenarioDescription.Text);
    }

    [Fact]
    public void CampaignPage_GameStats_LocationHasCorrectLocationName()
    {
        // Arrange
        string expectedLocationName = "Start location";
        IWebElement? gameStatsContainer =
            _wait.Until(webDriver => webDriver.FindElement(By.ClassName("game-stats")));

        // Act
        IWebElement actualLocationName = gameStatsContainer.FindElement(By.Id("location-name"));

        // Assert
        Assert.Equal(expectedLocationName, actualLocationName.Text);
    }

    [Fact]
    public void CampaignPage_GameStats_LocationHasCorrectLocationDescription()
    {
        // Arrange
        string expectedLocationDescription = "The place where it all began";
        IWebElement? gameStatsContainer =
            _wait.Until(webDriver => webDriver.FindElement(By.ClassName("game-stats")));

        // Act
        IWebElement actualLocationDescription = gameStatsContainer.FindElement(By.Id("location-description"));

        // Assert
        Assert.Equal(expectedLocationDescription, actualLocationDescription.Text);
    }

    [Fact]
    public void CampaignPage_Conversation_ContainsCorrectCampaignTitle()
    {
        // Arrange
        string expectedDashboardTitle = "Test Title";

        // Act
        IWebElement? actualDashboardTitle =
            _wait.Until(webDriver => webDriver.FindElement(By.ClassName("campaign-title")));

        // Assert
        Assert.Equal(expectedDashboardTitle, actualDashboardTitle.Text);
    }

    [Theory]
    [InlineData("do-prompt", true)]
    [InlineData("say-prompt", false)]
    [InlineData("attack-prompt", false)]
    public void CampaignPage_Conversation_DoUserPromptTypeChosenAsDefaultOnCampaignStart(string userPromptTypeId, bool expectedActive)
    {
        // Arrange
        IWebElement? conversationContainer =
            _wait.Until(webDriver => webDriver.FindElement(By.ClassName("conversation")));
        IWebElement prompt = conversationContainer.FindElement(By.Id(userPromptTypeId));
        string? promptClasses = prompt.GetAttribute("class");

        // Act
        bool actuallyActive = promptClasses.Contains("rz-state-active");

        // Assert
        Assert.Equal(expectedActive, actuallyActive);
    }

    [Theory]
    [InlineData("do-prompt", true)]
    [InlineData("say-prompt", false)]
    [InlineData("attack-prompt", false)]
    public void CampaignPage_Conversation_DoUserPromptTypeSelectedWhenClicked(string userPromptTypeId, bool expectedActive)
    {
        // Arrange
        IWebElement? conversationContainer =
            _wait.Until(webDriver => webDriver.FindElement(By.ClassName("conversation")));
        IWebElement doPrompt = conversationContainer.FindElement(By.Id("do-prompt"));
        IWebElement prompt = conversationContainer.FindElement(By.Id(userPromptTypeId));

        // Act
        doPrompt.Click();
        string? promptClasses = prompt.GetAttribute("class");
        bool actuallyActive = promptClasses.Contains("rz-state-active");

        // Assert
        Assert.Equal(expectedActive, actuallyActive);
    }

    [Theory]
    [InlineData("do-prompt", false)]
    [InlineData("say-prompt", true)]
    [InlineData("attack-prompt", false)]
    public void CampaignPage_Conversation_SayUserPromptTypeSelectedWhenClicked(string userPromptTypeId, bool expectedActive)
    {
        // Arrange
        IWebElement? conversationContainer =
            _wait.Until(webDriver => webDriver.FindElement(By.ClassName("conversation")));
        IWebElement sayPrompt = conversationContainer.FindElement(By.Id("say-prompt"));
        IWebElement prompt = conversationContainer.FindElement(By.Id(userPromptTypeId));

        // Act
        sayPrompt.Click();
        string? promptClasses = prompt.GetAttribute("class");
        bool actuallyActive = promptClasses.Contains("rz-state-active");

        // Assert
        Assert.Equal(expectedActive, actuallyActive);
    }

    [Theory]
    [InlineData("do-prompt", false)]
    [InlineData("say-prompt", false)]
    [InlineData("attack-prompt", true)]
    public void CampaignPage_Conversation_AttackUserPromptTypeSelectedWhenClicked(string userPromptTypeId, bool expectedActive)
    {
        // Arrange
        IWebElement? conversationContainer =
            _wait.Until(webDriver => webDriver.FindElement(By.ClassName("conversation")));
        IWebElement attackPrompt = conversationContainer.FindElement(By.Id("attack-prompt"));
        IWebElement prompt = conversationContainer.FindElement(By.Id(userPromptTypeId));

        // Act
        attackPrompt.Click();
        string? promptClasses = prompt.GetAttribute("class");
        bool actuallyActive = promptClasses.Contains("rz-state-active");

        // Assert
        Assert.Equal(expectedActive, actuallyActive);
    }

    [Theory]
    [InlineData("do-prompt", "What do you do?")]
    [InlineData("say-prompt", "What do you say?")]
    [InlineData("attack-prompt", "How do you attack?")]
    public void CampaignPage_Conversation_UserPromptTypeDisplaysCorrectPromptTypePlaceholderInInputField(string userPromptTypeId, string expectedPromptPlaceholder)
    {
        // Arrange
        IWebElement? conversationContainer =
            _wait.Until(webDriver => webDriver.FindElement(By.ClassName("conversation")));
        IWebElement prompt = conversationContainer.FindElement(By.Id(userPromptTypeId));

        // Act
        prompt.Click();
        IWebElement? inputField =
            _wait.Until(webDriver => webDriver.FindElement(By.ClassName("user-prompt")));
        string expectedPromptTypePlaceholder = inputField.GetAttribute("placeholder");

        // Assert
        Assert.Equal(expectedPromptPlaceholder, expectedPromptTypePlaceholder);
    }

    public void Dispose()
    {
        E2ETestUtility.RemoveTestCampaign(_driver, _wait);
        E2ETestUtility.Teardown(_driver);
        _driver.Dispose();
    }
}
