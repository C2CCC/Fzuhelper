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
            getBookSearchResult = "http://219.229.132.35/api/api.php/FzuHelper/bookSearch.html",
            getJwchNotice = "http://219.229.132.35/api/api.php/FzuHelper/jwcInfo.html",
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
                case "getBookSearchResult":
                    uri = getBookSearchResult;
                    break;
                case "getJwchNotice":
                    uri = getJwchNotice;
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
                //Check if the return data is correct
                try
                {
                    CheckJwch c = JsonConvert.DeserializeObject<CheckJwch>(httpResponseBody);
                    if(c.errMsg== "未登陆")
                    {
                        await ReLogin();
                        return "relogin";
                    }
                    return httpResponseBody;
                }
                catch
                {
                    return "error";
                }
            }
            catch
            {
                MainPage.SendToast("网络错误");
                return "error";
            }
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

        #region get from jwch,get region

        //get exam room
        public static async Task<string> GetExamRoom()
        {
            string jsonData = "";
            //Get token
            try
            {
                //from storage
                StorageFile usrInfo = await localFolder.GetFileAsync("usrInfo.txt");
                string token = (await FileIO.ReadTextAsync(usrInfo)).Split('\n')[1];
                //Get data
                HttpFormUrlEncodedContent content = new HttpFormUrlEncodedContent(new[] { new KeyValuePair<string, string>("token", token) });
                jsonData = await HttpRequest.GetFromJwch("get", "getExamRoom", content);
                if (jsonData == "relogin")
                {
                    jsonData = await HttpRequest.GetFromJwch("get", "getExamRoom", content);
                }
                //System.Diagnostics.Debug.WriteLine(examArr.ElementAt<Dictionary<string,string>>(0)["courseName"]);
                try
                {
                    //Save as file
                    StorageFile examRoom = await localFolder.CreateFileAsync("examRoom.txt", CreationCollisionOption.ReplaceExisting);
                    await FileIO.WriteTextAsync(examRoom, jsonData);
                }
                catch
                {

                }
                //getAgain = true;
                return jsonData;
            }
            catch
            {
                /*if (getAgain)
                {
                    //getAgain = !getAgain;
                    await ReLogin();
                    await GetExamRoom();
                }
                else
                {
                    MainPage.SendToast("网络错误");
                }*/
                return "";
            }
        }

        //get score
        public static async Task<string> GetScore()
        {
            string jsonData = "";
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
                //getAgain = true;
                return jsonData;
            }
            catch
            {
                /*if (getAgain)
                {
                    getAgain = !getAgain;
                    await HttpRequest.ReLogin();
                    await GetScore();
                }
                else
                {
                    MainPage.SendToast("网络错误");
                }*/
                return "";
            }
        }

        //get timetable
        public static async Task<string> GetTimetable()
        {
            string jsonData = "";
            //Get token
            try
            {
                //from storage
                StorageFile accInfo = await localFolder.GetFileAsync("accInfo.txt");
                string stunum = (await FileIO.ReadTextAsync(accInfo)).Split('\n')[0];
                //Get data
                HttpFormUrlEncodedContent content = new HttpFormUrlEncodedContent(new[] { new KeyValuePair<string, string>("stunum", stunum) });
                jsonData = await HttpRequest.GetFromJwch("get", "getTimetable", content);
                //System.Diagnostics.Debug.WriteLine(examArr.ElementAt<Dictionary<string,string>>(0)["courseName"]);
                try
                {
                    //Save as file
                    StorageFile timetable = await localFolder.CreateFileAsync("timetable.txt", CreationCollisionOption.ReplaceExisting);
                    await FileIO.WriteTextAsync(timetable, jsonData);
                }
                catch
                {

                }
                //getAgain = true;
                return jsonData;
            }
            catch
            {
                /*if (getAgain)
                {
                    getAgain = !getAgain;
                    await HttpRequest.ReLogin();
                    await GetTimetable();
                }
                else
                {
                    MainPage.SendToast("网络错误");
                }*/
                return "";
            }
        }

        //get book search result
        public static async Task<string> GetBookSearchResult(string key, string page)
        {
            string jsonData = "";
            try
            {
                //Get data
                HttpFormUrlEncodedContent content = new HttpFormUrlEncodedContent(new[] { new KeyValuePair<string, string>("key", key), new KeyValuePair<string, string>("page", page) });
                jsonData = await HttpRequest.GetFromJwch("get", "getBookSearchResult", content);
                return jsonData;
            }
            catch
            {
                return "";
            }
        }

        //get jwch notice
        public static async Task<string> GetJwchNotice(string page)
        {
            string jsonData = "";
            try
            {
                //Get data
                HttpFormUrlEncodedContent content = new HttpFormUrlEncodedContent(new[] { new KeyValuePair<string, string>("page", page) });
                jsonData = await HttpRequest.GetFromJwch("get", "getJwchNotice", content);
                return jsonData;
            }
            catch
            {
                return "";
            }
        }

        #endregion




        private class CheckJwch
        {
            public bool status { get; set; }

            public string errMsg { get; set; }
        }

        private class LogInReturnValue : CheckJwch
        {
            public Dictionary<string, string> data { get; set; }
        }

    }
}
