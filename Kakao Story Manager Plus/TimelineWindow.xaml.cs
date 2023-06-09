using KSMP.Controls;
using KSMP.Utils;
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
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using static KSMP.ApiHandler.DataType.CommentData;

namespace KSMP;

public sealed partial class TimelineWindow : Window
{
	private static List<TimelineWindow> s_instances = new();
	public string PostId { get; private set; }
	private TimelineWindow(PostData postData)
	{
		s_instances.Add(this);
		PostId = postData.id;

		InitializeComponent();
		AppWindow.SetIcon(Path.Combine(App.BinaryDirectory, "icon.ico"));
		FrMain.Content = new TimelineControl(this, postData, false, true);
	}

	public static TimelineWindow GetTimelineWindow(PostData postData) => s_instances.FirstOrDefault(x => x.PostId == postData.id) ?? new TimelineWindow(postData);

	private void OnWindowClosed(object sender, WindowEventArgs args) => s_instances.Remove(this);
}
