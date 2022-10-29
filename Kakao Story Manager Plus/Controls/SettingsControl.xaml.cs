using KSMP.Extension;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Win32;
using OpenQA.Selenium.DevTools.V104.HeapProfiler;
using System;
using static KSMP.ClassManager;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace KSMP.Controls
{
    public sealed partial class SettingsControl : UserControl
    {
        private static readonly string AppName = "KakaoStoryManagerPlus";
        private static readonly string AppPath = System.Reflection.Assembly.GetExecutingAssembly().Location[..^4] + ".exe";

        public SettingsControl()
        {
            this.InitializeComponent();
            bool willReceiveFavoriteFriendNotification = (Utils.Configuration.GetValue("FavoriteFriendNotification") as bool?) ?? true;
            bool willReceiveEmotionalNotification = (Utils.Configuration.GetValue("EmotionalNotification") as bool?) ?? true;
            bool willRefreshAfterWritePost = (Utils.Configuration.GetValue("RefreshAfterWritePost") as bool?) ?? true;
            bool willLaunchAtStartup = (Utils.Configuration.GetValue("LaunchAtStartup") as bool?) ?? false;
            bool willUseGifProfileImage = (Utils.Configuration.GetValue("UseGifProfileImage") as bool?) ?? false;

            TsFavoriteFriendNotification.IsOn = willReceiveFavoriteFriendNotification;
            TsEmotionalNotification.IsOn = willReceiveEmotionalNotification;
            TsRefreshAfterWritePost.IsOn = willRefreshAfterWritePost;
            TsLaunchAtStartup.IsOn = willLaunchAtStartup;
            TsUseGifProfileImage.IsOn = willUseGifProfileImage;

            TsFavoriteFriendNotification.Toggled += OnReceiveFavoriteFriendNotificationToggleSwitchToggled;
            TsEmotionalNotification.Toggled += OnReceiveEmotionalNotificationToggleSwitchToggled;
            TsRefreshAfterWritePost.Toggled += OnRefreshAfterWritePostToggleSwitchToggled;
            TsLaunchAtStartup.Toggled += OnLaunchAtStartupToggleSwitchToggled;
            TsUseGifProfileImage.Toggled += OnUseGifProfileImageToggleSwitchToggled;

            int defaultPostWritingPermission = (Utils.Configuration.GetValue("DefaultPostWritingPermission") as int?) ?? 0;
            CbxDefaultPostWritingPermission.SelectedIndex = defaultPostWritingPermission;
            CbxDefaultPostWritingPermission.SelectionChanged += OnDefaultPostWritingPermissionComboBoxSelectionChanged;
        }

        private async void OnUseGifProfileImageToggleSwitchToggled(object sender, RoutedEventArgs e)
        {
            var toggleSwitch = sender as ToggleSwitch;
            var isOn = toggleSwitch.IsOn;
            Utils.Configuration.SetValue("UseGifProfileImage", isOn);
            var result = await this.ShowMessageDialogAsync("옵션을 완전히 적용하기 위해서는 프로그램 재시작이 필요합니다.\n확인을 누르면 프로그램을 종료합니다.", "안내", true);
            if (result == ContentDialogResult.Primary) Environment.Exit(0);
        }

        private void OnRefreshAfterWritePostToggleSwitchToggled(object sender, RoutedEventArgs e)
        {
            var toggleSwitch = sender as ToggleSwitch;
            var isOn = toggleSwitch.IsOn;
            Utils.Configuration.SetValue("RefreshAfterWritePost", isOn);
        }

        private void OnDefaultPostWritingPermissionComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            var permission = Enum.GetName(typeof(PostWritingPermission), comboBox.SelectedIndex);
            Utils.Configuration.SetValue("DefaultPostWritingPermission", permission);
        }

        private void OnReceiveFavoriteFriendNotificationToggleSwitchToggled(object sender, RoutedEventArgs e)
        {
            var toggleSwitch = sender as ToggleSwitch;
            var isOn = toggleSwitch.IsOn;
            Utils.Configuration.SetValue("FavoriteFriendNotification", isOn);
        }

        private void OnReceiveEmotionalNotificationToggleSwitchToggled(object sender, RoutedEventArgs e)
        {
            var toggleSwitch = sender as ToggleSwitch;
            var isOn = toggleSwitch.IsOn;
            Utils.Configuration.SetValue("EmotionalNotification", isOn);
        }

        private void OnLaunchAtStartupToggleSwitchToggled(object sender, RoutedEventArgs e)
        {
            var toggleSwitch = sender as ToggleSwitch;
            var isOn = toggleSwitch.IsOn;
            Utils.Configuration.SetValue("LaunchAtStartup", isOn);

            var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
            if (isOn)
                key.SetValue(AppName, $"{AppPath}");
            else
                key.DeleteValue(AppName);

            key.Flush();
            key.Close();
        }
    }
}
