﻿using System;
using System.Collections.Generic;
using System.Net;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using static KSMP.ApiHandler.DataType.CommentData;

namespace KSMP.Controls;

public sealed partial class ImageViewerControl : UserControl
{
    private readonly List<Medium> _urlList;
    public ImageViewerControl(List<Medium> urlList, int index)
    {
        InitializeComponent();
        _urlList = urlList;
        LoadImage();
        FvImages.SelectedIndex = index;
    }

    private void LoadImage() => FvImages.ItemsSource = _urlList;

    private void OnScrollViewerTapped(object sender, TappedRoutedEventArgs e) => ResetImageSize((sender as ScrollViewer).Content as Image, sender as ScrollViewer);

    private static void ResetImageSize(Image image, ScrollViewer scrollViewer)
    {
        float heightFactor = (float)scrollViewer.ViewportHeight / (float)image.ActualHeight;
        float widthFactor = (float)scrollViewer.ViewportWidth / (float)image.ActualWidth;
        if(heightFactor < 1)
        {
            scrollViewer.ChangeView(scrollViewer.ScrollableWidth / 2, scrollViewer.ScrollableHeight / 2, heightFactor);
        }
        else if(widthFactor < 1)
        {
            scrollViewer.ChangeView(scrollViewer.ScrollableWidth / 2, scrollViewer.ScrollableHeight / 2, widthFactor);
        }
    }

    private void ImageLoaded(object sender, RoutedEventArgs e)
    {
        var image = sender as Image;
        var scrollViewer = image.Parent as ScrollViewer;
        ResetImageSize(image, scrollViewer);
    }

    private async void OnScrollViewerRightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        var index = FvImages.SelectedIndex;
        var list = FvImages.ItemsSource as List<Medium>;
        var medium = list[index];
        await Utility.SetImageClipboardFromUrl(this, medium.origin_url);
    }

    private void OnButtonPointerEntered(object sender, PointerRoutedEventArgs e) => Utility.ChangeSystemMouseCursor(true);

    private void OnButtonPointerExited(object sender, PointerRoutedEventArgs e) => Utility.ChangeSystemMouseCursor(false);

    private void CloseButtonTapped(object sender, TappedRoutedEventArgs e) => Pages.MainPage.HideOverlay();
    private async void DownloadButtonTapped(object sender, TappedRoutedEventArgs e)
    {
        var medium = FvImages.SelectedItem as Medium;
        var url = medium.origin_url;
        var file = await Utility.ShowImageFileSaveDialogAsync(url);
        if (file == null) return;
        GdLoading.Visibility = Visibility.Visible;
        var path = file.Path;
        await new WebClient().DownloadFileTaskAsync(url, path);
        GdLoading.Visibility = Visibility.Collapsed;
    }

    private void OnImageSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var flipView = (FlipView)sender;
        var item = flipView.ContainerFromItem(flipView.SelectedItem) as FlipViewItem;
        if (item != null)
        {
            var scrollViewer = item.ContentTemplateRoot as ScrollViewer;
            if(scrollViewer != null)
            {
                Console.WriteLine("SV");
                var image = scrollViewer.Content as Image;
                ResetImageSize(image, scrollViewer);
            }
        }
    }
}
