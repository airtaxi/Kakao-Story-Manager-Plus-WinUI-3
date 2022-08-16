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

namespace KSMP.Pages;

public sealed partial class LoginPage : Page
{
    private const string MainUrl = "https://story.kakao.com/";

    private bool isFirst = true;
    private bool isNavigated = false;

    public static bool IsLoggedIn;

    public LoginPage()
    {
        IsLoggedIn = false;
        InitializeComponent();
        Initialize();
    }

    private async void Initialize()
    {
        await CheckWebView2Runtime();
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
            await Task.Delay(100);
            TaskCompletionSource taskCompletionSource = new();
            await this.ShowMessageDialogAsync("프로그램 구동을 위해 WebView2 런타임이 필요합니다.\n확인 버튼을 누르면 설치합니다.", "안내");
            SetLoading(true, "런타임 다운로더 초기화중");
            var tempFile = Path.Combine(Path.GetTempPath(), "webview2runtime.exe");
            var client = new WebClient();
            client.DownloadFileCompleted += async (_, _) =>
            {
                await this.ShowMessageDialogAsync("런타임 다운로드가 완료되었습니다. 확인을 누르면 실행되는 설치 프로그램을 통하여 설치를 완료하신 뒤 프로그램을 재실행헤주세요", "안내");
                Process.Start(tempFile);
                Environment.Exit(0);
            };
            client.DownloadProgressChanged += async (_, e) =>
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
                MainWindow.Navigate(typeof(MainPage));
                MainWindow.EnableLoginRequiredMenuFlyoutItems();
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

        var timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(5)
        };
        timer.Tick += async (s2, e2) =>
        {
            timer.Stop();
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
        timer.Start();

        PbLogin.Visibility = Visibility.Visible;
    }

    private async Task<CookieContainer> GetCookieCookieContainerAsync(string url = MainUrl)
    {
        var cookies = await WvLogin.CoreWebView2.CookieManager.GetCookiesAsync(url);
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
