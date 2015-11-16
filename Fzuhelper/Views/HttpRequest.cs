using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Web.Http;
using Newtonsoft.Json;

namespace Fzuhelper.Views
{
    public class HttpRequest
    {
        private static StorageFolder localFolder = ApplicationData.Current.LocalFolder;

        private static string getTimetable = "http://219.229.132.35/api/api.php/Jwch/timeTable.html",
            getScore = "http://219.229.132.35/api/api.php/Jwch/getMark.html",
            getExamRoom = "http://219.229.132.35/api/api.php/Jwch/examRooms.html",
            Login = "http://219.229.132.35/api/api.php/Jwch/login.html";

        public static async Task<string> GetFromJwch(string method,string purpose,HttpFormUrlEncodedContent content)
        {
            HttpClient httpClient = new HttpClient();
            string uri;
            switch (purpose)
            {
                case "Login":
                    uri = Login;
                    break;
                case "getTimetable":
                    uri = getTimetable;
                    break;
                case "getScore":
                    uri = getScore;
                    break;
                case "getExamRoom":
                    uri = getExamRoom;
                    break;
                default:
                    uri = "";
                    break;
            }
            HttpResponseMessage httpResponse = new HttpResponseMessage();
            string httpResponseBody = "";
            try
            {
                if (method == "post")
                {
                    httpResponse = await httpClient.PostAsync(new Uri(uri), content);
                }
                else
                {
                    string con = await content.ReadAsStringAsync();
                    uri = uri + "?" + con;
                    httpResponse = await httpClient.GetAsync(new Uri(uri));
                }
                httpResponse.EnsureSuccessStatusCode();
                httpResponseBody = await httpResponse.Content.ReadAsStringAsync();
            }
            catch
            {
                MainPage.SendToast("网络错误");
                return "error";
            }
            return httpResponseBody;
        }

        public static async Task ReLogin()
        {
            try
            {
                //Get accInfo
                StorageFile accInfo = await localFolder.GetFileAsync("accInfo.txt");
                String info = await FileIO.ReadTextAsync(accInfo);
                string[] usr = info.Split('\n');
                //Create HttpContent
                HttpFormUrlEncodedContent content = new HttpFormUrlEncodedContent(
                new[]
                {
                    new KeyValuePair<string,string>("stunum",usr[0]),
                    new KeyValuePair<string,string>("passwd",usr[1]),
                });
                //Login
                string response = "";
                try
                {
                    response = await GetFromJwch("post", "Login", content);
                }
                catch
                {
                    MainPage.SendToast("网络错误");
                    return;
                }
                LogInReturnValue l = JsonConvert.DeserializeObject<LogInReturnValue>(response);
                StorageFile usrInfo = await localFolder.CreateFileAsync("usrInfo.txt", CreationCollisionOption.ReplaceExisting);
                await FileIO.WriteTextAsync(usrInfo, l.data["stuname"] + "\n" + l.data["token"]);
            }
            catch
            {
                MainPage.SendToast("身份过期，请重新登录");
                return;
            }
        }

        private class LogInReturnValue
        {
            public bool status { get; set; }

            public string errMsg { get; set; }

            public Dictionary<string, string> data { get; set; }
        }

    }
}
