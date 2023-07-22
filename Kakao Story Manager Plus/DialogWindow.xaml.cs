using KSMP.Utils;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Windows.Foundation;
using Windows.Graphics;
using Windows.UI.WindowManagement;
using WinRT.Interop;
using WinUIEx;

namespace KSMP;

public sealed partial class DialogWindow : WindowEx
{
	public delegate void ButtonClicked();
	public ButtonClicked PrimaryButtonClicked;
	public ButtonClicked SecondaryButtonClicked;
	public DispatcherTimer Timer;

	public DialogWindow(string title, string description, bool showCancel = false, string primaryText = "확인", string secondaryText = "취소")
	{
		this.InitializeComponent();
		WindowHelper.SetupWindowTheme(this);

		SizeChanged += OnSizeChanged;

		Title = title;
		AppWindow.SetIcon(Path.Combine(App.BinaryDirectory, "icon.ico"));

		TbTitle.Text = title;
		TbDescription.Text = description;
		BtPrimary.Content = primaryText;
		BtSecondary.Content = secondaryText;

		PresenterKind = AppWindowPresenterKind.CompactOverlay;


		this.CenterOnScreen();
		SystemBackdrop = new MicaBackdrop() { Kind = Microsoft.UI.Composition.SystemBackdrops.MicaKind.Base };
		AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
		AppWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
		AppWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
		IsAlwaysOnTop = true;
		IsResizable = false;
		IsMaximizable = false;
		IsMaximizable = false;

		Content.LosingFocus += OnLosingFocus;


		if (!showCancel) BtSecondary.Visibility = Visibility.Collapsed;
		else
		{
			Grid.SetColumn(BtPrimary, 0);
			Grid.SetColumn(BtSecondary, 1);
		}

		GdMain.UpdateLayout();
		Content.UpdateLayout();

		Timer = new DispatcherTimer();
		Timer.Interval = TimeSpan.FromMilliseconds(100);
		Timer.Tick += OnTimerTick;
		Timer.Start();
		Width = 415;
		Height = 130;

		ResizeToContent();
	}
	private void OnTimerTick(object sender, object e) => ResizeToContent();

	private void ResizeToContent()
	{
		if(RdDescription.Height == GridLength.Auto)
		{
			Timer.Stop();
			Timer.Tick -= OnTimerTick;
			Timer = null;
			var height = GdMain.ActualHeight + 10;
			Height = height;
		}
		RdDescription.Height = GridLength.Auto;
	}

	private void OnLosingFocus(UIElement sender, LosingFocusEventArgs args) => BringToFront();

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

	private bool _selected = false;

	private void OnPrimaryButtonClicked(object sender, RoutedEventArgs e)
	{
		_selected = true;
		PrimaryButtonClicked?.Invoke();
		Close();
	}

	private void OnSecondaryButtonClicked(object sender, RoutedEventArgs e)
	{
		_selected = true;
		SecondaryButtonClicked?.Invoke();
		Close();
	}

	private void OnWindowClosed(object sender, WindowEventArgs args) => args.Handled = !_selected;

	private void OnUnloaded(object sender, RoutedEventArgs e) => SizeChanged -= OnSizeChanged;

	private void OnSizeChanged(object sender, WindowSizeChangedEventArgs args)
	{
		var scale = GetScaleAdjustment();

		var dragRects = new List<RectInt32>();
		RectInt32 dragRect;
		dragRect.X = 0;
		dragRect.Y = 0;
		dragRect.Width = (int)(GdMain.ActualWidth * scale);
		dragRect.Height = (int)(30 * scale);
		dragRects.Add(dragRect);

		AppWindow.TitleBar.SetDragRectangles(dragRects.ToArray());
	}
}
