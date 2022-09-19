using KSMP.Extension;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KSMP.Utils
{
    public static class Dialog
    {
        public static async Task ShowPermissionRequiredMessageDialog(UIElement element, string targetUserId, string description = "해당 사용자와 친구를 맺어야 글을 볼 수 있습니다.", string title = "오류")
        {
            var dialog = element.GenerateMessageDialog(description, title);
            dialog.SecondaryButtonText = "프로필 보기";
            dialog.SecondaryButtonClick += (s, e) => Pages.MainPage.ShowProfile(targetUserId);
            await dialog.ShowAsync();
        }
    }
}
