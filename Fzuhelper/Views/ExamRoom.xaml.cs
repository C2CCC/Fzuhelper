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
using HtmlAgilityPack;
using System.Text.RegularExpressions;
using Fzuhelper.Controls;

// “空白页”项模板在 http://go.microsoft.com/fwlink/?LinkId=234238 上提供

namespace Fzuhelper.Views
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class ExamRoom : Page
    {
        //private StorageFolder fzuhelperDataFolder = await ApplicationData.Current.LocalFolder.GetFolderAsync("FzuhelperData");

        //private static bool getAgain = true;

        //private static bool firstTimeLoad = true;

        //private string jsonData;

        private string htmlStr;

        //private ExamRoomReturnValue errv;

        private List<ExamRoomArr> examArr = new List<ExamRoomArr>();

        public ExamRoom()
        {
            this.InitializeComponent();

            //IniList(false);

            InitExamRoom(false);
        }

        private async void InitExamRoom(bool IsRefresh)
        {
            refreshIndicator.IsActive = true;

            await MockGet(IsRefresh);

            FormatExamRoom();

            refreshIndicator.IsActive = false;
        }

        private async Task MockGet(bool IsRefresh)
        {
            if (IsRefresh)
            {
                bool status = await MockJwch.MockGetExamRoom();
                if (!status)
                {
                    NotifyPopup notifyPopup = new NotifyPopup("获取考场失败");
                    notifyPopup.Show();
                    return;
                }
                try
                {
                    //Get data from storage
                    StorageFolder fzuhelperDataFolder = await ApplicationData.Current.LocalFolder.GetFolderAsync("FzuhelperData");
                    StorageFile examroom = await fzuhelperDataFolder.GetFileAsync("examroom.dat");
                    htmlStr = await FileIO.ReadTextAsync(examroom);
                }
                catch
                {

                }
            }
            else
            {
                try
                {
                    //Get data from storage
                    StorageFolder fzuhelperDataFolder = await ApplicationData.Current.LocalFolder.GetFolderAsync("FzuhelperData");
                    StorageFile examroom = await fzuhelperDataFolder.GetFileAsync("examroom.dat");
                    htmlStr = await FileIO.ReadTextAsync(examroom);
                }
                catch
                {
                    bool status = await MockJwch.MockGetExamRoom();
                    if (!status)
                    {
                        NotifyPopup notifyPopup = new NotifyPopup("获取考场失败");
                        notifyPopup.Show();
                    }
                    else
                    {
                        await MockGet(false);
                    }
                }
            }
        }

        private void FormatExamRoom()
        {
            try
            {
                examArr.Clear();
                htmlStr = htmlStr.Replace("\n", "");
                htmlStr = htmlStr.Replace("\r", "");
                htmlStr = htmlStr.Replace("\t", "");
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(htmlStr);
                HtmlNode examroomTableNode = doc.GetElementbyId("ContentPlaceHolder1_DataList_xxk");
                HtmlNodeCollection trs = examroomTableNode.ChildNodes;
                trs.First().Remove();
                //trs.Last().Remove();

                foreach (HtmlNode node in trs)
                {
                    foreach (HtmlNode tdNode in node.ChildNodes)
                    {
                        if(tdNode.InnerHtml == "")
                        {
                            continue;
                        }
                        tdNode.FirstChild.Remove();
                        string courseName = tdNode.FirstChild.ChildNodes[1].InnerText;
                        string teacherName = tdNode.FirstChild.ChildNodes[5].InnerText;
                        string dtr = tdNode.FirstChild.ChildNodes[7].InnerText;
                        dtr = dtr.Replace(" ", "");
                        string examDate;
                        string examTime;
                        string examRoom;
                        if (dtr == "")
                        {
                            examDate = "";
                            examTime = "";
                            examRoom = "";
                        }
                        else
                        {
                            string[] dateTimeRoom = Regex.Split(dtr, "&nbsp;&nbsp;&nbsp;&nbsp;");
                            examDate = dateTimeRoom[0];
                            examTime = dateTimeRoom[1];
                            examRoom = dateTimeRoom[2];
                        }
                        ExamRoomArr erai = new ExamRoomArr(courseName, teacherName, examDate, examTime, examRoom);
                        examArr.Add(erai);
                    }
                }
                listView.ItemsSource = null;
                listView.ItemsSource = examArr;
            }
            catch
            {

            }
        }

        /*private async void IniList(bool IsRefresh)
        {
            refreshIndicator.IsActive = true;
            if (!IsRefresh)
            {
                try
                {
                    //Get data from storage
                    StorageFolder fzuhelperDataFolder = await ApplicationData.Current.LocalFolder.GetFolderAsync("FzuhelperData");
                    StorageFile examRoom = await fzuhelperDataFolder.GetFileAsync("examRoom.dat");
                    jsonData = await FileIO.ReadTextAsync(examRoom);
                }
                catch
                {
                    if (firstTimeLoad)
                    {
                        IniList(true);
                        firstTimeLoad = false;
                        return;
                    }
                    MainPage.SendToast("获取数据出错，请刷新");
                    refreshIndicator.IsActive = false;
                    return;
                }
            }
            else
            {
                try
                {
                    jsonData = await HttpRequest.GetExamRoom();
                    if (jsonData == "error")
                    {
                        MainPage.SendToast("获取数据出错");
                        refreshIndicator.IsActive = false;
                        return;
                    }
                }
                catch
                {
                    MainPage.SendToast("获取数据出错");
                    refreshIndicator.IsActive = false;
                    return;
                }
            }
            try
            {
                errv = JsonConvert.DeserializeObject<ExamRoomReturnValue>(jsonData);
                //System.Diagnostics.Debug.WriteLine(errv.data["stuname"]);
                examArr = JsonConvert.DeserializeObject<List<ExamRoomArr>>(errv.data["examArr"].ToString());
                listView.ItemsSource = examArr;
                refreshIndicator.IsActive = false;
                //Unknown error, just login again
                if (examArr.Count() == 0 && getAgain)
                {
                    getAgain = false;
                    await HttpRequest.ReLogin();
                    IniList(true);
                }
            }
            catch
            {
                getAgain = true;
                MainPage.SendToast("获取数据出错，请刷新");
            }
            refreshIndicator.IsActive = false;
        }*/

        private class ExamRoomReturnValue
        {
            public bool status { get; set; }

            public string errMsg { get; set; }

            public PropertySet data { get; set; }
        }

        private void refreshExamRoom_Click(object sender, RoutedEventArgs e)
        {
            InitExamRoom(true);
        }

        private class ExamRoomArr
        {
            public ExamRoomArr() { }

            public ExamRoomArr(string courseName,string teacherName,string examDate,string examTime,string examRoom)
            {
                this.courseName = courseName;
                this.teacherName = teacherName;
                this.examDate = examDate;
                this.examTime = examTime;
                this.examRoom = examRoom;
            }

            public string courseName { get; set; }

            public string teacherName { get; set; }

            public string examDate { get; set; }

            public string examTime { get; set; }

            public string examRoom { get; set; }
        }
    }
}
