using KSMP.Controls;
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
	public TimelineWindow(PostData postData)
	{
		InitializeComponent();
		AppWindow.SetIcon(Path.Combine(App.BinaryDirectory, "icon.ico"));
		FrMain.Content = new TimelineControl(this, postData, false, true);
	}
}
