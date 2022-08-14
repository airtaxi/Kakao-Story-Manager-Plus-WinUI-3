using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace KSMP.Controls;

public sealed partial class WritePostControl : UserControl
{
    private readonly InputControl _inputControl;
    private readonly Button _button;
    public delegate void PostCompleted();
    public PostCompleted OnPostCompleted;

    public WritePostControl(Button button = null)
    {
        InitializeComponent();
        _inputControl = new InputControl();
        _button = button;
        FrInputControl.Content = _inputControl;
        _inputControl.SetMinHeight(200);
        _inputControl.SetWidth(Width);
        _inputControl.SetMaxHeight(300);
        _inputControl.AcceptReturn(true);
        _inputControl.WrapText(true);
    }

    public InputControl GetInputControl() => _inputControl;

    private readonly List<string> _permissons = new()
    {
        "A",
        "F",
        "P",
        "M"
    };

    private async void OnWriteButtonClicked(object sender, RoutedEventArgs e) => await WritePostAsync();

    public async Task WritePostAsync()
    {
        var quoteDatas = StoryApi.Utils.GetQuoteDataFromString(_inputControl.GetTextBox().Text);
        PbMain.Visibility = Visibility.Visible;
        await StoryApi.ApiHandler.WritePost(quoteDatas, null, _permissons[CbxPermission.SelectedIndex], true, true, null, null);
        PbMain.Visibility = Visibility.Collapsed;
        _button?.Flyout.Hide();
        OnPostCompleted.Invoke();
    }
}
