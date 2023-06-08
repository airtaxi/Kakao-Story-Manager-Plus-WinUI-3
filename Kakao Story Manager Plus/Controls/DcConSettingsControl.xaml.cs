using KSMP.Api.DcCon;
using KSMP.Extension;
using KSMP.Pages;
using KSMP.Utils;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using static KSMP.Controls.InputControl;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace KSMP.Controls
{
    public sealed partial class DcConSettingsControl : UserControl
    {
        private const string DcConUrl = "https://dccon.dcinside.com/";

        public DcConSettingsControl()
        {
            this.InitializeComponent();
            var list = Utility.GetCurrentDcConList();
            var collection = new ObservableCollection<DataType.Package>(list);
            collection.CollectionChanged += OnDcConListCollectionChanged;
			{
				RoutedEventHandler unloaded = null;
				unloaded = (s, e) =>
				{
					collection.CollectionChanged -= OnDcConListCollectionChanged;
                    Unloaded -= unloaded;
				};
				Unloaded += unloaded;
			}
			LvMain.ItemsSource = collection;
        }

        private void OnShowDcConListButtonClicked(object sender, RoutedEventArgs e) => Process.Start(new ProcessStartInfo("https://dccon.dcinside.com/") { UseShellExecute = true });

        private async void OnAddDcConButtonClicked(object sender, RoutedEventArgs e)
        {
            var url = TbxUrl.Text;
            var isValidUrl = url.StartsWith(DcConUrl);
            int id = 0;
            var idString = url = url.Substring(url.IndexOf('#') + 1);
            isValidUrl = isValidUrl && int.TryParse(idString, out id);
            if (!isValidUrl)
            {
                await Utility.ShowMessageDialogAsync("입력된 디시콘 주소가 올바르지 않습니다", "오류");
                return;
            }

            var detail = await Api.DcCon.ApiHandler.GetDcDonPackageDetailAsync(id);
            if(detail == null)
            {
                await Utility.ShowMessageDialogAsync("디시콘 파싱 과정 중 오류가 발생하였습니다.", "오류");
                return;
            }

            var data = Configuration.GetValue("DcConList") as JArray ?? new();
            var collection = data.ToObject<ObservableCollection<DataType.Package>>();
            if (collection.Any(x => x.PackageInfo.PackageIndex == detail.PackageInfo.PackageIndex))
            {
                await Utility.ShowMessageDialogAsync("이미 추가된 디시콘입니다.", "오류");
                return;
            }
            collection.Add(detail);
            collection.CollectionChanged += OnDcConListCollectionChanged;
			{
				RoutedEventHandler unloaded = null;
				unloaded = (s, e) =>
				{
					collection.CollectionChanged -= OnDcConListCollectionChanged;
                    Unloaded -= unloaded;
				};
				Unloaded += unloaded;
			}
			LvMain.ItemsSource = collection;
            Configuration.SetValue("DcConList", collection);
        }

        private void OnDcConListCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            var collection = sender as ObservableCollection<DataType.Package>;
            Configuration.SetValue("DcConList", collection);
        }

        private void OnListViewSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listView = sender as ListView;
            var item = listView.SelectedItem as DataType.Package;
            BtDelete.IsEnabled = item != null;
        }

        private void OnDeleteDcConButtonClicked(object sender, RoutedEventArgs e)
        {
            var list = Utility.GetCurrentDcConList();
            var current = LvMain.SelectedItem as DataType.Package;
            if (current == null) return;
            list.RemoveAll(x => x.PackageInfo.PackageIndex == current.PackageInfo.PackageIndex);
            var collection = new ObservableCollection<DataType.Package>(list);
            collection.CollectionChanged += OnDcConListCollectionChanged;
			{
				RoutedEventHandler unloaded = null;
				unloaded = (s, e) =>
				{
					collection.CollectionChanged -= OnDcConListCollectionChanged;
                    Unloaded -= unloaded;
				};
				Unloaded += unloaded;
			}
			LvMain.ItemsSource = collection;
            Configuration.SetValue("DcConList", collection);
        }

        private void OnExitButtonClicked(object sender, TappedRoutedEventArgs e) => MainPage.HideOverlay(false);
    }
}
