using KSMP.Extension;
using KSMP.Pages;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Win32;
using System;
using static KSMP.ClassManager;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace KSMP.Controls;

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
        bool willUseGifProfileImage = (Utils.Configuration.GetValue("UseGifProfileImage") as bool?) ?? false;
        bool willUseGifInTimeline = (Utils.Configuration.GetValue("UseGifInTimeline") as bool?) ?? false;
        bool willUseEmbeddedVideoPlayer = (Utils.Configuration.GetValue("UseEmbeddedVideoPlayer") as bool?) ?? false;
        bool willClearTimelineOnRefresh = (Utils.Configuration.GetValue("ClearTimelineOnRefresh") as bool?) ?? true;
        bool willWarnOnHighMemoryUsage = (Utils.Configuration.GetValue("WarnOnHighMemoryUsage") as bool?) ?? true;
        bool willUseResponsiveTimeline = (Utils.Configuration.GetValue("UseResponsiveTimeline") as bool?) ?? true;
        bool willUseDynamicTimelineLoading = (Utils.Configuration.GetValue("UseDynamicTimelineLoading") as bool?) ?? false;
        bool willShowMyProfileOnStartup = (Utils.Configuration.GetValue("ShowMyProfileOnStartup") as bool?) ?? false;

        TsFavoriteFriendNotification.IsOn = willReceiveFavoriteFriendNotification;
        TsEmotionalNotification.IsOn = willReceiveEmotionalNotification;
        TsLaunchAtStartup.IsOn = willLaunchAtStartup;
        TsUseGifProfileImage.IsOn = willUseGifProfileImage;
        TsUseGifInTimeline.IsOn = willUseGifInTimeline;
        TsUseEmbeddedVideoPlayer.IsOn = willUseEmbeddedVideoPlayer;
        TsClearTimelineOnRefresh.IsOn = willClearTimelineOnRefresh;
        TsWarnOnHighMemoryUsage.IsOn = willWarnOnHighMemoryUsage;
        TsUseResponsiveTimeline.IsOn = willUseResponsiveTimeline;
        TsUseDynamicTimelineLoading.IsOn = willUseDynamicTimelineLoading;
        TsShowMyProfileOnStartup.IsOn = willShowMyProfileOnStartup;

        TsFavoriteFriendNotification.Toggled += OnReceiveFavoriteFriendNotificationToggleSwitchToggled;
        TsEmotionalNotification.Toggled += OnReceiveEmotionalNotificationToggleSwitchToggled;
        TsLaunchAtStartup.Toggled += OnLaunchAtStartupToggleSwitchToggled;
        TsUseGifProfileImage.Toggled += OnUseGifProfileImageToggleSwitchToggled;
        TsUseGifInTimeline.Toggled += OnUseGifInTimelineToggleSwitchToggled;
        TsUseEmbeddedVideoPlayer.Toggled += OnUseEmbeddedVideoPlayerToggleSwitchToggled;
        TsClearTimelineOnRefresh.Toggled += OnClearTimelineOnRefreshToggleSwitchToggled;
        TsWarnOnHighMemoryUsage.Toggled += OnWarnOnHighMemoryUsageToggleSwitchToggled;
        TsUseResponsiveTimeline.Toggled += OnUseResponsiveTimelineToggleSwitchToggled;
        TsUseDynamicTimelineLoading.Toggled += OnUseDynamicTimelineLoadingToggleSwitchToggled;
        TsShowMyProfileOnStartup.Toggled += OnShowMyProfileOnStartupToggleSwitchToggled;

        int defaultPostWritingPermission = (Utils.Configuration.GetValue("DefaultPostWritingPermission") as int?) ?? 0;
        CbxDefaultPostWritingPermission.SelectedIndex = defaultPostWritingPermission;
        CbxDefaultPostWritingPermission.SelectionChanged += OnDefaultPostWritingPermissionComboBoxSelectionChanged;

        ValidateThemeSetting();
        CbxThemeSetting.SelectionChanged += OnThemeSettingComboBoxSelectionChanged;
    }

    private void OnShowMyProfileOnStartupToggleSwitchToggled(object sender, RoutedEventArgs e)
    {
        var toggleSwitch = sender as ToggleSwitch;
        var isOn = toggleSwitch.IsOn;
        Utils.Configuration.SetValue("ShowMyProfileOnStartup", isOn);
    }

    private void OnUseEmbeddedVideoPlayerToggleSwitchToggled(object sender, RoutedEventArgs e)
    {
        var toggleSwitch = sender as ToggleSwitch;
        var isOn = toggleSwitch.IsOn;
        Utils.Configuration.SetValue("UseEmbeddedVideoPlayer", isOn);
        RequestProgramRestart();
    }

    private async void OnUseDynamicTimelineLoadingToggleSwitchToggled(object sender, RoutedEventArgs e)
    {
        var toggleSwitch = sender as ToggleSwitch;
        var isOn = toggleSwitch.IsOn;
        Utils.Configuration.SetValue("UseDynamicTimelineLoading", isOn);
        TimelinePage.WillUseDynamicTimelineLoading = isOn;
        await TimelinePage.Refresh();
    }

    private void OnUseResponsiveTimelineToggleSwitchToggled(object sender, RoutedEventArgs e)
    {
        var toggleSwitch = sender as ToggleSwitch;
        var isOn = toggleSwitch.IsOn;
        Utils.Configuration.SetValue("UseResponsiveTimeline", isOn);
        App.RecordedFirstFeedId = TimelinePage.LastFeedId;
        MainPage.NavigateTimeline(MainPage.LastArgs);
    }

    private void OnWarnOnHighMemoryUsageToggleSwitchToggled(object sender, RoutedEventArgs e)
    {
        var toggleSwitch = sender as ToggleSwitch;
        var isOn = toggleSwitch.IsOn;
        Utils.Configuration.SetValue("WarnOnHighMemoryUsage", isOn);
    }

    private void OnThemeSettingComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var comboBox = sender as ComboBox;

        string themeSetting;
        if (comboBox.SelectedIndex == 0) themeSetting = "Default";
        else if (comboBox.SelectedIndex == 1) themeSetting = "Light";
        else themeSetting = "Dark";
        Utils.Configuration.SetValue("ThemeSetting", themeSetting);
        RequestProgramRestart();
    }

    private async void RequestProgramRestart()
    {
        var result = await this.ShowMessageDialogAsync("옵션을 완전히 적용하기 위해서는 프로그램 재시작이 필요합니다.\n확인을 누르면 프로그램을 재시작합니다.", "안내", true);
        if (result == ContentDialogResult.Primary) Utility.SaveCurrentStateAndRestartProgram();
    }

    private void ValidateThemeSetting()
    {
        var themeSetting = Utils.Configuration.GetValue("ThemeSetting") as string ?? "Default";
        if(themeSetting == "Default") CbxThemeSetting.SelectedIndex = 0;
        else if(themeSetting == "Light") CbxThemeSetting.SelectedIndex = 1;
        else CbxThemeSetting.SelectedIndex = 2;
    }

    private async void OnClearTimelineOnRefreshToggleSwitchToggled(object sender, RoutedEventArgs e)
    {
        var toggleSwitch = sender as ToggleSwitch;
        var isOn = toggleSwitch.IsOn;
        Utils.Configuration.SetValue("ClearTimelineOnRefresh", isOn);
        if (!isOn) await this.ShowMessageDialogAsync("무한 스크롤 활성화 시 WinUI 프레임워크의 버그로 인하여 메모리가 더 누수되며, 프로세서 사용량이 늘어날 수 있습니다.", "경고", false);
    }

    private void OnUseGifInTimelineToggleSwitchToggled(object sender, RoutedEventArgs e)
    {
        var toggleSwitch = sender as ToggleSwitch;
        var isOn = toggleSwitch.IsOn;
        Utils.Configuration.SetValue("UseGifInTimeline", isOn);
        RequestProgramRestart();
    }

    private void OnUseGifProfileImageToggleSwitchToggled(object sender, RoutedEventArgs e)
    {
        var toggleSwitch = sender as ToggleSwitch;
        var isOn = toggleSwitch.IsOn;
        Utils.Configuration.SetValue("UseGifProfileImage", isOn);
        RequestProgramRestart();
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

    private void OnShowDcConSettingsButtonClicked(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        var control = new DcConSettingsControl();
        MainPage.ShowOverlay(control, true);
        MainPage.HideSettingsFlyout();
    }
}
