using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;

namespace KSMP.Controls.ViewModels
{
    public partial class EmotionViewModel : ObservableObject
    {
        [ObservableProperty]
        string profileUrl;

        [ObservableProperty]
        string name;

        [ObservableProperty]
        UIElement emotionControl;

        [ObservableProperty]
        string id;
    }
}
