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
    }

    public void Dispose()
    {
        E2ETestUtility.RemoveTestCampaign(_driver, _wait);
        E2ETestUtility.Teardown(_driver);
        _driver.Dispose();
    }
}
