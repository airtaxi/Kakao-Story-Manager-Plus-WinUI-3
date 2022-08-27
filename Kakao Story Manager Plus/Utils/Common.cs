﻿using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Diagnostics;
using System.Reflection;
using Windows.ApplicationModel;
using Windows.UI;
using Windows.UI.Core;

namespace KSMP.Utils
{
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

        public static string GetVersionString()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
            return fileVersionInfo.ProductVersion;
        }

        public static bool IsModifierDown(Windows.System.VirtualKey virtualKey = Windows.System.VirtualKey.Control)
        {
            var state = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(virtualKey);
            var isDown = state == (CoreVirtualKeyStates.Down | CoreVirtualKeyStates.Locked);
            isDown = isDown || state == CoreVirtualKeyStates.Down;
            return isDown;
        }
    }
}
