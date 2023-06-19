using KSMP.Controls;
using Microsoft.UI.Windowing;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using System;
using System.IO;
using System.Runtime.InteropServices;
using WinRT.Interop;
using static KSMP.ApiHandler.DataType.CommentData;
using KSMP.Utils;

namespace KSMP;

public sealed partial class WritePostWindow : Window
{
	public DispatcherTimer Timer;
	public WritePostControl Control;
	public WritePostWindow(PostData post = null)
	{
		InitializeComponent();
		WindowHelper.SetupWindowTheme(this);

		AppWindow.SetIcon(Path.Combine(App.BinaryDirectory, "icon.ico"));

		if(post == null) Control = new WritePostControl();
		else
		{
			Title = "글 공유";
			Control = new WritePostControl(post);
		}

		Control.SetParentWindow(this);
		Control.OnCloseRequested += OnCloseRequested;

		FrMain.Content = Control;
		FrMain.UpdateLayout();

		var presenter = AppWindow.Presenter as OverlappedPresenter;
		presenter.IsResizable = false;
		presenter.IsMaximizable = false;
		presenter.IsMinimizable = false;

		Timer = new DispatcherTimer();
		Timer.Interval = TimeSpan.FromMilliseconds(100);
		Timer.Tick += OnTimerTick;
		Timer.Start();

		ResizeToContent();
	}


	private void OnTimerTick(object sender, object e) => ResizeToContent();

	private void OnControlSizeChanged(object sender, SizeChangedEventArgs e) => ResizeToContent();

	private void ResizeToContent()
	{
		if (Control.IsComboBoxDropDownOpened) return;
		var scale = GetScaleAdjustment();
		var height = Control.GetHeight() + 35;
		AppWindow.ResizeClient(new((int)(400 * scale), (int)(height * scale)));
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

	private void OnCloseRequested() => Close();

	private void OnWindowClosed(object sender, WindowEventArgs args)
	{
		Timer.Stop();
		Timer.Tick -= OnTimerTick;
		Control.OnCloseRequested -= OnCloseRequested;
		Control = null;
	}
	private async void OnPreviewKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
	{
		var isControlDown = Common.IsModifierDown();

		if (e.Key == Windows.System.VirtualKey.Escape)
			Close();
		else if (isControlDown && e.Key == Windows.System.VirtualKey.W)
			Close();
	}
}
