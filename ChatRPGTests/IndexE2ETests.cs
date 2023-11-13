using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace ChatRPGTests;

[Collection("E2E collection")]
public class IndexE2ETests : IDisposable
{
    private readonly ChatRPGFixture _fixture;
    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;

    public IndexE2ETests(ChatRPGFixture fixture)
    {
        _fixture = fixture;
        _driver = E2ETestUtility.Setup("/");
        _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void IndexPage_ContainsTitleText()
    {
        // Arrange
        string expectedTitle = "ChatRPG";

        // Act
        Thread.Sleep(1200); // wait for typing animation to finish
        IWebElement? actualTitle = _wait.Until(webDriver => webDriver.FindElement(By.ClassName("title-front")));

        // Assert
        Assert.Contains(expectedTitle, actualTitle.Text);
    }

    [Fact]
    public void IndexPage_ContainsSloganText()
    {
        // Arrange
        string expectedSlogan = "Immerse yourself in the ultimate AI-powered adventure!";

        // Act
        Thread.Sleep(1000); // wait for typing animation to finish
        IWebElement? actualSlogan = _wait.Until(webDriver => webDriver.FindElement(By.ClassName("slogan-front")));

        // Assert
        Assert.Equal(expectedSlogan, actualSlogan.Text);
    }

    [Fact]
    public void IndexPage_ContainsLoginButton()
    {
        Thread.Sleep(1000); // wait for typing animation to finish
        // Act
        IWebElement? loginButton = _wait.Until(webDriver => webDriver.FindElement(By.Id("login-button")));

        // Assert
        Assert.True(loginButton.Displayed);
    }

    [Fact]
    public void IndexPage_ContainsRegisterButton()
    {
        Thread.Sleep(1000); // wait for typing animation to finish
        // Act
        IWebElement? registerButton = _wait.Until(webDriver => webDriver.FindElement(By.Id("register-button")));

        // Assert
        Assert.True(registerButton.Displayed);
    }

    [Fact]
    public void PressingLoginButton_ShouldRedirectToLoginPage()
    {
        Thread.Sleep(1000); // wait for typing animation to finish
        // Act
        _wait.Until(webDriver => webDriver.FindElement(By.Id("login-button"))).Click();
        string expectedUrl = "http://localhost:5111/Login";
        bool urlRedirects = _wait.Until(webDriver => webDriver.Url == expectedUrl);

        // Assert
        Assert.True(urlRedirects);
    }

    [Fact]
    public void PressingRegisterButton_ShouldRedirectToRegisterPage()
    {
        Thread.Sleep(1000); // wait for typing animation to finish
        // Act
        _wait.Until(webDriver => webDriver.FindElement(By.Id("register-button"))).Click();
        string expectedUrl = "http://localhost:5111/Register";
        bool urlRedirects = _wait.Until(webDriver => webDriver.Url == expectedUrl);

        // Assert
        Assert.True(urlRedirects);
    }

    public void Dispose()
    {
        E2ETestUtility.Teardown(_driver);
        _driver.Dispose();
    }
}
