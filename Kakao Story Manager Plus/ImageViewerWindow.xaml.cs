using KSMP.Controls;
using Microsoft.UI.Windowing;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Windows.Graphics;
using WinRT.Interop;
using static KSMP.ApiHandler.DataType.CommentData;
using KSMP.Utils;

namespace KSMP;

public sealed partial class ImageViewerWindow : Window
{
	public ImageViewerWindow(List<Medium> urlList, int index)
	{
		InitializeComponent();
		WindowHelper.SetupWindowTheme(this);

		AppWindow.SetIcon(Path.Combine(App.BinaryDirectory, "icon.ico"));
		AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;

		var white = Application.Current.Resources["White2"] as SolidColorBrush;
		AppWindow.TitleBar.ButtonBackgroundColor = white.Color;
		AppWindow.TitleBar.ButtonHoverBackgroundColor = white.Color;
		AppWindow.TitleBar.ButtonInactiveBackgroundColor = white.Color;
		AppWindow.TitleBar.ButtonPressedBackgroundColor = white.Color;

		FrMain.Content = new ImageViewerControl(urlList, index);
		FrMain.UpdateLayout();
		AdjustDragRectangle();
	}

	private enum Monitor_DPI_Type : int
	{
		MDT_Effective_DPI = 0,
		MDT_Angular_DPI = 1,
		MDT_Raw_DPI = 2,
		MDT_Default = MDT_Effective_DPI
	}

	[LibraryImport("Shcore.dll", SetLastError = true)]
	private static partial int GetDpiForMonitor(IntPtr hmonitor, Monitor_DPI_Type dpiType, out uint dpiX, out uint dpiY);

	private double GetScaleAdjustment()
	{
		IntPtr hWnd = WindowNative.GetWindowHandle(this);
		WindowId wndId = Win32Interop.GetWindowIdFromWindow(hWnd);
		DisplayArea displayArea = DisplayArea.GetFromWindowId(wndId, DisplayAreaFallback.Primary);
		IntPtr hMonitor = Win32Interop.GetMonitorFromDisplayId(displayArea.DisplayId);

		// Get DPI.
		int result = GetDpiForMonitor(hMonitor, Monitor_DPI_Type.MDT_Default, out uint dpiX, out uint _);
		if (result != 0)
			throw new Exception("Could not get DPI for monitor.");

		uint scaleFactorPercent = (uint)(((long)dpiX * 100 + (96 >> 1)) / 96);
		return scaleFactorPercent / 100.0;
	}

	private void OnSizeChanged(object sender, SizeChangedEventArgs args) => AdjustDragRectangle();

	private void AdjustDragRectangle()
	{
		var scale = GetScaleAdjustment();

		var dragRects = new List<RectInt32>();
		RectInt32 dragRect;
		dragRect.X = (int)(30 * scale);
		dragRect.Y = 0;
		dragRect.Width = (int)((FrMain.ActualWidth - 30) * scale);
		dragRect.Height = (int)(35 * scale);
		dragRects.Add(dragRect);

		AppWindow.TitleBar.SetDragRectangles(dragRects.ToArray());
	}

	private async void OnPreviewKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
	{
		var isControlDown = Common.IsModifierDown();

		if (e.Key == Windows.System.VirtualKey.Escape)
			Close();
		if ((isControlDown && e.Key == Windows.System.VirtualKey.R) || e.Key == Windows.System.VirtualKey.F5)
			await (FrMain.Content as ImageViewerControl).DownloadImage();
		else if (isControlDown && e.Key == Windows.System.VirtualKey.W)
			Close();
	}
}
