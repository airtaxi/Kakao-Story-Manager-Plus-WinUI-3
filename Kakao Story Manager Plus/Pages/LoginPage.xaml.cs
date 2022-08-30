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

namespace KSMP.Pages;

public sealed partial class LoginPage : Page
{
    private const string MainUrl = "https://story.kakao.com/";

    private bool isFirst = true;
    private bool isNavigated = false;

    public static bool IsLoggedIn;
    private DispatcherTimer _loginCheckTimer = null;
    public delegate void LoginSuccess();
    public static LoginSuccess OnLoginSuccess;

    public LoginPage()
    {
        IsLoggedIn = false;
        InitializeComponent();
        Initialize();
    }

    private async void Initialize()
    {
        await Task.Delay(100);
        SetLoading(true, "의존성 패키지 검사중");
        await CheckWebView2Runtime();
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
        }

        WvLogin.NavigationCompleted += OnNavigationCompleted;
        WvLogin.Source = new Uri("https://story.kakao.com/s/logout");
        BtLogin.Content = "로그아웃중...";
        BtLogin.IsEnabled = false;

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

    public void SetLoading(bool isLoading, string message = null)
    {
        if (GdLoading == null) return;
        GdLoading.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
        if (isLoading && message != null)
        {
            IsEnabled = false;
            TbLoading.Text = message;
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
            {
                SetLoading(true, $"업데이터 다운로드중 ({e.ProgressPercentage}%)");
            };

            await client.DownloadFileTaskAsync(new Uri("https://kagamine-rin.com/KSMP/Installer.msi"), tempFile);
        }

    }

    private async Task CheckWebView2Runtime()
    {
        string version = null;

        try { version = CoreWebView2Environment.GetAvailableBrowserVersionString(); }
        catch (Exception)
        {
            // ignored
        }

        if (string.IsNullOrEmpty(version))
        {
            TaskCompletionSource taskCompletionSource = new();
            await this.ShowMessageDialogAsync("프로그램 구동을 위해 WebView2 런타임이 필요합니다.\n확인 버튼을 누르면 설치합니다.", "안내");
            SetLoading(true, "런타임 다운로더 초기화중");
            var tempFile = Path.Combine(Path.GetTempPath(), "webview2runtime.exe");
            var client = new WebClient();
            client.DownloadFileCompleted += async (_, _) =>
            {
                await this.ShowMessageDialogAsync("런타임 다운로드가 완료되었습니다. 확인을 누르면 실행되는 설치 프로그램을 통하여 설치를 완료하신 뒤 프로그램을 재실행 해주세요", "안내");
                Process.Start(tempFile);
                Environment.Exit(0);
            };
            client.DownloadProgressChanged += (_, e) =>
            {
                SetLoading(true, $"런타임 다운로드중 ({e.ProgressPercentage}%)");
            };
            await client.DownloadFileTaskAsync(new Uri("https://go.microsoft.com/fwlink/p/?LinkId=2124703"), tempFile);
            await taskCompletionSource.Task;
        }
    }

    private async void OnNavigationCompleted(WebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
    {
        await this.RunOnMainThreadAsync(async () =>
        {
            var isLogin = sender.Source.AbsoluteUri.Contains("continue=https://story.kakao.com/");
            isLogin = isLogin || sender.Source.AbsoluteUri == "https://story.kakao.com/";
            if (sender.Source.AbsoluteUri == "https://accounts.kakao.com/login") isLogin = false;
            if (!isLogin && BtLogin.IsEnabled == false)
            {
                if (PbLogin.Visibility != Visibility.Collapsed) return;
                BtLogin.Content = "로그인";
                BtLogin.IsEnabled = true;
                if (CbxRememberCredentials.IsChecked == true) OnLoginButtonClicked(BtLogin, null);
                return;
            }

            bool wasFirst = isFirst;
            if (isFirst)
            {
                isFirst = false;
                await WvLogin.ExecuteScriptAsync($"document.getElementById(\"id_email_2\").value = \"{TbxLogin.Text}\";");
                await WvLogin.ExecuteScriptAsync($"document.getElementById(\"id_password_3\").value = \"{PbxLogin.Password}\";");
                await WvLogin.ExecuteScriptAsync("document.getElementsByClassName(\"ico_account ico_check\")[0].click();");
                await WvLogin.ExecuteScriptAsync("document.getElementsByClassName(\"btn_g btn_confirm submit\")[0].click();");
            }
            var cookieContainer = await GetCookieCookieContainerAsync();
            var cookies = GetCookieCollection(cookieContainer);
            var isLoggedIn = cookies.Any(x => x.Name == "_karmt");
            if (isLoggedIn)
            {
                if (isNavigated) return;
                isNavigated = true;
                SaveCredentials(TbxLogin.Text, PbxLogin.Password, CbxRememberCredentials.IsChecked == true);
                StoryApi.ApiHandler.Init(cookieContainer);
                IsLoggedIn = true;
                _loginCheckTimer?.Stop();
                WvLogin.Close();
                MainWindow.ReloginTaskCompletionSource?.SetResult();
                MainWindow.Navigate(typeof(MainPage));
                MainWindow.EnableLoginRequiredMenuFlyoutItems();
                OnLoginSuccess?.Invoke();
            }
            else if (!wasFirst)
            {
                await this.ShowMessageDialogAsync("로그인에 실패하였습니다.", "오류");
                PbLogin.Visibility = Visibility.Collapsed;
                BtLogin.IsEnabled = true;
            }
        });
    }

    private void OnLoginButtonClicked(object sender, RoutedEventArgs e)
    {
        isFirst = true;
        BtLogin.IsEnabled = false;
        var email = TbxLogin.Text;
        var password = PbxLogin.Password;

        WvLogin.Source = new Uri("https://accounts.kakao.com/login?continue=https://story.kakao.com/");

        _loginCheckTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(5)
        };
        _loginCheckTimer.Tick += async (s2, e2) =>
        {
            _loginCheckTimer.Stop();
            await this.RunOnMainThreadAsync(async () =>
            {
                var cookieContainer = await GetCookieCookieContainerAsync();
                var cookies = GetCookieCollection(cookieContainer);
                var isLoggedIn = cookies.Any(x => x.Name == "_karmt");
                if (BtLogin.IsEnabled == false && !isLoggedIn)
                {
                    await this.ShowMessageDialogAsync("로그인이 지연되고 있습니다.\n수동으로 로그인을 시도합니다.", "오류");
                    WvLogin.Visibility = Visibility.Visible;
                }
            });
        };
        _loginCheckTimer.Start();

        PbLogin.Visibility = Visibility.Visible;
    }

    private async Task<CookieContainer> GetCookieCookieContainerAsync(string url = MainUrl)
    {
        var cookies = await WvLogin?.CoreWebView2?.CookieManager?.GetCookiesAsync(url);
        if (cookies == null) return new();
        CookieContainer cookieContainer = new();

        foreach (var cookie in cookies)
            cookieContainer.Add(new Cookie() { Name = cookie.Name, Value = cookie.Value, Domain = cookie.Domain });

        return cookieContainer;
    }

    private static CookieCollection GetCookieCollection(CookieContainer cookieContainer) => cookieContainer.GetCookies(new Uri(MainUrl));

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
        if (e.Key == Windows.System.VirtualKey.Enter && BtLogin.IsEnabled) OnLoginButtonClicked(BtLogin, null);
    }
}
