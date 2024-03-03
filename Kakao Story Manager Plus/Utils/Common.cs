using Microsoft.UI.Xaml.Media;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Windows.UI;
using Windows.UI.Core;

namespace KSMP.Utils;

public static class Common
{
    public static SolidColorBrush GetColorFromHexa(string hexaColor)
    {
        return new SolidColorBrush(
            Color.FromArgb(
                Convert.ToByte(hexaColor.Substring(1, 2), 16),
                Convert.ToByte(hexaColor.Substring(3, 2), 16),
                Convert.ToByte(hexaColor.Substring(5, 2), 16),
                Convert.ToByte(hexaColor.Substring(7, 2), 16)
            )
        );
    }

    public static string GetVersionString() => Assembly.GetExecutingAssembly().GetName().Version.ToString();

    public static bool IsModifierDown(Windows.System.VirtualKey virtualKey = Windows.System.VirtualKey.Control)
    {
        var state = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(virtualKey);
        var isDown = state == (CoreVirtualKeyStates.Down | CoreVirtualKeyStates.Locked);
        isDown = isDown || state == CoreVirtualKeyStates.Down;
        return isDown;
    }
}
