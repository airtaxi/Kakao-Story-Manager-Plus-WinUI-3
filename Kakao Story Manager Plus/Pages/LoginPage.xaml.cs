using System;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

using KSMP.Extension;
using System.Threading.Tasks;
using System.Net;
using System.Diagnostics;
using System.IO;
using Version = System.Version;
using System.Drawing.Text;
using Microsoft.Win32;

namespace KSMP.Pages;

public sealed partial class LoginPage : Page
{
    public static bool IsLoggedIn;

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
            TbxLogin.Text = (Utils.Configuration.GetValue("email") as string) ?? string.Empty;
            PbxLogin.Password = (Utils.Configuration.GetValue("password") as string) ?? string.Empty;
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
            await Utility.ShowMessageDialogAsync("프로그램 아이콘 표시에 필요한 폰트 파일이 설치되어있지 않습니다.\n확인을 누르면 폰트 파일을 다운로드 받습니다.\n이후 폰트를 설치하고 프로그램을 다시 실행해주세요.", "오류");
            
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
            PrLoading.IsIndeterminate = !showProgress;
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
        if (localVersionString == null)
        {
            await Utility.ShowMessageDialogAsync("프로그램의 버전을 확인할 수 없습니다", "오류");
            return;
        }

        var remoteVersion = new Version(remoteVersionString);
        var localVersion = new Version(localVersionString);
        var result = remoteVersion.CompareTo(localVersion);

        if(result > 0)
        {
            await Utility.ShowMessageDialogAsync($"프로그램 업데이트가 필요합니다.\n확인을 누르시면 업데이트를 진행합니다.\n\n클라이언트 버전: {localVersionString}\n최신 버전: {remoteVersionString}", "안내");
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
            await Utility.ShowMessageDialogAsync("본 프로그램을 이용하기 위해서는 Edge 브라우저가 설치되어있어야 합니다.", "오류");
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
        else Utils.Configuration.SetValue("willRememberCredentials", null);
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
        await Task.Run(() => loginSuccess = LoginManager.LoginWithSelenium(email, password));
        PbLogin.Visibility = Visibility.Collapsed;
        IsEnabled = true;

        if (!loginSuccess) await Utility.ShowMessageDialogAsync("로그인에 실패하였습니다.", "오류");
        else
        {
            IsLoggedIn = true;
            SaveCredentials(email, password, CbxRememberCredentials.IsChecked == true);
            MainWindow.Navigate(typeof(MainPage));
            MainWindow.EnableLoginRequiredMenuFlyoutItems();
        }
    }

    private void OnLoginButtonClicked(object sender, RoutedEventArgs e) => BeginLogin();
}
