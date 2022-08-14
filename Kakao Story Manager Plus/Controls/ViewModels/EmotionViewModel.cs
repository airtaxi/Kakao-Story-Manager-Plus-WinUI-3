using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
