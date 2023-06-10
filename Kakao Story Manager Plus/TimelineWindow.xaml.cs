using KSMP.Controls;
using Microsoft.UI.Xaml;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static KSMP.ApiHandler.DataType.CommentData;

namespace KSMP;

public sealed partial class TimelineWindow : Window
{
	private static List<TimelineWindow> s_instances = new();

	public TimelineControl Control;
	public string PostId { get; private set; }

	private TimelineWindow(PostData postData)
	{
		s_instances.Add(this);
		PostId = postData.id;

		InitializeComponent();

		AppWindow.SetIcon(Path.Combine(App.BinaryDirectory, "icon.ico"));
		Control = new TimelineControl(this, postData, false, true);
		FrMain.Content = Control;
	}

	public static bool HasInstanceContainsId(string id) => s_instances.Any(x => x.PostId == id);
	public static TimelineWindow GetTimelineWindow(PostData postData) => s_instances.FirstOrDefault(x => x.PostId == postData.id) ?? new TimelineWindow(postData);

	private void OnWindowClosed(object sender, WindowEventArgs args) => s_instances.Remove(this);
}
