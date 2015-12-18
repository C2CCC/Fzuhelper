using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Web.Http;
using Newtonsoft.Json;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;

namespace Fzuhelper.Views
{
    public class HttpRequest
    {
        //private static StorageFolder fzuhelperDataFolder = await ApplicationData.Current.LocalFolder.GetFolderAsync("FzuhelperData");

        private static ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

        private static string getTimetable = "http://219.229.132.35/api/api.php/Jwch/timeTable.html",
            getScore = "http://219.229.132.35/api/api.php/Jwch/getMark.html",
            getExamRoom = "http://219.229.132.35/api/api.php/Jwch/examRooms.html",
            getBookSearchResult = "http://219.229.132.35/api/api.php/FzuHelper/bookSearch.html",
            getJwchNotice = "http://219.229.132.35/api/api.php/FzuHelper/jwcInfo.html",
            getGradePoint = "http://120.24.251.94/api/point.php",
            getEmptyRoom = "http://59.77.231.43/empty_room.php",
            getCurrentWeek = "http://219.229.132.35/api/api.php/Jwch/nowWeekCount.html",
            Login = "http://219.229.132.35/api/api.php/Jwch/login.html";

        private static string gradePointToken = "55cafd5f6dd29baa6db9f9419d731964";

        private static readonly HttpClient httpClient = new HttpClient();

        public static async Task<string> GetFromJwch(string method,string purpose,HttpFormUrlEncodedContent content,bool tokenRequire = false)
        {
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
                case "getGradePoint":
                    uri = getGradePoint;
                    break;
                case "getEmptyRoom":
                    uri = getEmptyRoom;
                    break;
                case "getCurrentWeek":
                    uri = getCurrentWeek;
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
                    if (tokenRequire)
                    {
                        CheckJwch c = JsonConvert.DeserializeObject<CheckJwch>(httpResponseBody);
                        if (c.errMsg != "" && c.errMsg != null)
                        {
                            await ReLogin();
                            return "relogin";
                        }
                    }
                    return httpResponseBody;
                }
                catch
                {
                    return httpResponseBody;
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
                StorageFolder fzuhelperDataFolder = await ApplicationData.Current.LocalFolder.GetFolderAsync("FzuhelperData");
                StorageFile accInfo = await fzuhelperDataFolder.GetFileAsync("accInfo.dat");
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
                    if (response == "error")
                    {
                        return;
                    }
                }
                catch
                {
                    MainPage.SendToast("网络错误");
                    return;
                }
                LogInReturnValue l = JsonConvert.DeserializeObject<LogInReturnValue>(response);
                StorageFile usrInfo = await fzuhelperDataFolder.CreateFileAsync("usrInfo.dat", CreationCollisionOption.ReplaceExisting);
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
                StorageFolder fzuhelperDataFolder = await ApplicationData.Current.LocalFolder.GetFolderAsync("FzuhelperData");
                StorageFile usrInfo = await fzuhelperDataFolder.GetFileAsync("usrInfo.dat");
                string token = (await FileIO.ReadTextAsync(usrInfo)).Split('\n')[1];
                //Get data
                HttpFormUrlEncodedContent content = new HttpFormUrlEncodedContent(new[] { new KeyValuePair<string, string>("token", token) });
                jsonData = await HttpRequest.GetFromJwch("get", "getExamRoom", content,true);
                if (jsonData == "relogin")
                {
                    jsonData = await HttpRequest.GetFromJwch("get", "getExamRoom", content,true);
                }
                if (jsonData == "error")
                {
                    return jsonData;
                }
                try
                {
                    //Save as file
                    StorageFile examRoom = await fzuhelperDataFolder.CreateFileAsync("examRoom.dat", CreationCollisionOption.ReplaceExisting);
                    await FileIO.WriteTextAsync(examRoom, jsonData);
                }
                catch
                {

                }
                return jsonData;
            }
            catch
            {
                return "error";
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
                StorageFolder fzuhelperDataFolder = await ApplicationData.Current.LocalFolder.GetFolderAsync("FzuhelperData");
                StorageFile usrInfo = await fzuhelperDataFolder.GetFileAsync("usrInfo.dat");
                string token = (await FileIO.ReadTextAsync(usrInfo)).Split('\n')[1];
                //Get data
                HttpFormUrlEncodedContent content = new HttpFormUrlEncodedContent(new[] { new KeyValuePair<string, string>("token", token) });
                jsonData = await HttpRequest.GetFromJwch("get", "getScore", content,true);
                if (jsonData == "relogin")
                {
                    jsonData = await HttpRequest.GetFromJwch("get", "getScore", content, true);
                }
                if (jsonData == "error")
                {
                    return jsonData;
                }
                try
                {
                    //Save as file
                    StorageFile score = await fzuhelperDataFolder.CreateFileAsync("score.dat", CreationCollisionOption.ReplaceExisting);
                    await FileIO.WriteTextAsync(score, jsonData);
                }
                catch
                {

                }
                return jsonData;
            }
            catch
            {
                return "error";
            }
        }

        //get grade point(one term)
        public static async Task<string> GetGradePoint(string year,string term)
        {
            string jsonData = "";
            //Get token
            try
            {
                //from localsettings
                StorageFolder fzuhelperDataFolder = await ApplicationData.Current.LocalFolder.GetFolderAsync("FzuhelperData");
                //StorageFile accInfo = await fzuhelperDataFolder.GetFileAsync("accInfo.dat");
                //string sn = (await FileIO.ReadTextAsync(accInfo)).Split('\n')[0];
                string sn = localSettings.Values["muser"].ToString();
                //Get data
                HttpFormUrlEncodedContent content = new HttpFormUrlEncodedContent(new[] { new KeyValuePair<string, string>("token", gradePointToken), new KeyValuePair<string, string>("year", year), new KeyValuePair<string, string>("term", term), new KeyValuePair<string, string>("sn", sn) });
                string reg = "false";
                for (int i = 0; i < 10; i++)
                {
                    await Task.Delay(300);
                    jsonData = await HttpRequest.GetFromJwch("get", "getGradePoint", content);
                    try
                    {
                        Match mt = Regex.Match(jsonData, reg);
                        if (!mt.Success)
                        {
                            break;
                        }
                    }
                    catch
                    {

                    }
                }
                if (jsonData == "error")
                {
                    return jsonData;
                }
                try
                {
                    //Save as file
                    StorageFile gradepoint = await fzuhelperDataFolder.CreateFileAsync(year+term+".dat", CreationCollisionOption.ReplaceExisting);
                    await FileIO.WriteTextAsync(gradepoint, jsonData);
                }
                catch
                {

                }
                return jsonData;
            }
            catch
            {
                return "error";
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
                StorageFolder fzuhelperDataFolder = await ApplicationData.Current.LocalFolder.GetFolderAsync("FzuhelperData");
                StorageFile accInfo = await fzuhelperDataFolder.GetFileAsync("accInfo.dat");
                string stunum = (await FileIO.ReadTextAsync(accInfo)).Split('\n')[0];
                //Get data
                HttpFormUrlEncodedContent content = new HttpFormUrlEncodedContent(new[] { new KeyValuePair<string, string>("stunum", stunum) });
                jsonData = await HttpRequest.GetFromJwch("get", "getTimetable", content);
                if(jsonData == "error")
                {
                    return jsonData;
                }
                try
                {
                    //Save as file
                    StorageFile timetable = await fzuhelperDataFolder.CreateFileAsync("timetable.dat", CreationCollisionOption.ReplaceExisting);
                    await FileIO.WriteTextAsync(timetable, jsonData);
                }
                catch
                {

                }
                return jsonData;
            }
            catch
            {
                return "error";
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

        public static async Task<string> GetEmptyRoom(string time,string place,string star,string end,string term)
        {
            string jsonData = "";
            string regexStr = @"{.*sum.*}";
            try
            {
                //Get data
                HttpFormUrlEncodedContent content = new HttpFormUrlEncodedContent(new[] { new KeyValuePair<string, string>("time", time), new KeyValuePair<string, string>("place", place), new KeyValuePair<string, string>("star", star), new KeyValuePair<string, string>("end", end), new KeyValuePair<string, string>("term", term) });
                jsonData = await HttpRequest.GetFromJwch("get", "getEmptyRoom", content);
                try
                {
                    Match mt = Regex.Match(jsonData, regexStr);
                    jsonData = mt.Value;
                }
                catch
                {

                }
                return jsonData;
            }
            catch
            {
                return "";
            }
        }

        #endregion


        //get term,week
        public static async Task<string> TryGetTerm()
        {
            string term = "";
            try
            {
                term = await GetTermFromJwch();
                return term;
            }
            catch
            {
                try
                {
                    StorageFolder fzuhelperDataFolder = await ApplicationData.Current.LocalFolder.GetFolderAsync("FzuhelperData");
                    StorageFile termInfo = await fzuhelperDataFolder.GetFileAsync("termInfo.dat");
                    term = await FileIO.ReadTextAsync(termInfo);
                }
                catch
                {

                }
                return term;
            }
        }

        private static async Task<string> GetTermFromJwch()
        {
            string strMsg = "";
            string regexStr = @"\d{4}.{3}\d{2}.{3}";
            //string regexStr = @"\d{4}\w{2}\d{2}\w{2}";
            string term = "";
            string url = "http://59.77.226.32/tt.asp";
            //try
            //{
                WebRequest request = WebRequest.Create(url);
                WebResponse response = request.GetResponseAsync().Result;
                StreamReader reader = new StreamReader(response.GetResponseStream());
                strMsg = reader.ReadToEnd();

                Match mt = Regex.Match(strMsg, regexStr);
                term = mt.Value;

                term = term.Substring(0, 4) + "学年" + term.Substring(7, 2) + "学期";

                reader.Dispose();
                response.Dispose();
                try
                {
                    StorageFolder fzuhelperDataFolder = await ApplicationData.Current.LocalFolder.GetFolderAsync("FzuhelperData");
                    StorageFile termInfo = await fzuhelperDataFolder.CreateFileAsync("termInfo.dat", CreationCollisionOption.ReplaceExisting);
                    await FileIO.WriteTextAsync(termInfo, term);
                }
                catch
                {

                }
                return term;
            //}
            /*catch
            {
                return "";
            }*/
        }

        public static async Task<string> TryGetWeek()
        {
            string week = "";
            try
            {
                week = await GetCurrentWeek();
                return week;
            }
            catch
            {
                try
                {
                    StorageFolder fzuhelperDataFolder = await ApplicationData.Current.LocalFolder.GetFolderAsync("FzuhelperData");
                    StorageFile weekInfo = await fzuhelperDataFolder.GetFileAsync("weekInfo.dat");
                    week = await FileIO.ReadTextAsync(weekInfo);
                }
                catch
                {

                }
                return week;
            }
        }

        public static async Task<string> GetCurrentWeek()
        {
            string jsonData = "";
            //try
            //{
                //Get data
                HttpFormUrlEncodedContent content = new HttpFormUrlEncodedContent(new[] { new KeyValuePair<string, string>("","") });
                jsonData = await HttpRequest.GetFromJwch("get", "getCurrentWeek", content);
                LogInReturnValue r = JsonConvert.DeserializeObject<LogInReturnValue>(jsonData);
                jsonData = r.data["week"];
                jsonData = "第" + jsonData + "周";
                try
                {
                    StorageFolder fzuhelperDataFolder = await ApplicationData.Current.LocalFolder.GetFolderAsync("FzuhelperData");
                    StorageFile weekInfo = await fzuhelperDataFolder.CreateFileAsync("weekInfo.dat", CreationCollisionOption.ReplaceExisting);
                    await FileIO.WriteTextAsync(weekInfo, jsonData);
                }
                catch
                {

                }
                return jsonData;
            //}
            /*catch
            {
                return "";
            }*/
        }



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
