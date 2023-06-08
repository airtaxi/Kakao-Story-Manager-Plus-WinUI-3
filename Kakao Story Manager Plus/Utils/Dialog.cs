using KSMP.Extension;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;

namespace KSMP.Utils;

public static class Dialog
{
    public static async Task ShowPermissionRequiredMessageDialog(UIElement element, string targetUserId, string description = "해당 사용자와 친구를 맺어야 글을 볼 수 있습니다.", string title = "오류")
    {
        var result = await Utility.ShowMessageDialogAsync(description, title, true, "확인", "프로필 보기");
        if (result == ContentDialogResult.Secondary) Pages.MainPage.ShowProfile(targetUserId);
    }
}
