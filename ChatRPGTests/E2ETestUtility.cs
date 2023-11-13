using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace ChatRPGTests;

public static class E2ETestUtility
{
    public static IWebDriver Setup(string page)
    {
        ChromeOptions chromeOptions = new ChromeOptions();
        chromeOptions.AddArguments("--ignore-certificate-errors",
            "--start-maximized", "--disable-popup-blocking", "headless");
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
        }

        driver.Close();
        driver.Quit();
    }
}
