using CommunityToolkit.Mvvm.ComponentModel;

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
