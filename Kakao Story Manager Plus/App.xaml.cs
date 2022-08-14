using KSMP.Controls;
using KSMP.Extension;
using KSMP.Pages;
using Microsoft.Toolkit.Uwp.Notifications;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace KSMP
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            ToastNotificationManagerCompat.OnActivated += OnNotificationActivated;
        }

        private async void OnNotificationActivated(ToastNotificationActivatedEventArgsCompat toastArgs)
        {
            if (!LoginPage.IsLoggedIn) return;
            ToastArguments args = ToastArguments.Parse(toastArgs.Argument);
            var keys = args.Select(x => x.Key).ToList();
            var action = keys[0];

            var activityId = keys.Where(x => x.StartsWith("Activity=")).SingleOrDefault();
            activityId = activityId?.Replace("Activity=", "");

            var profileId = keys.Where(x => x.StartsWith("Profile=")).SingleOrDefault();
            profileId = profileId?.Replace("Profile=", "");

            var commentId = keys.Where(x => x.StartsWith("Comment=")).SingleOrDefault();
            commentId = commentId?.Replace("Comment=", "");

            if (action == "Open")
            {
                MainWindow.Instance.DispatcherQueue.TryEnqueue(() => MainPage.ShowWindow());
                if (profileId != null)
                    MainWindow.Instance.DispatcherQueue.TryEnqueue(() => MainPage.ShowProfile(profileId));
                else if (activityId != null)
                {
                    var post = await StoryApi.ApiHandler.GetPost(activityId);
                    MainWindow.Instance.DispatcherQueue.TryEnqueue(() => MainPage.ShowOverlay(new TimelineControl(post, false, true)));
                }
            }
            else if(action == "Like") await StoryApi.ApiHandler.LikeComment(activityId, commentId, false);
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            m_window = new MainWindow();
            m_window.Activate();
        }

        private Microsoft.UI.Xaml.Window m_window;
    }
}
