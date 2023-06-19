// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KSMP.Api.DcCon;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace KSMP.Controls;

public sealed partial class DcConListControl : UserControl
{
    public delegate void Selected(DataType.Package.Detail item);
    public Selected OnSelected;
    private List<FrameworkElement> _containers = new List<FrameworkElement>();
    public DcConListControl()
    {
        this.InitializeComponent();
        Refresh();
    }

    private void Refresh()
    {
        IsEnabled = false;
        PrLoading.Visibility = Visibility.Visible;
        SpList.Children.Clear();
        var packages = Utility.GetCurrentDcConList();
        foreach (var package in packages)
        {
            var thumbnailUrl = $"https://dcimg5.dcinside.com/dccon.php?no={package.PackageInfo.MainImgPath}";
            var container = new Button();
            container.Padding = new Thickness(2);

            var image = new Image
            {
                Width = 30,
                Height = 30
            };
            _ = Task.Run(async () => await Utility.RunOnMainThreadAsync(() => Utility.SetDcConImageAsync(image, thumbnailUrl)));
            Utility.LoadedImages.Add(image);

            container.Content = image;
            container.Margin = new Thickness(2.5,0,2.5,0);
            container.Tag = package;
            SpList.Children.Add(container);
            container.Click += OnButtonClicked;
			{
				RoutedEventHandler unloaded = null;
				unloaded = (s, e) =>
				{
					container.Click += OnButtonClicked;
                    Unloaded -= unloaded;
				};
				Unloaded += unloaded;
			}
			_containers.Add(container);
        }
        SelectItem(_containers.FirstOrDefault());
        IsEnabled = true;
        PrLoading.Visibility = Visibility.Collapsed;
    }

    private void OnButtonClicked(object sender, RoutedEventArgs e) => SelectItem(sender as FrameworkElement);

    private void SelectItem(FrameworkElement container)
    {
        IsEnabled = false;
        PrLoading.Visibility = Visibility.Visible;
        if (container == null) return;
        GvMain.Items.Clear();
        var data = container.Tag as DataType.Package;
        foreach(var detail in data.PackageDetail)
        {
            var url = $"https://dcimg5.dcinside.com/dccon.php?no={detail.Path}";
            var image = new Image
            {
                Width = 60,
                Height = 60
            };
            GvMain.Items.Add(image);

            _ = Utility.SetDcConImageAsync(image, url);
            image.Tag = detail;
        }
        IsEnabled = true;
        PrLoading.Visibility = Visibility.Collapsed;
    }

    private void OnDcConSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is null) return;
        var gridView = sender as GridView;

        var image = gridView.SelectedItem as FrameworkElement;
        if (image == null) return;
        GvMain.SelectedItem = null;

        var detail = image.Tag as DataType.Package.Detail;
        OnSelected?.Invoke(detail);
    }
}
