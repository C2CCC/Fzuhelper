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
using System.Threading.Tasks;

// “空白页”项模板在 http://go.microsoft.com/fwlink/?LinkId=234238 上提供

namespace Fzuhelper.Views
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class ExamRoom : Page
    {
        //private StorageFolder fzuhelperDataFolder = await ApplicationData.Current.LocalFolder.GetFolderAsync("FzuhelperData");

        private static bool initialAgain = true;
        
        private string jsonData;

        private ExamRoomReturnValue errv;

        private List<ExamRoomArr> examArr;

        public ExamRoom()
        {
            this.InitializeComponent();

            IniList();
        }

        private async void IniList()
        {
            try
            {
                refreshIndicator.IsActive = true;
                //Get data from storage
                StorageFolder fzuhelperDataFolder = await ApplicationData.Current.LocalFolder.GetFolderAsync("FzuhelperData");
                StorageFile examRoom = await fzuhelperDataFolder.GetFileAsync("examRoom.dat");
                jsonData = await FileIO.ReadTextAsync(examRoom);
                initialAgain = true;
                errv = JsonConvert.DeserializeObject<ExamRoomReturnValue>(jsonData);
                //System.Diagnostics.Debug.WriteLine(errv.data["stuname"]);
                examArr = JsonConvert.DeserializeObject<List<ExamRoomArr>>(errv.data["examArr"].ToString());
                listView.ItemsSource = examArr;
                refreshIndicator.IsActive = false;
            }
            catch
            {
                refreshIndicator.IsActive = false;
                if (initialAgain)
                {
                    refreshIndicator.IsActive = true;
                    initialAgain = !initialAgain;
                    jsonData = await HttpRequest.GetExamRoom();
                    IniList();
                }
                else
                {
                    //MainPage.SendToast("无法获取列表");
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

        private async void refreshExamRoom_Click(object sender, RoutedEventArgs e)
        {
            refreshIndicator.IsActive = true;
            await HttpRequest.GetExamRoom();
            refreshIndicator.IsActive = false;
            IniList();
        }

        private class ExamRoomArr
        {
            public string courseName { get; set; }

            public string teacherName { get; set; }

            public string examDate { get; set; }

            public string examTime { get; set; }

            public string examRoom { get; set; }
        }
    }
}
