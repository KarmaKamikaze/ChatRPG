using System.Collections.ObjectModel;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace ChatRPGTests;

public static class E2ETestUtility
{
    public static IWebDriver Setup(string page)
    {
        ChromeOptions chromeOptions = new ChromeOptions();
        chromeOptions.AddArguments("--ignore-certificate-errors", "--start-maximized",
            "--disable-popup-blocking", "--window-size=1920,1080", "headless");
        IWebDriver driver = new ChromeDriver(chromeOptions);
        driver.Navigate().GoToUrl($"http://localhost:5111{page}");

        return driver;
    }

    public static void Teardown(IWebDriver driver)
    {
        try
        {
            driver.Navigate().GoToUrl("http://localhost:5111/Logout");
        }
        catch
        {
            // Was not logged in or unable to log out
            // We do not care if it fails during teardown
        }

        driver.Close();
        driver.Quit();
    }

    public static void Login(WebDriverWait wait, string username, string password)
    {
        Thread.Sleep(1000); // wait for Index title animation to finish
        wait.Until(webDriver => webDriver.FindElement(By.Id("login-button"))).Click();
        wait.Until(webDriver => webDriver.FindElement(By.Id("username-form"))).SendKeys(username);
        wait.Until(webDriver => webDriver.FindElement(By.Id("password-form"))).SendKeys(password);
        wait.Until(webDriver => webDriver.FindElement(By.Id("login-submit"))).Submit();
        Thread.Sleep(500); // wait for page to load fully
    }

    public static void CreateTestCampaign(IWebDriver driver, WebDriverWait wait, bool goToCampaign = false)
    {
        driver.Navigate().GoToUrl("http://localhost:5111/");
        Thread.Sleep(500);
        wait.Until(webDriver => webDriver.FindElement(By.Id("inputCampaignTitle"))).SendKeys("Test Title");
        wait.Until(webDriver => webDriver.FindElement(By.Id("inputCharacterName"))).SendKeys("Test Name");
        wait.Until(webDriver => webDriver.FindElement(By.Id("inputCharacterDescription"))).SendKeys("Test Description");
        wait.Until(webDriver => webDriver.FindElement(By.Id("inputCustomStartScenario"))).SendKeys("Test Scenario");
        wait.Until(webDriver => webDriver.FindElement(By.Id("create-campaign-button"))).Click();
        Thread.Sleep(200); // Wait for page to load
        if (!goToCampaign)
        {
            driver.Navigate().GoToUrl("http://localhost:5111/");
            Thread.Sleep(500);
        }
    }

    public static void RemoveTestCampaign(IWebDriver driver, WebDriverWait wait)
    {
        driver.Navigate().GoToUrl("http://localhost:5111/");
        Thread.Sleep(500); // Wait for page to load
        IWebElement? yourCampaignsContainer =
            wait.Until(webDriver => webDriver.FindElement(By.Id("your-campaigns")));
        ReadOnlyCollection<IWebElement>? removeButtons = yourCampaignsContainer.FindElements(By.ClassName("delete-campaign-button"));
        removeButtons[0].Click(); // Remove latest campaign
        wait.Until(webDriver => webDriver.FindElement(By.Id("modal-confirm"))).Click();
        Thread.Sleep(200);
    }
}
