using KSMP.Controls;
using KSMP.Utils;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Networking.BackgroundTransfer;
using Windows.Security.Authentication.OnlineId;
using WinRT.Interop;
using WinUIEx;
using static KSMP.ApiHandler.DataType.CommentData;

namespace KSMP;

public sealed partial class TimelineWindow : WindowEx
{
	private static List<TimelineWindow> s_instances = new();

	public TimelineWindowControl Control;
	public string PostId { get; private set; }
	private PostData _post;

	private TimelineWindow(PostData postData)
	{
		s_instances.Add(this);
		PostId = postData.id;

		InitializeComponent();

		SystemBackdrop = new MicaBackdrop() { Kind = Microsoft.UI.Composition.SystemBackdrops.MicaKind.Base };
		AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
		AppWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
		AppWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
		GdTitleBar.Loaded += TitleBarGridLoaded;
		GdTitleBar.SizeChanged += TitleBarGridSizeChanged;

		var windowState = Configuration.GetValue("timelineWindowState") as WindowState;
		if(windowState != null)
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
		Initialize();
		Control = new TimelineWindowControl(this, postData, false, true);
		FrMain.Content = Control;
		this.CenterOnScreen();
	}

	private async void Initialize()
	{
		BtEmotions.IsEnabled = false;
		BtShare.IsEnabled = false;
		BtUp.IsEnabled = false;
		_post = await ApiHandler.GetPost(PostId);

		var emotionsFlyout = new Flyout
		{
			Content = new EmotionsWindowControl(_post, this)
		};
		BtEmotions.Flyout = emotionsFlyout;

		async void emotionsFlyoutOpeningEventHandler(object s, object e)
		{
			if (_post.liked)
			{
				await ApiHandler.LikePost(_post.id, null);
				await RefreshContent();
				HideEmotionsButtonFlyout();
			}
		}
		BtEmotions.Flyout.Opening += emotionsFlyoutOpeningEventHandler;
		BtEmotions.Flyout.Placement = FlyoutPlacementMode.TopEdgeAlignedLeft;

		var shareFlyout = new MenuFlyout();
		var sharePostMenuFlyoutItem = new MenuFlyoutItem() { Text = "스토리로 공유" };
		var sharePostCommand = new XamlUICommand();
		sharePostCommand.ExecuteRequested += OnSharePost;
		sharePostMenuFlyoutItem.Command = sharePostCommand;
		shareFlyout.Items.Add(sharePostMenuFlyoutItem);

		if (!_post.sharable || _post.@object != null)
			sharePostMenuFlyoutItem.Visibility = Visibility.Collapsed;

		var copyUrlPostMenuFlyoutItem = new MenuFlyoutItem() { Text = $"URL 복사하기" };
		var copyUrlCommand = new XamlUICommand();
		copyUrlCommand.ExecuteRequested += CopyPostUrl;
		copyUrlPostMenuFlyoutItem.Command = copyUrlCommand;
		shareFlyout.Items.Add(copyUrlPostMenuFlyoutItem);
		BtShare.Flyout = shareFlyout;

		BtEmotions.IsEnabled = true;
		BtShare.IsEnabled = true;
		BtUp.IsEnabled = true;

		RefreshUpButton();
		RefreshEmotionsButton();
	}

	private void RefreshEmotionsButton()
	{
		var white = Utility.GetSolidColorBrushFromHexString("#FFFFFFFF");
		if (_post.liked)
		{
			var emotion = _post.liked_emotion;
			if (emotion == "like")
			{
				BtEmotions.Background = Common.GetColorFromHexa("#FFE25434");
				FiEmotions.Foreground = white;
				FiEmotions.Glyph = "\xeb52";
			}
			else if (emotion == "good")
			{
				BtEmotions.Background = Common.GetColorFromHexa("#FFBCCB3C");
				FiEmotions.Foreground = white;
				FiEmotions.Glyph = "\ue735";
			}
			else if (emotion == "pleasure")
			{
				BtEmotions.Background = Common.GetColorFromHexa("#FFEFBD30");
				FiEmotions.Foreground = white;
				FiEmotions.Glyph = "\ued54";
			}
			else if (emotion == "sad")
			{
				BtEmotions.Background = Common.GetColorFromHexa("#FF359FB0");
				FiEmotions.Foreground = white;
				FiEmotions.Glyph = "\ueb42";
			}
			else if (emotion == "cheerup")
			{
				BtEmotions.Background = Common.GetColorFromHexa("#FF9C62AE");
				FiEmotions.Foreground = white;
				FiEmotions.Glyph = "\ue945";
			}
		}
		else
		{
			var requestedTheme = Utility.GetRequestedTheme();
			SolidColorBrush gray3;
			if (requestedTheme == ElementTheme.Light) gray3 = Utility.GetSolidColorBrushFromHexString("#FF888D94");
			else gray3 = Utility.GetSolidColorBrushFromHexString("#FF77726B");

			FiEmotions.Foreground = gray3;
			FiEmotions.Glyph = "\ueb52";
			BtEmotions.Background = Application.Current.Resources["White"] as SolidColorBrush;
		}
	}

	private void RefreshUpButton()
	{
		SolidColorBrush white;
		SolidColorBrush gray3;
		SolidColorBrush gray6;
		var requestedTheme = Utility.GetRequestedTheme();
		if (requestedTheme == ElementTheme.Light)
		{
			white = Utility.GetSolidColorBrushFromHexString("#FFFFFFFF");
			gray3 = Utility.GetSolidColorBrushFromHexString("#FF888D94");
			gray6 = Utility.GetSolidColorBrushFromHexString("#FFAAAAAA");
		}
		else
		{
			white = Utility.GetSolidColorBrushFromHexString("#FF343434");
			gray3 = Utility.GetSolidColorBrushFromHexString("#FF77726B");
			gray6 = Utility.GetSolidColorBrushFromHexString("#FF949494");

		}
		if (_post.sympathized)
		{
			BtUp.Background = gray6;
			FaUp.Foreground = white;
		}
		else
		{
			BtUp.Background = white;
			FaUp.Foreground = gray3;
		}
	}

	private async void OnSharePost(XamlUICommand sender, ExecuteRequestedEventArgs args) => await Control.SharePost();

	private async void CopyPostUrl(XamlUICommand sender, ExecuteRequestedEventArgs args)
	{
		string userId = _post.actor.id;
		string postId = _post.id.Split(new string[] { "." }, StringSplitOptions.None)[1];
		string postUrl = "https://story.kakao.com/" + userId + "/" + postId;
		var dataPackage = new DataPackage();
		dataPackage.SetText(postUrl);
		Clipboard.SetContent(dataPackage);
		await Utility.ShowMessageDialogAsync("포스트의 URL이 클립보드에 복사되었습니다", "안내");
	}

	public void HideEmotionsButtonFlyout() => BtEmotions.Flyout.Hide();

	private async void OnUpButtonClicked(object sender, RoutedEventArgs e)
	{
		BtUp.IsEnabled = false;
		var isUp = _post.sympathized;
		await ApiHandler.UpPost(_post.id, isUp);
		await RefreshContent();
		BtUp.IsEnabled = true;
	}

	public async Task RefreshContent()
	{
		_post = await ApiHandler.GetPost(PostId);
		RefreshUpButton();
		RefreshEmotionsButton();
		await Control.RefreshContent();
	}

	private double GetScaleAdjustment()
	{
		IntPtr hWnd = WindowNative.GetWindowHandle(this);
		WindowId wndId = Win32Interop.GetWindowIdFromWindow(hWnd);
		DisplayArea displayArea = DisplayArea.GetFromWindowId(wndId, DisplayAreaFallback.Primary);
		IntPtr hMonitor = Win32Interop.GetMonitorFromDisplayId(displayArea.DisplayId);

		// Get DPI.
		int result = Utility.GetDpiForMonitor(hMonitor, 0, out uint dpiX, out uint _);
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

	private void TitleBarGridLoaded(object sender, RoutedEventArgs e) => SetDragRegionForTitleBarGrid();
	private void TitleBarGridSizeChanged(object sender, SizeChangedEventArgs e) => SetDragRegionForTitleBarGrid();

	public static bool HasInstanceContainsId(string id) => s_instances.Any(x => x.PostId == id);
	public static TimelineWindow FindTimelineWindowByPostId(string id) => s_instances.FirstOrDefault(x => x.PostId == id);
	public static TimelineWindow GetTimelineWindow(PostData postData) => s_instances.FirstOrDefault(x => x.PostId == postData.id) ?? new TimelineWindow(postData);

	private void OnWindowClosed(object sender, WindowEventArgs args)
	{
		var appWindow = AppWindow;
		var presenter = appWindow?.Presenter as OverlappedPresenter;
		var isMaximized = presenter?.State == OverlappedPresenterState.Maximized;

		var windowState = Configuration.GetValue("timelineWindowState") as WindowState ?? new();

		windowState.WasMaxmized = isMaximized;
		if (!isMaximized)
		{
			var width = Width;
			var height = Height;
			windowState.Width = (int)width;
			windowState.Height = (int)height;
		}
		Configuration.SetValue("timelineWindowState", windowState);

		s_instances.Remove(this);
	}

	private async void OnPreviewKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
	{
		var isControlDown = Common.IsModifierDown();

		if (e.Key == Windows.System.VirtualKey.Escape)
			Close();
		if ((isControlDown && e.Key == Windows.System.VirtualKey.R) || e.Key == Windows.System.VirtualKey.F5)
			await Control.RefreshContent(true);
		else if (isControlDown && e.Key == Windows.System.VirtualKey.S)
			await Control.SharePost();
		else if (isControlDown && e.Key == Windows.System.VirtualKey.E)
			await Control.EditPost();
		else if (isControlDown && e.Key == Windows.System.VirtualKey.D)
			await Control.DeletePost();
		//else if (isControlDown && e.Key == Windows.System.VirtualKey.W)
		//	Close();
	}

	private async void OnRefreshButtonClicked(object sender, RoutedEventArgs e) => await RefreshContent();
}
