// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using KSMP.Extension;
using KSMP.Pages;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using StoryApi;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static StoryApi.ApiHandler.DataType.EmoticonItems;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace KSMP.Controls
{
    public sealed partial class EmoticonListControl : UserControl
    {
        public delegate void Selected(EmoticonItem item, int index);
        public Selected OnSelected;
        private List<FrameworkElement> _containers = new List<FrameworkElement>();
        public EmoticonListControl()
        {
            this.InitializeComponent();
            Refresh();
        }

        private async void Refresh()
        {
            IsEnabled = false;
            PrLoading.Visibility = Visibility.Visible;
            SpList.Children.Clear();
            var data = await ApiHandler.GetEmoticonList();
            foreach (var item in data.Items)
            {
                var thumbnailUrl = $"https:{item.OnImageUrl}";
                var container = new Button();
                container.Padding = new Thickness(2);

                var image = new Image
                {
                    Width = 30,
                    Height = 30
                };
                _ = Task.Run(async () => await MainPage.GetInstance().RunOnMainThreadAsync(() => Utility.SetImageUrlSource(image, thumbnailUrl)));
                Utility.LoadedImages.Add(image);

                container.Content = image;
                container.Margin = new Thickness(2.5,0,2.5,0);
                container.Tag = item;
                SpList.Children.Add(container);
                container.Click += OnButtonClicked;
                _containers.Add(container);
            }
            await Select(_containers.FirstOrDefault());
            IsEnabled = true;
            PrLoading.Visibility = Visibility.Collapsed;
        }

        private async void OnButtonClicked(object sender, RoutedEventArgs e) => await Select(sender as FrameworkElement);

        private async Task Select(FrameworkElement container)
        {
            IsEnabled = false;
            PrLoading.Visibility = Visibility.Visible;
            if (container == null) return;
            GvMain.Items.Clear();
            var data = container.Tag as EmoticonItem;
            for(int index = 1; index <= data.Count; index++)
            {
                var url = await ApiHandler.GetEmoticonUrl(data.Id, index.ToString());
                var image = new Image
                {
                    Width = 60,
                    Height = 60
                };
                GvMain.Items.Add(image);

                _ = Utility.SetEmoticonImage(url, image);
                image.Tag = (data, index);
            }
            IsEnabled = true;
            PrLoading.Visibility = Visibility.Collapsed;
        }

        private void OnEmoticonSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is null) return;
            var gridView = sender as GridView;
            var image = gridView.SelectedItem as FrameworkElement;
            if (image == null) return;
            GvMain.SelectedItem = null;
            if (image.Tag == null) return;
            (EmoticonItem, int) tag = ((EmoticonItem, int))image.Tag;
            OnSelected?.Invoke(tag.Item1, tag.Item2);
        }
    }
}
