using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
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
            bool willLaunchAtStartup = (Utils.Configuration.GetValue("LaunchAtStartup") as bool?) ?? false;

            TsFavoriteFriendNotification.IsOn = willReceiveFavoriteFriendNotification;
            TsEmotionalNotification.IsOn = willReceiveEmotionalNotification;
            TsLaunchAtStartup.IsOn = willLaunchAtStartup;

            TsFavoriteFriendNotification.Toggled += OnReceiveFavoriteFriendNotificationToggleSwitchToggled;
            TsEmotionalNotification.Toggled += OnReceiveEmotionalNotificationToggleSwitchToggled;
            TsLaunchAtStartup.Toggled += OnLaunchAtStartupToggleSwitchToggled;

            int defaultPostWritingPermission = (Utils.Configuration.GetValue("DefaultPostWritingPermission") as int?) ?? 0;
            CbxDefaultPostWritingPermission.SelectedIndex = defaultPostWritingPermission;
            CbxDefaultPostWritingPermission.SelectionChanged += OnDefaultPostWritingPermissionComboBoxSelectionChanged;
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
                key.SetValue(AppName, $"\"{AppPath}\"");
            else
                key.DeleteValue(AppName);

            key.Flush();
            key.Close();
        }
    }
}
