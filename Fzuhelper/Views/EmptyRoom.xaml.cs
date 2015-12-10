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
using Newtonsoft.Json;
using Windows.Storage;

// “空白页”项模板在 http://go.microsoft.com/fwlink/?LinkId=234238 上提供

namespace Fzuhelper.Views
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class EmptyRoom : Page
    {
        private ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

        private string jsonData = "";

        private Dictionary<string, string> errv = new Dictionary<string, string>();

        private List<string> resultList = new List<string>();

        public EmptyRoom()
        {
            this.InitializeComponent();
        }

        private async void EmptyRoomButton_Click(object sender, RoutedEventArgs e)
        {
            refreshIndicator.IsActive = true;
            emptyRoomSearchResultView.ItemsSource = null;
            resultList.Clear();
            string time = datePicker.Date.Year.ToString() + "-" + (datePicker.Date.Month < 10 ? "0" + datePicker.Date.Month.ToString() : datePicker.Date.Month.ToString()) + "-" + (datePicker.Date.Day < 10 ? "0" + datePicker.Date.Day.ToString() : datePicker.Date.Day.ToString());
            string place = "";
            switch (placeSelector.SelectedValue.ToString())
            {
                case "西三":
                    place = "w3";
                    break;
                case "西二":
                    place = "w2";
                    break;
                case "西一":
                    place = "w1";
                    break;
                case "中楼":
                    place = "m";
                    break;
                case "东一":
                    place = "e1";
                    break;
                case "东二":
                    place = "e2";
                    break;
                case "东三":
                    place = "e3";
                    break;
                case "文楼":
                    place = "w";
                    break;
                default:
                    break;
            }
            string star = beginJie.SelectedValue.ToString();
            string end = endJie.SelectedValue.ToString();
            string term = localSettings.Values["term"].ToString().Substring(0, 4) + localSettings.Values["term"].ToString().Substring(6, 2);

            jsonData = await HttpRequest.GetEmptyRoom(time, place, star, end, term);
            try
            {
                errv = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonData);
                foreach(string key in errv.Keys)
                {
                    if (errv["sum"] == "0")
                    {
                        resultList.Add("无教室");
                        break;
                    }
                    if (key != "sum")
                    {
                        resultList.Add(errv[key]);
                    }
                }
                emptyRoomSearchResultView.ItemsSource = resultList;
            }
            catch
            {

            }
            refreshIndicator.IsActive = false;
        }

    }
}
