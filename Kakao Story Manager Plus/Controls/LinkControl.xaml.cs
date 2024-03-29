﻿using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using KSMP.Extension;
using static KSMP.ApiHandler.DataType;

namespace KSMP.Controls;

public sealed partial class LinkControl : UserControl
{
    private readonly string _url;
    public LinkControl(TimeLineData.Scrap data)
    {
        InitializeComponent();
        if ((data.image?.Count ?? 0) > 0)
        {
            bool willUseRealGifInTimeline = (Utils.Configuration.GetValue("UseRealGifInTimeline") as bool?) ?? false;
            
            var link = data.image[0];
            if (!link.Contains(".gif") || willUseRealGifInTimeline) Utility.SetImageUrlSource(ImgLink, link);
        }

        TbLinkTitle.Text = data.title ?? "";
        TbLinkDesc.Text = data.description ?? "";
        TbLinkUrl.Text = data.host ?? "";

        if (string.IsNullOrEmpty(TbLinkTitle.Text))
            TbLinkTitle.Visibility = Visibility.Collapsed;
        if (string.IsNullOrEmpty(TbLinkDesc.Text))
            TbLinkDesc.Visibility = Visibility.Collapsed;
        if (string.IsNullOrEmpty(TbLinkUrl.Text))
            TbLinkUrl.Visibility = Visibility.Collapsed;

        _url = data.url;
        ToolTipService.SetToolTip(TbLinkTitle, data.title);
        ToolTipService.SetToolTip(TbLinkDesc, data.description);
        ToolTipService.SetToolTip(TbLinkUrl, data.url);
    }

    public void UnloadMedia() => ImgLink?.DisposeImage();

    private void OnPointerEntered(object sender, PointerRoutedEventArgs e) => Utility.ChangeSystemMouseCursor(true);
    private void OnPointerExited(object sender, PointerRoutedEventArgs e) => Utility.ChangeSystemMouseCursor(false);

    private async void OnTapped(object sender, TappedRoutedEventArgs e)
    {
        e.Handled = true;
        await Windows.System.Launcher.LaunchUriAsync(new Uri(_url, UriKind.Absolute));
    }
}
