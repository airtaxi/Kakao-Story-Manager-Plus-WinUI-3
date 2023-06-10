using KSMP.Controls.Extras;
using KSMP.Pages;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace KSMP.Controls;

public sealed partial class ExtrasControl : UserControl
{
    public ExtrasControl()
    {
        InitializeComponent();
    }

    private void OnBatchDeleteFriendsButtonClicked(object sender,  RoutedEventArgs e)
    {
        var control = new BatchDeleteFriendsControl();
        MainPage.ShowOverlay(control, true);
        MainPage.HideSettingsFlyout();
    }
}
