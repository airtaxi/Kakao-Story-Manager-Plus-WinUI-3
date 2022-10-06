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

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace KSMP.Controls.Flyouts
{
    public sealed partial class PasteClipboardImageQueryControl : UserControl
    {
        public delegate void ButtonClickDelegate();
        public ButtonClickDelegate OnClose;
        public ButtonClickDelegate OnPaste;
        public PasteClipboardImageQueryControl()
        {
            InitializeComponent();
        }

        private void OnPasteButtonClicked(object sender, RoutedEventArgs e) => OnPaste?.Invoke();
        private void OnCloseButtonClicked(object sender, RoutedEventArgs e) => OnClose?.Invoke();
    }
}
