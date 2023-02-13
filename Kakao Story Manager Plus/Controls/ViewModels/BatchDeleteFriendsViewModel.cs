using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;

namespace KSMP.Controls.ViewModels;

public partial class BatchDeleteFriendsViewModel : ObservableObject
{
    [ObservableProperty]
    string profileUrl;

    [ObservableProperty]
    string name;

    [ObservableProperty]
    bool isChecked;

    public bool IsBlindedUser { get; set; }
    public bool IsFavoriteUser { get; set; }
    public string UserId { get; set; }
}
