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
        IWebElement? actualDashboardTitle =
            _wait.Until(webDriver => webDriver.FindElement(By.ClassName("dashboard-title")));

        // Assert
        Assert.Equal(expectedDashboardTitle, actualDashboardTitle.Text);
    }

    [Fact]
    public void AuthorizedIndexPage_YourCampaigns_ContainsSameAmountOfCampaignsAsDisplayed()
    {
        // Arranged
        IWebElement? campaignsContainer = _wait.Until(webDriver => webDriver.FindElement(By.Id("your-campaigns")));
        ReadOnlyCollection<IWebElement>? campaigns = campaignsContainer.FindElements(By.ClassName("card"));
        string expectedAmountOfScenarios = campaigns.Count.ToString();

        // Act
        IWebElement? campaignsCounter = _wait.Until(webDriver => webDriver.FindElement(By.Id("campaigns-count")));
        string actualAmountOfScenarios = Regex.Match(campaignsCounter.Text, @"\d+").Value;

        // Assert
        Assert.Equal(expectedAmountOfScenarios, actualAmountOfScenarios);
    }

    [Fact]
    public void AuthorizedIndexPage_StartScenarios_ContainsSameAmountOfScenariosAsDisplayed()
    {
        // Arrange
        IWebElement? startScenariosContainer =
            _wait.Until(webDriver => webDriver.FindElement(By.Id("start-scenarios")));
        ReadOnlyCollection<IWebElement>? startScenarios = startScenariosContainer.FindElements(By.ClassName("card"));
        int expectedAmountOfScenarios = startScenarios.Count;

        // Act
        IWebElement? scenariosCounter = _wait.Until(webDriver => webDriver.FindElement(By.Id("scenarios-count")));
        string actualAmountOfScenarios = Regex.Match(scenariosCounter.Text, @"\d+").Value;

        // Assert
        Assert.Equal(expectedAmountOfScenarios.ToString(), actualAmountOfScenarios);
    }

    [Fact]
    public void AuthorizedIndexPage_StartScenarios_ContainsCorrectAmountOfPredefinedStartScenarios()
    {
        // Arrange
        int expectedAmountOfStartScenarios = 5;

        // Act
        IWebElement? scenariosCounter = _wait.Until(webDriver => webDriver.FindElement(By.Id("scenarios-count")));
        string actualAmountOfStartScenarios = Regex.Match(scenariosCounter.Text, @"\d+").Value;

        // Assert
        Assert.Equal(expectedAmountOfStartScenarios.ToString(), actualAmountOfStartScenarios);
    }

    [Theory]
    [MemberData(nameof(StartScenarioTitles))]
    public void
        AuthorizedIndexPage_StartScenarios_CorrectScenarioTitlesAppearInScenarioComponents(string expectedTitle)
    {
        // Arrange
        IWebElement? startScenariosContainer =
            _wait.Until(webDriver => webDriver.FindElement(By.Id("start-scenarios")));
        ReadOnlyCollection<IWebElement>? startScenarios = startScenariosContainer.FindElements(By.ClassName("card"));

        // Act
        string? actualTitle = startScenarios.Select(s => s.FindElement(By.TagName("h5")).Text)
            .FirstOrDefault(title => title == expectedTitle);

        // Assert
        Assert.Equal(expectedTitle, actualTitle);
    }

    [Theory]
    [MemberData(nameof(StartScenarioDescriptions))]
    public void AuthorizedIndexPage_StartScenarios_CorrectScenarioDescriptionsAppearInScenarioComponents(
        string expectedDescription)
    {
        // Arrange
        IWebElement? startScenariosContainer =
            _wait.Until(webDriver => webDriver.FindElement(By.Id("start-scenarios")));
        ReadOnlyCollection<IWebElement>? startScenarios = startScenariosContainer.FindElements(By.ClassName("card"));

        // Act
        string? actualDescription = startScenarios.Select(s => s.FindElement(By.TagName("p")).Text)
            .FirstOrDefault(title => title == expectedDescription);

        // Assert
        Assert.Equal(expectedDescription, actualDescription);
    }

    [Theory]
    [MemberData(nameof(StartScenarioTitles))]
    public void
        AuthorizedIndexPage_StartScenarios_ClickingScenariosPlacesCorrectTitleInCustomCampaignComponent(
            string expectedTitle)
    {
        // Arrange
        IWebElement? startScenariosContainer =
            _wait.Until(webDriver => webDriver.FindElement(By.Id("start-scenarios")));
        ReadOnlyCollection<IWebElement>? startScenarios = startScenariosContainer.FindElements(By.Id("scenario-card"));

        // Act
        foreach (IWebElement scenario in startScenarios)
        {
            IWebElement h5Element = scenario.FindElement(By.TagName("h5"));
            if (h5Element.Text == expectedTitle)
            {
                scenario.Click();
            }
        }

        string? actualTitle = _wait.Until(webDriver => webDriver.FindElement(By.Id("inputCampaignTitle")))
            .GetAttribute("value");

        // Assert
        Assert.Equal(expectedTitle, actualTitle);
    }

    [Theory]
    [MemberData(nameof(StartScenarioDescriptions))]
    public void AuthorizedIndexPage_StartScenarios_ClickingScenariosPlacesCorrectDescriptionInCustomCampaignComponent(
        string expectedDescription)
    {
        // Arrange
        IWebElement? startScenariosContainer =
            _wait.Until(webDriver => webDriver.FindElement(By.Id("start-scenarios")));
        ReadOnlyCollection<IWebElement>? startScenarios = startScenariosContainer.FindElements(By.Id("scenario-card"));

        // Act
        foreach (IWebElement scenario in startScenarios)
        {
            IWebElement h5Element = scenario.FindElement(By.TagName("p"));
            if (h5Element.Text == expectedDescription)
            {
                scenario.Click();
            }
        }

        string? actualDescription =
            _wait.Until(webDriver => webDriver.FindElement(By.Id("inputCustomStartScenario"))).GetAttribute("value");

        // Assert
        Assert.Equal(expectedDescription, actualDescription);
    }

    [Fact]
    public void AuthorizedIndexPage_CustomCampaign_CampaignTitleIsEmptyOnInitialization()
    {
        // Arrange
        string expectedTitle = string.Empty;

        // Act
        IWebElement? campaignTitle = _wait.Until(webDriver => webDriver.FindElement(By.Id("inputCampaignTitle")));
        string actualTitle = campaignTitle.GetAttribute("value");

        // Assert
        Assert.Equal(expectedTitle, actualTitle);
    }

    [Fact]
    public void AuthorizedIndexPage_CustomCampaign_CharacterNameIsEmptyOnInitialization()
    {
        // Arrange
        string expectedCharacterName = string.Empty;

        // Act
        IWebElement? campaignCharacterName = _wait.Until(webDriver => webDriver.FindElement(By.Id("inputCharacterName")));
        string actualCharacterName = campaignCharacterName.GetAttribute("value");

        // Assert
        Assert.Equal(expectedCharacterName, actualCharacterName);
    }

    [Fact]
    public void AuthorizedIndexPage_CustomCampaign_CustomStartScenarioIsEmptyOnInitialization()
    {
        // Arrange
        string expectedStartScenario = string.Empty;

        // Act
        IWebElement? campaignStartScenario = _wait.Until(webDriver => webDriver.FindElement(By.Id("inputCustomStartScenario")));
        string actualStartScenario = campaignStartScenario.GetAttribute("value");

        // Assert
        Assert.Equal(expectedStartScenario, actualStartScenario);
    }

    [Fact]
    public void AuthorizedIndexPage_CustomCampaign_CreateCampaignButtonIsDisplayed()
    {
        // Act
        IWebElement? startCampaignButton = _wait.Until(webDriver => webDriver.FindElement(By.Id("create-campaign-button")));

        // Assert
        Assert.True(startCampaignButton.Displayed);
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

    public static IEnumerable<object[]> StartScenarioTitles => new List<object[]>
    {
        new object[] { "The Forgotten Artifacts of Aldorath" },
        new object[] { "Shadows over Silverpine" },
        new object[] { "The Ashen Veil" },
        new object[] { "Whispers of the Void" },
        new object[] { "The Lost Kingdom of Zur" },
    };

    public static IEnumerable<object[]> StartScenarioDescriptions => new List<object[]>
    {
        new object[]
        {
            "The peaceful kingdom of Aldorath is now facing imminent dangers from a prophecy that " +
            "foretells its downfall. The only hope lies in the legend of seven ancient artifacts rumored " +
            "to have immense power. The players start as inexperienced adventurers summoned by the king."
        },
        new object[]
        {
            "In the remote, usually tranquil village of Silverpine, people have been mysteriously " +
            "vanishing during the night. The players start as a group of investigators hired by the " +
            "desperate villagers to discover the source of these unsettling circumstances."
        },
        new object[]
        {
            "A deadly, visibly moving fog known as the Ashen Veil is swallowing cities one by one. " +
            "The players begin as adventurers brought together by fate in a small town on the edge of " +
            "the fog's destructive path, tasked with finding a way to halt the approaching doom."
        },
        new object[]
        {
            "An ancient, long-forgotten deity has been awakened, threating to engulf the world into chaos " +
            "and darkness. The players are chosen by an opposing deity and begin their journey at a " +
            "secluded temple, receiving their divine mission from the temple's oracle."
        },
        new object[]
        {
            "The legendary submerged city of Zur, believed to be a myth, has risen from the depths of the " +
            "ocean, bringing about seismic disturbances around the world and causing sea creatures to " +
            "behave abnormally. The players start their adventure as part of a commissioned exploration " +
            "team, tasked by the Grand Council to investigate this unprecedented event."
        },
    };
}
