using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
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
    public sealed partial class ExamRoom : Page
    {
        private StorageFolder localFolder = ApplicationData.Current.LocalFolder;

        private static bool again = true;

        public ExamRoom()
        {
            this.InitializeComponent();

            IniList();
        }

        private async void IniList()
        {
            //Get token
            try
            {
                //from storage
                StorageFile usrInfo = await localFolder.GetFileAsync("usrInfo.txt");
                string token = (await FileIO.ReadTextAsync(usrInfo)).Split('\n')[1];
                //Get data
                HttpFormUrlEncodedContent content = new HttpFormUrlEncodedContent(new[] { new KeyValuePair<string, string>("token", token) });
                string jsonData = await HttpRequest.GetFromJwch("get","getExamRoom", content);
                ExamRoomReturnValue errv = JsonConvert.DeserializeObject<ExamRoomReturnValue>(jsonData);
                //System.Diagnostics.Debug.WriteLine(errv.data["stuname"]);
                examArr = JsonConvert.DeserializeObject<List<ExamRoomArr>>(errv.data["examArr"].ToString());
                System.Diagnostics.Debug.WriteLine(examArr);
                //System.Diagnostics.Debug.WriteLine(examArr.ElementAt<Dictionary<string,string>>(0)["courseName"]);
                listView.ItemsSource = examArr;
            }
            catch
            {
                if (again)
                {
                    again = false;
                    await HttpRequest.ReLogin();
                    IniList();
                }
                else
                {
                    MainPage.SendToast("网络错误");
                }
                return;
            }
        }

        private class ExamRoomReturnValue
        {
            public bool status { get; set; }

            public string errMsg { get; set; }

            public PropertySet data { get; set; }
        }

        private class ExamRoomArr
        {
            public string courseName { get; set; }

            public string teacherName { get; set; }

            public string examDate { get; set; }

            public string examTime { get; set; }

            public string examRoom { get; set; }
        }

        private List<ExamRoomArr> examArr;
    }
}
