using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Newtonsoft.Json;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;
using System.Net.Http;
using HtmlAgilityPack;

namespace Fzuhelper.Views
{
    public class MockJwch
    {
        //private static StorageFolder fzuhelperDataFolder = await ApplicationData.Current.LocalFolder.GetFolderAsync("FzuhelperData");

        private static ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

        private static string mockLoginUri = "http://59.77.226.32/logincheck.asp",
            mockGetTimetableUri = "http://59.77.226.35/student/xkjg/wdkb/kb_xs.aspx",
            mockGetScoreUri = "http://59.77.226.35/student/xyzk/cjyl/score_sheet.aspx",
            mockGetExamRoomUri = "http://59.77.226.35/student/xkjg/examination/exam_list.aspx";

        private static async Task SaveFile(string fileName,string fileData)
        {
            StorageFolder fzuhelperDataFolder = await ApplicationData.Current.LocalFolder.GetFolderAsync("FzuhelperData");
            StorageFile file = await fzuhelperDataFolder.CreateFileAsync(fileName + ".dat", CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteTextAsync(file, fileData);
        }

        private static HttpClient CreateHttpClient()
        {
            HttpClientHandler requestHandler = new HttpClientHandler();
            requestHandler.AllowAutoRedirect = false;
            HttpClient request = new HttpClient(requestHandler);
            request.DefaultRequestHeaders.Host = "59.77.226.32";
            request.DefaultRequestHeaders.Connection.Add("keep-alive");
            request.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
            request.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");
            request.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/47.0.2526.106 Safari/537.36");
            return request;
        }

        public static async Task<bool> MockLogin()
        {
            HttpClient request = CreateHttpClient();
            try
            {
                HttpResponseMessage response = new HttpResponseMessage();
                request.DefaultRequestHeaders.Add("Origin", "http://jwch.fzu.edu.cn");
                request.DefaultRequestHeaders.Add("Referer", "http://jwch.fzu.edu.cn/");
                //request.DefaultRequestHeaders.Add("Content-Type", "application/x-www-form-urlencoded");
                //Prepare post data
                string muser = localSettings.Values["muser"].ToString();
                string passwd = localSettings.Values["passwd"].ToString();
                FormUrlEncodedContent content = new FormUrlEncodedContent(new[] {
                    new KeyValuePair<string, string>("muser", muser),
                    new KeyValuePair<string, string>("passwd", passwd),
                    new KeyValuePair<string, string>("x", "0"),
                    new KeyValuePair<string, string>("y", "0"),
                });

                //Get response
                response = await request.PostAsync(mockLoginUri, content);
                string responseStr1 = await response.Content.ReadAsStringAsync();
                if (responseStr1.Contains("alert"))
                {
                    MainPage.SendToast("账号或密码错误");
                    return false;
                }

                //response.Headers.GetValues("Set-Cookie");
                Uri redirectUri = response.Headers.Location;
                request.DefaultRequestHeaders.Remove("Origin");
                response = await request.GetAsync(redirectUri);
                string responseStr2 = await response.Content.ReadAsStringAsync();
                if (responseStr2.Contains("alert"))
                {
                    MainPage.SendToast("账号或密码错误");
                    return false;
                }

                //id
                string queryId = response.Headers.Location.Query.Substring(4);
                //ASP.NET_SessionId
                try
                {
                    string[] respCookie = (string[])response.Headers.GetValues("Set-Cookie");
                    string aspDotNetSessionCookie = respCookie[0].Split(';')[0];
                    localSettings.Values["AspDotNetSessionCookie"] = aspDotNetSessionCookie;
                }
                catch
                {

                }
                localSettings.Values["QueryId"] = queryId;
                localSettings.Values["IsLogedIn"] = true;

                request.Dispose();

                return true;
            }
            catch
            {
                request.Dispose();
                return false;
            }
        }

        public static async Task<bool> MockGetTimetable()
        {
            HttpClient request = CreateHttpClient();
            try
            {
                HttpResponseMessage response = new HttpResponseMessage();
                request.DefaultRequestHeaders.Add("Referer", "http://59.77.226.35/left.aspx" + "?id=" + localSettings.Values["QueryId"].ToString());
                request.DefaultRequestHeaders.Add("Cookie", localSettings.Values["AspDotNetSessionCookie"].ToString());

                //Get response
                response = await request.GetAsync(mockGetTimetableUri + "?id=" + localSettings.Values["QueryId"].ToString());
                string responseStr = await response.Content.ReadAsStringAsync();

                //If session expired
                if (responseStr.Contains("会话过期，请重新登录"))
                {
                    await MockLogin();
                    request.Dispose();
                    return await MockGetTimetable();
                }

                //Save as file
                await SaveFile("timetable", responseStr);

                request.Dispose();

                return true;
            }
            catch
            {
                request.Dispose();
                return false;
            }
        }

        public static async Task<bool> MockGetScore()
        {
            HttpClient request = CreateHttpClient();
            try
            {
                HttpResponseMessage response = new HttpResponseMessage();
                request.DefaultRequestHeaders.Add("Referer", "http://59.77.226.35/left.aspx" + "?id=" + localSettings.Values["QueryId"].ToString());
                request.DefaultRequestHeaders.Add("Cookie", localSettings.Values["AspDotNetSessionCookie"].ToString());

                //Get response
                response = await request.GetAsync(mockGetScoreUri + "?id=" + localSettings.Values["QueryId"].ToString() + "&bj=score");
                string responseStr = await response.Content.ReadAsStringAsync();

                //If session expired
                if (responseStr.Contains("会话过期，请重新登录"))
                {
                    await MockLogin();
                    request.Dispose();
                    return await MockGetScore();
                }

                //Save as file
                await SaveFile("score", responseStr);

                request.Dispose();

                return true;
            }
            catch
            {
                request.Dispose();
                return false;
            }
        }

        public static async Task<bool> MockGetExamRoom()
        {
            HttpClient request = CreateHttpClient();
            try
            {
                HttpResponseMessage response = new HttpResponseMessage();
                request.DefaultRequestHeaders.Add("Referer", "http://59.77.226.35/left.aspx" + "?id=" + localSettings.Values["QueryId"].ToString());
                request.DefaultRequestHeaders.Add("Cookie", localSettings.Values["AspDotNetSessionCookie"].ToString());

                //Get response
                response = await request.GetAsync(mockGetExamRoomUri + "?id=" + localSettings.Values["QueryId"].ToString());
                string responseStr = await response.Content.ReadAsStringAsync();

                //If session expired
                if (responseStr.Contains("会话过期，请重新登录"))
                {
                    await MockLogin();
                    request.Dispose();
                    return await MockGetExamRoom();
                }

                //Save as file
                await SaveFile("examroom", responseStr);

                request.Dispose();

                return true;
            }
            catch
            {
                request.Dispose();
                return false;
            }
        }

        public static async Task<bool> MockGetCurrentUser()
        {
            HttpClient request = CreateHttpClient();
            try
            {
                HttpResponseMessage response = new HttpResponseMessage();
                request.DefaultRequestHeaders.Add("Referer", "http://59.77.226.35/left.aspx" + "?id=" + localSettings.Values["QueryId"].ToString());
                request.DefaultRequestHeaders.Add("Cookie", localSettings.Values["AspDotNetSessionCookie"].ToString());

                //Get response
                string uri = "http://59.77.226.35/jcxx/xsxx/StudentInformation.aspx" + "?id=" + localSettings.Values["QueryId"].ToString();
                response = await request.GetAsync(uri);
                string responseStr = await response.Content.ReadAsStringAsync();
                
                try
                {
                    HtmlDocument doc = new HtmlDocument();
                    doc.LoadHtml(responseStr);
                    HtmlNode usernameNode = doc.GetElementbyId("ContentPlaceHolder1_LB_xm");
                    localSettings.Values["username"] = usernameNode.InnerText;
                }
                catch
                {

                }

                request.Dispose();

                return true;
            }
            catch
            {
                request.Dispose();
                return false;
            }
        }

        /*
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
                jsonData = await HttpRequest.GetFromJwch("get", "getExamRoom", content, true);
                if (jsonData == "relogin")
                {
                    jsonData = await HttpRequest.GetFromJwch("get", "getExamRoom", content, true);
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
                jsonData = await HttpRequest.GetFromJwch("get", "getScore", content, true);
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
        public static async Task<string> GetGradePoint(string year, string term)
        {
            string jsonData = "";
            //Get token
            try
            {
                //from storage
                StorageFolder fzuhelperDataFolder = await ApplicationData.Current.LocalFolder.GetFolderAsync("FzuhelperData");
                StorageFile accInfo = await fzuhelperDataFolder.GetFileAsync("accInfo.dat");
                string sn = (await FileIO.ReadTextAsync(accInfo)).Split('\n')[0];
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
                    StorageFile gradepoint = await fzuhelperDataFolder.CreateFileAsync(year + term + ".dat", CreationCollisionOption.ReplaceExisting);
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
                if (jsonData == "error")
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

        public static async Task<string> GetEmptyRoom(string time, string place, string star, string end, string term)
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
            //Get data
            HttpFormUrlEncodedContent content = new HttpFormUrlEncodedContent(new[] { new KeyValuePair<string, string>("", "") });
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
        }



        private class CheckJwch
        {
            public bool status { get; set; }

            public string errMsg { get; set; }
        }

        private class LogInReturnValue : CheckJwch
        {
            public Dictionary<string, string> data { get; set; }
        }*/

    }
}
