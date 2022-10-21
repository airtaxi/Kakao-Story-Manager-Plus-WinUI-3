// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using ABI.Windows.Foundation;
using AngleSharp.Common;
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
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
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
            foreach(var item in data.Items)
            {
                var thumbnailUrl = $"https:{item.OnImageUrl}";
                var container = new Button();
                container.Padding = new Thickness(2);
                var image = new Image
                {
                    Source = Utility.GenerateImageUrlSource(thumbnailUrl),
                    Width = 30,
                    Height = 30
                };
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
                await Utils.Post.SetEmoticonImage(url, image);
                image.Tag = (data, index);
                GvMain.Items.Add(image);
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
            (EmoticonItem, int) tag = ((EmoticonItem, int))image.Tag;
            OnSelected?.Invoke(tag.Item1, tag.Item2);
        }
    }
}
