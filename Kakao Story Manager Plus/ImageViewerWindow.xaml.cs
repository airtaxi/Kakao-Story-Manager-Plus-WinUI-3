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
using WinUIEx;
using System.Threading.Tasks;
using Windows.Devices.WiFiDirect;

namespace KSMP;

public sealed partial class ImageViewerWindow : WindowEx
{
	public ImageViewerWindow(List<Medium> urlList, int index)
	{
		InitializeComponent();
		WindowHelper.SetupWindowTheme(this);

		SystemBackdrop = new MicaBackdrop() { Kind = Microsoft.UI.Composition.SystemBackdrops.MicaKind.Base };
		AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
		AppWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
		AppWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
		GdTitleBar.Loaded += TitleBarGridLoaded;
		GdTitleBar.SizeChanged += TitleBarGridSizeChanged;

		var windowState = Configuration.GetValue("imageViewerWindowState") as WindowState;
		if (windowState != null)
		{
			var wasMaximized = windowState.WasMaxmized;
			if (wasMaximized) (AppWindow.Presenter as OverlappedPresenter).Maximize();
			else
			{
				Width = windowState.Width;
				Height = windowState.Height;
			}
		}

		AppWindow.SetIcon(Path.Combine(App.BinaryDirectory, "icon.ico"));

		FrMain.Content = new ImageViewerControl(urlList, index);
		FrMain.UpdateLayout();
		this.CenterOnScreen();
	}

	private void TitleBarGridLoaded(object sender, RoutedEventArgs e) => SetDragRegionForTitleBarGrid();
	private void TitleBarGridSizeChanged(object sender, SizeChangedEventArgs e) => SetDragRegionForTitleBarGrid();

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

	private void SetDragRegionForTitleBarGrid()
	{
		var scaleAdjustment = GetScaleAdjustment();

		if (AppWindow.TitleBar.RightInset < 0 || AppWindow.TitleBar.LeftInset < 0) return;

		List<Windows.Graphics.RectInt32> dragRectsList = new();
		Windows.Graphics.RectInt32 dragRect;

		dragRect.X = 0;
		dragRect.Y = 0;
		dragRect.Height = (int)(GdTitleBar.ActualHeight * scaleAdjustment);
		dragRect.Width = (int)((CdTitleBarIcon.ActualWidth + CdTitleBarMain.ActualWidth) * scaleAdjustment);
		dragRectsList.Add(dragRect);

		Windows.Graphics.RectInt32[] dragRects = dragRectsList.ToArray();
		AppWindow.TitleBar.SetDragRectangles(dragRects);
	}

	private async void OnPreviewKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
	{
		var isControlDown = Common.IsModifierDown();

		if (e.Key == Windows.System.VirtualKey.Escape)
			Close();
		if ((isControlDown && e.Key == Windows.System.VirtualKey.R) || e.Key == Windows.System.VirtualKey.F5)
			await (FrMain.Content as ImageViewerControl).DownloadImageAsync();
		else if (isControlDown && e.Key == Windows.System.VirtualKey.W)
			Close();
		else if (isControlDown && e.Key == Windows.System.VirtualKey.S)
			await (FrMain.Content as ImageViewerControl).DownloadImageAsync();
		else if (isControlDown && e.Key == Windows.System.VirtualKey.C)
			await (FrMain.Content as ImageViewerControl).CopyImageAsync();
	}

	private void OnWindowClosed(object sender, WindowEventArgs args)
	{
		var appWindow = AppWindow;
		var presenter = appWindow?.Presenter as OverlappedPresenter;
		var isMaximized = presenter?.State == OverlappedPresenterState.Maximized;

		var windowState = Configuration.GetValue("imageViewerWindowState") as WindowState ?? new();

		windowState.WasMaxmized = isMaximized;
		if (!isMaximized)
		{
			var width = Width;
			var height = Height;
			windowState.Width = (int)width;
			windowState.Height = (int)height;
		}
		Configuration.SetValue("imageViewerWindowState", windowState);
	}

	private void OnZoomInButtonClicked(object sender, RoutedEventArgs e) => (FrMain.Content as ImageViewerControl).ZoomIn();
	private void OnZoomOutButtonClicked(object sender, RoutedEventArgs e) => (FrMain.Content as ImageViewerControl).ZoomOut();
	private async void OnSaveButtonClicked(object sender, RoutedEventArgs e) => await (FrMain.Content as ImageViewerControl).DownloadImageAsync();

	private async void OnGridSizeChanged(object sender, SizeChangedEventArgs e)
	{
		await Task.Delay(100);
		(FrMain.Content as ImageViewerControl)?.ZoomOut();
	}
}
