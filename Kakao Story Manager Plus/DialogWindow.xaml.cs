using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using PInvoke;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics;
using Windows.UI.WindowManagement;
using WinRT.Interop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace KSMP
{
	/// <summary>
	/// An empty window that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class DialogWindow : Window
	{
		public delegate void ButtonClicked();
		public ButtonClicked PrimaryButtonClicked;
		public ButtonClicked SecondaryButtonClicked;

		public DialogWindow(string title, string description, bool showCancel = false, string primaryText = "확인", string secondaryText = "취소")
		{
			this.InitializeComponent();
			SizeChanged += OnSizeChanged;

			Title = title;
			AppWindow.SetIcon(Path.Combine(App.BinaryDirectory, "icon.ico"));

			TbTitle.Text = title;
			TbDescription.Text = description;
			BtPrimary.Content = primaryText;
			BtSecondary.Content = secondaryText;

			AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;

			var presenter = AppWindow.Presenter as OverlappedPresenter;
			presenter.SetBorderAndTitleBar(true, false);
			presenter.IsResizable = false;
			presenter.IsMaximizable = false;
			presenter.IsMinimizable = false;
			if (!showCancel) BtSecondary.Visibility = Visibility.Collapsed;

			GdMain.UpdateLayout();

			var scale = GetScaleAdjustment();
			AppWindow.ResizeClient(new((int)(500 * scale), (int)(GdMain.ActualHeight * scale)));
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
}
