using System;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

using KSMP.Extension;
using Microsoft.Web.WebView2.Core;
using System.Threading.Tasks;
using System.Net;
using System.Diagnostics;
using System.IO;
using Microsoft.UI.Windowing;
using Version = System.Version;
using System.Drawing.Text;
using OpenQA.Selenium.Chromium;
using OpenQA.Selenium.Edge;
using WebDriverManager;
using WebDriverManager.DriverConfigs.Impl;
using Microsoft.Win32;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support;
using static StoryApi.ApiHandler.DataType.CommentData;
using System.Threading;
using OpenQA.Selenium.Support.UI;
using System.Linq.Expressions;
using System.Collections.Generic;

namespace KSMP.Pages;

public sealed partial class LoginPage : Page
{
    public static bool IsLoggedIn;
    public delegate void LoginSuccess();
    public static LoginSuccess OnLoginSuccess;
    public static EdgeDriver SeleniumDriver = null;

    public LoginPage()
    {
        IsLoggedIn = false;
        InitializeComponent();
        Initialize();
    }

    private async void Initialize()
    {
        await Task.Delay(100);
        SetLoading(true, "의존성 검사중");
        await CheckEdgeBrowserInstalled();
        SetLoading(true, "버전 확인중");
        await CheckVersion();
        SetLoading(true, "폰트 확인중");
        await CheckFont();
        SetLoading(false);
        var hasRememberedCreditionals = Utils.Configuration.GetValue("willRememberCredentials") as bool? ?? false;
        if (hasRememberedCreditionals)
        {
            TbxLogin.Text = Utils.Configuration.GetValue("email") as string ?? string.Empty;
            PbxLogin.Password = Utils.Configuration.GetValue("password") as string ?? string.Empty;
            CbxRememberCredentials.IsChecked = hasRememberedCreditionals;
            BeginLogin();
        }

        MainWindow.DisableLoginRequiredMenuFlyoutItems();
    }

    private async Task CheckFont()
    {
        var fontsCollection = new InstalledFontCollection();
        var segoeFluentIconsExist = fontsCollection.Families.Any(x => x.Name == "Segoe Fluent Icons");
        if(!segoeFluentIconsExist)
        {
            await this.ShowMessageDialogAsync("프로그램 아이콘 표시에 필요한 폰트 파일이 설치되어있지 않습니다.\n확인을 누르면 폰트 파일을 다운로드 받습니다.\n이후 폰트를 설치하고 프로그램을 다시 실행해주세요.", "오류");
            
            var client = new WebClient();
            SetLoading(true, "폰트 다운로드 초기화중");

            var tempFile = Path.Combine(Path.GetTempPath(), $"Segoe Fluent Icons.ttf");

            client.DownloadFileCompleted += (_, _) =>
            {
                Process.Start(new ProcessStartInfo(tempFile) { UseShellExecute = true });
                Environment.Exit(0);
            };

            client.DownloadProgressChanged += (_, e) =>
            {
                SetLoading(true, $"폰트 다운로드중 ({e.ProgressPercentage}%)");
            };

            await client.DownloadFileTaskAsync(new Uri("https://kagamine-rin.com/KSMP/font.ttf"), tempFile);
        }
    }

    public void SetLoading(bool isLoading, string message = null, int progress = -1)
    {
        if (GdLoading == null) return;
        GdLoading.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
        if (isLoading && message != null)
        {
            IsEnabled = false;
            TbLoading.Text = message;
            var showProgress = progress > 0;
            PrLoading.IsIndeterminate = showProgress;
            if (showProgress) PrLoading.Value = progress;
        }
        else
            IsEnabled = true;
    }

    private async Task CheckVersion()
    {
        var client = new WebClient();
        var remoteVersionString = await client.DownloadStringTaskAsync(new Uri("https://kagamine-rin.com/KSMP/version"));
        var localVersionString = Utils.Common.GetVersionString();
        var remoteVersion = new Version(remoteVersionString);
        var localVersion = new Version(localVersionString);
        var result = remoteVersion.CompareTo(localVersion);

        if(result > 0)
        {
            await this.ShowMessageDialogAsync("프로그램 업데이트가 필요합니다.\n확인을 누르시면 업데이트를 진행합니다.", "안내");
            SetLoading(true, "업데이터 다운로드 초기화중");

            var tempFile = Path.Combine(Path.GetTempPath(), $"KSMP_{remoteVersionString}.msi");

            client.DownloadFileCompleted += (_, _) =>
            {
                Process.Start(new ProcessStartInfo(tempFile) { UseShellExecute = true });
                Environment.Exit(0);
            };

            client.DownloadProgressChanged += (_, e) =>
                SetLoading(true, $"업데이터 다운로드중 ({e.ProgressPercentage}%)", e.ProgressPercentage);

            await client.DownloadFileTaskAsync(new Uri("https://kagamine-rin.com/KSMP/Installer.msi"), tempFile);
        }

    }

    private async Task CheckEdgeBrowserInstalled()
    {
        bool isAvailable = false;
        try
        {
            var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Edge\BLBeacon", true);
            string version = key.GetValue("version") as string;
            if (!string.IsNullOrEmpty(version)) isAvailable = true;
        }
        catch (Exception) { } // Ignore
        if (!isAvailable)
        {
            await this.ShowMessageDialogAsync("본 프로그램을 이용하기 위해서는 Edge 브라우저가 설치되어있어야 합니다.", "오류");
            Environment.Exit(0);
        }
    }
    
    private static void SaveCredentials(string email, string password, bool willRememberCredentials)
    {
        if (willRememberCredentials)
        {
            Utils.Configuration.SetValue("email", email);
            Utils.Configuration.SetValue("password", password);
            Utils.Configuration.SetValue("willRememberCredentials", willRememberCredentials);
        }
        else
        {
            Utils.Configuration.SetValue("email", null);
            Utils.Configuration.SetValue("password", null);
            Utils.Configuration.SetValue("willRememberCredentials", null);
        }
    }

    private void OnLoginTextBoxKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter) PbxLogin.Focus(FocusState.Keyboard);
    }

    private void OnLoginPasswordBoxKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter && BtLogin.IsEnabled) BeginLogin();
    }

    private async void BeginLogin()
    {
        IsEnabled = false;
        PbLogin.Visibility = Visibility.Visible;

        var email = TbxLogin.Text;
        var password = PbxLogin.Password;
        bool loginSuccess = false;
        await Task.Run(() => loginSuccess = LoginWithSelenium(email, password));
        PbLogin.Visibility = Visibility.Collapsed;
        IsEnabled = true;

        if (!loginSuccess) await this.ShowMessageDialogAsync("로그인에 실패하였습니다.", "오류");
        else
        {
            IsLoggedIn = true;
            SaveCredentials(email, password, CbxRememberCredentials.IsChecked == true);
            MainWindow.Navigate(typeof(MainPage));
        }
    }

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


    private static bool LoginWithSelenium(string email, string password)
    {
        var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Edge\BLBeacon", true);
        string version = key.GetValue("version") as string;
        var driverPath = new DriverManager().SetUpDriver(new EdgeConfig(), version);
        driverPath = Path.GetDirectoryName(driverPath);

        var service = EdgeDriverService.CreateDefaultService(driverPath);
        service.HideCommandPromptWindow = true;
        service.UseVerboseLogging = true;

        var options = new EdgeOptions();
        options.AddArgument("headless");

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

                var loginButton = SeleniumDriver.FindElement(By.XPath("//*[@id=\"login-form\"]/fieldset/div[8]/button[1]"));
                loginButton.Click();
            }

            var wait = new WebDriverWait(SeleniumDriver, TimeSpan.FromSeconds(10));
            wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.UrlToBe("https://story.kakao.com/"));
            var rawCookies = SeleniumDriver.Manage().Cookies.AllCookies;

            bool isSuccess = rawCookies.Any(x => x.Name == "_karmt");
            if (!isSuccess) return false;

            var cookies = new List<System.Net.Cookie>();
            var cookieContainer = new CookieContainer();
            foreach (var rawCookie in rawCookies)
            {
                cookieContainer.Add(new System.Net.Cookie()
                {
                    Name = rawCookie.Name,
                    Domain = rawCookie.Domain,
                    Value = rawCookie.Value
                });
            }

            StoryApi.ApiHandler.Init(cookieContainer, cookies);
            return true;
        }
        catch (Exception) { return false; }
        finally
        {
            SeleniumDriver?.Close();
            SeleniumDriver?.Dispose();
            SeleniumDriver = null;
        }
    }

    private void OnLoginButtonClicked(object sender, RoutedEventArgs e) => BeginLogin();
}
