using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using static KSMP.ClassManager;
using static StoryApi.ApiHandler.DataType;

namespace KSMP.Controls;

public sealed partial class InputControl : UserControl
{
    private class Quote
    {
        public string type = "text";
        public string id = null;
        public string text = null;
    }

    public delegate void ImagePasted(string temporaryImageFilePath);
    public delegate void SubmitShortcutActivated();
    public SubmitShortcutActivated OnSubmitShortcutActivated;
    public ImagePasted OnImagePasted;

    public InputControl(string placeholder = null)
    {
        InitializeComponent();
        var control = new FriendListControl();
        PuDropdown.Child = control;
        control.OnFriendSelected += OnFriendSelected;
        TbxMain.PlaceholderText = placeholder ?? "";
    }

    private void OnFriendSelected(FriendProfile profile)
    {
        if(profile != null)
        {
            PuDropdown.IsOpen = false;
            var text = TbxMain.Text;
            var before = text[..text.IndexOf("@")];
            TbxMain.Text = before + "{!{{" + "\"id\":\"" + profile.Id + "\", \"type\":\"profile\", \"text\":\"" + profile.Name + "\"}}!} ";
            TbxMain.Focus(FocusState.Keyboard);
            TbxMain.SelectionStart = TbxMain.Text.Length;
            TbxMain.SelectionLength = 0;
        }
    }

    public TextBox GetTextBox() => TbxMain;
    public void SetWidth(double width) => TbxMain.Width = width;
    public void SetMinHeight(double minHeight) => TbxMain.MinHeight = minHeight;
    public void SetMaxHeight(double maxHeight) => TbxMain.MaxHeight = maxHeight;
    public void AcceptReturn(bool willAllow) => TbxMain.AcceptsReturn = willAllow;
    public void WrapText(bool willWrap) => TbxMain.TextWrapping = willWrap ? TextWrapping.Wrap : TextWrapping.NoWrap;

    public List<QuoteData> GetQuoteDatas() => StoryApi.Utils.GetQuoteDataFromString(TbxMain.Text);
    private void ShowNameSuggestion(string name)
    {
        var friendListControl = PuDropdown.Child as FriendListControl;
        var count = friendListControl.SearchFriendList(name);
        if (PuDropdown.IsOpen && (count == 0 || string.IsNullOrEmpty(name)))
            PuDropdown.IsOpen = false;
        else if (!PuDropdown.IsOpen)
            PuDropdown.IsOpen = true;
    }

    private void TextboxTextChanged(object sender, RoutedEventArgs e)
    {
        var text = TbxMain.Text;
        if (text.Contains('@'))
        {
            var name = text[(text.IndexOf("@") + 1)..];
            ShowNameSuggestion(name);
        }
        else PuDropdown.IsOpen = false;
    }

    public void FocusTextBox()
    {
        TbxMain.Focus(FocusState.Keyboard);
        TbxMain.Select(TbxMain.Text.Length, 0);
    }

    public void AppendText(string append)
    {
        TbxMain.Text += append;
        FocusTextBox();
    }

    private async void OnTextBoxPreviewKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        var textBox = sender as TextBox;
        var isControlDown = Utils.Common.IsModifierDown();
        if (isControlDown && e.Key == Windows.System.VirtualKey.Enter)
        {
            e.Handled = true;
            OnSubmitShortcutActivated?.Invoke();
        }
        else if (isControlDown && e.Key == Windows.System.VirtualKey.V)
        {
            var view = Clipboard.GetContent();
            DataPackageView dataPackageView = Clipboard.GetContent();
            var hasImage = dataPackageView.Contains(StandardDataFormats.Bitmap);
            if (hasImage)
            {
                var randomFilePath = Path.GetTempFileName();
                var rawImage = await dataPackageView.GetBitmapAsync();
                using var imageStream = await rawImage.OpenReadAsync();
                var stream = await StorageFile.GetFileFromPathAsync(randomFilePath);
                var writeStream = await stream.OpenStreamForWriteAsync();
                await imageStream.AsStreamForRead().CopyToAsync(writeStream);
                writeStream.Close();

                e.Handled = true;
                OnImagePasted?.Invoke(randomFilePath);
            }
        }
    }

    private async void OnDropped(object sender, DragEventArgs e)
    {
        if (!e.DataView.Contains(StandardDataFormats.StorageItems)) return;
        var items = await e.DataView.GetStorageItemsAsync();
        if (items == null) return;

        foreach(var item in items) 
            if (item?.Path != null) OnImagePasted?.Invoke(item.Path);
    }

    private void OnDragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = DataPackageOperation.Copy;
        e.DragUIOverride.Caption = "미디어 끌어오기";
        e.DragUIOverride.IsCaptionVisible = true;
    }
}
