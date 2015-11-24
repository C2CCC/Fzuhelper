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
using Newtonsoft.Json;
using System.Threading.Tasks;
using Windows.Web.Http;
using System.Collections.ObjectModel;

// “空白页”项模板在 http://go.microsoft.com/fwlink/?LinkId=234238 上提供

namespace Fzuhelper.Views
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class Score : Page
    {
        private StorageFolder localFolder = ApplicationData.Current.LocalFolder;

        private static bool initialAgain = true;

        private string jsonData;

        private ScoreReturnValue srv;

        private static List<ScoreArr> markArr;

        private ObservableCollection<Group> Groups;

        public Score()
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
                StorageFile score = await localFolder.GetFileAsync("score.txt");
                jsonData = await FileIO.ReadTextAsync(score);
                //System.Diagnostics.Debug.WriteLine(jsonData);
                srv = JsonConvert.DeserializeObject<ScoreReturnValue>(jsonData);
                //System.Diagnostics.Debug.WriteLine(srv.data["markArr"]);
                markArr = JsonConvert.DeserializeObject<List<ScoreArr>>(srv.data["markArr"].ToString());
                Groups = CreateGroups();
                cvsGroups.Source = Groups;
                listViewZoomOutView.ItemsSource = cvsGroups.View.CollectionGroups;
                initialAgain = true;
                refreshIndicator.IsActive = false;
            }
            catch
            {
                refreshIndicator.IsActive = false;
                if (initialAgain)
                {
                    initialAgain = !initialAgain;
                    await HttpRequest.GetScore();
                    IniList();
                }
                else
                {
                    MainPage.SendToast("无法获取列表");
                }
                return;
            }
        }

        /*private async Task<string> GetScore()
        {
            //Get token
            try
            {
                //from storage
                StorageFile usrInfo = await localFolder.GetFileAsync("usrInfo.txt");
                string token = (await FileIO.ReadTextAsync(usrInfo)).Split('\n')[1];
                //Get data
                HttpFormUrlEncodedContent content = new HttpFormUrlEncodedContent(new[] { new KeyValuePair<string, string>("token", token) });
                jsonData = await HttpRequest.GetFromJwch("get", "getScore", content);
                //System.Diagnostics.Debug.WriteLine(examArr.ElementAt<Dictionary<string,string>>(0)["courseName"]);
                try
                {
                    //Save as file
                    StorageFile score = await localFolder.CreateFileAsync("score.txt", CreationCollisionOption.ReplaceExisting);
                    await FileIO.WriteTextAsync(score, jsonData);
                }
                catch
                {

                }
                getAgain = true;
                return "";
            }
            catch
            {
                if (getAgain)
                {
                    getAgain = !getAgain;
                    await HttpRequest.ReLogin();
                    await GetScore();
                }
                else
                {
                    MainPage.SendToast("网络错误");
                }
                return "";
            }
        }*/

        private class ScoreReturnValue
        {
            public bool status { get; set; }

            public string errMsg { get; set; }

            public PropertySet data { get; set; }
        }

        private class ScoreArr
        {
            public string majorType { get; set; }

            public string courseTime { get; set; }

            public string courseName { get; set; }

            public string credit { get; set; }

            public string sore { get; set; }

            public string gradePoint { get; set; }

            public string getPoint { get; set; }

            public string minorType { get; set; }
        }
        //Define Group
        private class Group
        {
            public string Name { get; set; }

            public ObservableCollection<ScoreArr> Items
            {
                get;
                private set;
            }

            public Group()
            {
                this.Items = new ObservableCollection<ScoreArr>();
            }
        }

        private async void refreshScore_Click(object sender, RoutedEventArgs e)
        {
            refreshIndicator.IsActive = true;
            await HttpRequest.GetScore();
            refreshIndicator.IsActive = false;
            IniList();
        }

        private static ObservableCollection<Group> CreateGroups()
        {
            var groups = new ObservableCollection<Group>();

            List<string> countTime = new List<string>();

            var group = new Group();

            foreach (ScoreArr item in markArr)
            {
                if (countTime.Count == 0)
                {
                    countTime.Add(item.courseTime);
                    group.Name = item.courseTime;
                }
                else if (!countTime.Contains(item.courseTime))
                {
                    groups.Add(group);
                    group = new Group();
                    countTime.Add(item.courseTime);
                    group.Name = item.courseTime;
                }
                group.Items.Add(item);
                if (item.Equals(markArr.Last())){
                    groups.Add(group);
                }
            }
            return groups;
        }
    }
}
