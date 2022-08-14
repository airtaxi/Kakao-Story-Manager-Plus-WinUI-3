using KSMP.Controls.ViewModels;
using KSMP.Extension;
using KSMP.Pages;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using StoryApi;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using static KSMP.ClassManager;
using static StoryApi.ApiHandler.DataType;
using static StoryApi.ApiHandler.DataType.FriendData;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace KSMP.Controls
{
    public sealed partial class EmotionsListControl : UserControl
    {
        public EmotionsListControl(List<ShareData.Share> likes)
        {
            this.InitializeComponent();

            List<EmotionViewModel> itemsSource = new();
            foreach(var like in likes)
            {
                var viewModel = new EmotionViewModel();
                viewModel.Name = like.actor.display_name;
                viewModel.Id = like.actor.id;

                UIElement emotionControl = new Frame();
                if (like.emotion == "like")
                    emotionControl = new Emotions.LikeControl();
                else if (like.emotion == "good")
                    emotionControl = new Emotions.CoolControl();
                else if (like.emotion == "pleasure")
                    emotionControl = new Emotions.PleasureControl();
                else if (like.emotion == "sad")
                    emotionControl = new Emotions.SadControl();
                else if (like.emotion == "cheerup")
                    emotionControl = new Emotions.CheerUpControl();
                viewModel.EmotionControl = emotionControl;

                viewModel.ProfileUrl = like.actor.GetValidUserProfileUrl();

                itemsSource.Add(viewModel);
            }
            LvEmotions.ItemsSource = itemsSource;
        }

        private void SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listView = sender as ListView;
            var viewModel = listView.SelectedItem as EmotionViewModel;
            if (viewModel == null) return;

            MainPage.HideOverlay();
            MainPage.ShowProfile(viewModel.Id);

            listView.SelectedItem = null;
        }
    }
}
