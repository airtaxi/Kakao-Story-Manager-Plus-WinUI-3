using KSMP.Controls;
using Microsoft.UI.Windowing;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics;
using WinRT.Interop;
using static KSMP.ApiHandler.DataType.CommentData;
using static KSMP.Controls.WritePostControl;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace KSMP
{
	/// <summary>
	/// An empty window that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class WritePostWindow : Window
	{
		public DispatcherTimer Timer;
		public WritePostControl Control;
		public WritePostWindow(PostData post = null)
		{
			InitializeComponent();
			AppWindow.SetIcon(Path.Combine(App.BinaryDirectory, "icon.ico"));

			if(post == null) Control = new WritePostControl();
			else
			{
				Title = "글 공유";
				Control = new WritePostControl(post);
			}

			Control.OnPostCompleted += OnPostCompleted;

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

		private void OnPostCompleted() => Close();
		private void OnWindowClosed(object sender, WindowEventArgs args)
		{
			Timer.Stop();
			Timer.Tick -= OnTimerTick;
			Control.OnPostCompleted -= OnPostCompleted;
			Control = null;
		}
	}
}
