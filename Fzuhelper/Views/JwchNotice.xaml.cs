using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http;
using Newtonsoft.Json;

// “空白页”项模板在 http://go.microsoft.com/fwlink/?LinkId=234238 上提供

namespace Fzuhelper.Views
{
    public class TurnRed : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            string isRed = (string)value;
            return (isRed == "true") ? new SolidColorBrush(Color.FromArgb(255, 255, 0, 0)) : new SolidColorBrush(Color.FromArgb(255, 0, 0, 0));
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class JwchNotice : Page
    {
        private static int page = 1;

        private string jsonData = "";

        private JwchNoticeResult j = new JwchNoticeResult();

        public JwchNotice()
        {
            this.InitializeComponent();

            InitialLoad();
        }

        private async void InitialLoad()
        {
            refreshIndicator.IsActive = true;
            page = 1;
            jsonData = await HttpRequest.GetJwchNotice(page.ToString());
            ShowResult(false);
            //loadMore.Content = "加载更多";
            //loadMore.IsEnabled = true;
            //loadMore.Visibility = Visibility.Visible;
            refreshIndicator.IsActive = false;
        }

        private void ShowResult(bool IsNextPage)
        {
            try
            {
               if (!IsNextPage)
                {
                   jwchNoticeListView.ItemsSource = null;
                   j = JsonConvert.DeserializeObject<JwchNoticeResult>(jsonData);
                }
                if (j.errMsg != "")
                {
                   return;
                }
                jwchNoticeListView.ItemsSource = j.data;
                page++;
            }
            catch
            {

            }
        }

        private void refreshJwchNotice_Click(object sender, RoutedEventArgs e)
        {
            InitialLoad();
        }

        private async void jwchNoticeListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            JwchInfo lvi = (JwchInfo)e.ClickedItem;
            string url = lvi.url;
            Uri uri = new Uri(url);
            try
            {
                await Launcher.LaunchUriAsync(uri);
            }
            catch
            {

            }
        }

        /*private async void loadMore_Click(object sender, RoutedEventArgs e)
        {
            refreshIndicator.IsActive = true;
            jsonData = await HttpRequest.GetJwchNotice(page.ToString());
            JwchNoticeResult l = JsonConvert.DeserializeObject<JwchNoticeResult>(jsonData);
            j.errMsg = l.errMsg;
            if (j.errMsg != "")
            {
                loadMore.Content = "没有更多内容";
                loadMore.IsEnabled = false;
                refreshIndicator.IsActive = false;
                return;
            }
            j.data = j.data.Concat(l.data).ToList();
            //System.Diagnostics.Debug.WriteLine(b.data.Count);
            ShowResult(true);
            refreshIndicator.IsActive = false;
        }*/

        private class JwchNoticeResult
        {
            public bool status { get; set; }

            public string errMsg { get; set; }

            public List<JwchInfo> data { get; set; }
        }

        private class JwchInfo
        {
            public string title { get; set; }

            public string date { get; set; }

            public string isRed { get; set; }

            public string url { get; set; }
        }

    }
}
