using Microsoft.Win32;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Support.UI;
using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using WebDriverManager.DriverConfigs.Impl;
using WebDriverManager;
using System.IO;

namespace KSMP;

public static class LoginManager
{
    public static EdgeDriver SeleniumDriver = null;

    private static bool CheckIfElementExists(WebDriver driver, By by)
    {
        try
        {
            driver.FindElement(by);
            return true;
        }
        catch (NoSuchElementException)
        {
            return false;
        }
    }

    public static bool LoginWithSelenium(string email, string password, bool isHeadless = true)
    {
        var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Edge\BLBeacon", true);
        string version = key.GetValue("version") as string;
        try { new DriverManager().SetUpDriver(new EdgeConfig(), version); } catch (Exception) { };
        string driverPath = Path.Combine(App.BinaryDirectory, "Edge", version, "X64");

        var service = EdgeDriverService.CreateDefaultService(driverPath);
        service.HideCommandPromptWindow = true;
        service.UseVerboseLogging = true;

        var options = new EdgeOptions();
        if(isHeadless) options.AddArgument("headless");

        SeleniumDriver = new EdgeDriver(service, options);
        SeleniumDriver.Navigate().GoToUrl("https://accounts.kakao.com/login/?continue=https://story.kakao.com/");

        try
        {
            var isNewLogin = CheckIfElementExists(SeleniumDriver, By.XPath("//*[@id=\"input-loginKey\"]"));

            if (isNewLogin)
            {
                var emailBox = SeleniumDriver.FindElement(By.XPath("//*[@id=\"input-loginKey\"]"));
                emailBox.SendKeys(email);

                var passwordBox = SeleniumDriver.FindElement(By.XPath("//*[@id=\"input-password\"]"));
                passwordBox.SendKeys(password);

                var loginButton = SeleniumDriver.FindElement(By.XPath("//*[@id=\"mainContent\"]/div/div/form/div[4]/button[1]"));
                loginButton.Click();
            }
            else
            {
                var emailBox = SeleniumDriver.FindElement(By.XPath("//*[@id=\"id_email_2\"]"));
                emailBox.SendKeys(email);

                var passwordBox = SeleniumDriver.FindElement(By.XPath("//*[@id=\"id_password_3\"]"));
                passwordBox.SendKeys(password);

                var checkBox = SeleniumDriver.FindElement(By.XPath("//*[@id=\"login-form\"]/fieldset/div[4]/div/label/span[1]"));
                checkBox.Click();

                var loginButton = SeleniumDriver.FindElement(By.XPath("//*[@id=\"login-form\"]/fieldset/div[8]/button[1]"));
                loginButton.Click();
            }

            var wait = new WebDriverWait(SeleniumDriver, TimeSpan.FromDays(1));
            wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.UrlToBe("https://story.kakao.com/"));
            var rawCookies = SeleniumDriver.Manage().Cookies.AllCookies;

            bool isSuccess = rawCookies.Any(x => x.Name == "_karmt");
            if (!isSuccess)
            {
                if (isHeadless) return false;
                else return LoginWithSelenium(email, password, false);
            }

            var appKey = SeleniumDriver.ExecuteScript("return Kakao.Auth.getAppKey();").ToString();

            var cookies = new List<System.Net.Cookie>();
            var cookieContainer = new CookieContainer();
            foreach (var rawCookie in rawCookies)
            {
                var cookie = new System.Net.Cookie()
                {
                    Name = rawCookie.Name,
                    Domain = rawCookie.Domain,
                    Path = rawCookie.Path,
                    Value = rawCookie.Value
                };
                cookieContainer.Add(cookie);
                cookies.Add(cookie);
            }

            StoryApi.ApiHandler.Init(cookieContainer, cookies, appKey);
            return true;
        }
        catch (Exception exception)
        {
            if (isHeadless) return false;
            else return LoginWithSelenium(email, password, false);
        }
        finally
        {
            SeleniumDriver?.Close();
            SeleniumDriver?.Dispose();
            SeleniumDriver = null;
        }
    }
}
