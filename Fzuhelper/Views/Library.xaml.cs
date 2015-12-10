using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
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
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class Library : Page
    {
        private string key = "";

        private static int page = 1;

        private string jsonData = "";

        private BookSearchResult b = new BookSearchResult();

        private BookSearchResult l = new BookSearchResult();

        public Library()
        {
            this.InitializeComponent();
        }

        private async void bookSearch_Click(object sender, RoutedEventArgs e)
        {
            refreshIndicator.IsActive = true;
            bool flag;
            page = 1;
            key = bookName.Text;
            jsonData = await HttpRequest.GetBookSearchResult(key, page.ToString());
            flag = ShowResult(false);
            if (flag)
            {
                loadMore.Content = "加载更多";
                loadMore.IsEnabled = true;
                loadMore.Visibility = Visibility.Visible;
            }
            else
            {
                loadMore.Visibility = Visibility.Collapsed;
            }
            refreshIndicator.IsActive = false;
        }

        private bool ShowResult(bool IsNextPage)
        {
            if (!IsNextPage)
            {
                bookSearchResult.ItemsSource = null;
                try
                {
                    b = JsonConvert.DeserializeObject<BookSearchResult>(jsonData);
                }
                catch
                {
                    return false;
                }
            }
            if(b.errMsg == "图书馆请求信息失败")
            {
                MainPage.SendToast(b.errMsg);
                return false;
            }
            bookSearchResult.ItemsSource = b.data;
            page++;
            return true;
        }

        private async void loadMore_Click(object sender, RoutedEventArgs e)
        {
            refreshIndicator.IsActive = true;
            jsonData = await HttpRequest.GetBookSearchResult(key, page.ToString());
            try
            {
                l = JsonConvert.DeserializeObject<BookSearchResult>(jsonData);
            }
            catch
            {
                refreshIndicator.IsActive = false;
                return;
            }
            b.errMsg = l.errMsg;
            if (b.errMsg == "图书馆请求信息失败")
            {
                loadMore.Content = "没有更多内容";
                loadMore.IsEnabled = false;
                refreshIndicator.IsActive = false;
                return;
            }
            b.data = b.data.Concat(l.data).ToList();
            //System.Diagnostics.Debug.WriteLine(b.data.Count);
            ShowResult(true);
            refreshIndicator.IsActive = false;
        }

        private class BookSearchResult
        {
            public bool status { get; set; }

            public string errMsg { get; set; }

            public List<BookInfo> data { get; set; }
        }

        private class BookInfo
        {
            public string name { get; set; }

            public string author { get; set; }

            public string publisher { get; set; }

            public string place { get; set; }

            public string amount { get; set; }
        }

        private void bookName_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                bookSearch_Click(new object(), new RoutedEventArgs());
            }
        }
    }
}
