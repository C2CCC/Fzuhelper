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
using Fzuhelper.Controls;

namespace Fzuhelper.Views
{
    public class MockJwch
    {
        //private static StorageFolder fzuhelperDataFolder = await ApplicationData.Current.LocalFolder.GetFolderAsync("FzuhelperData");

        private static ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

        private static string mockLoginUri = "http://59.77.226.32/logincheck.asp",
            mockGetTimetableUri = "http://59.77.226.35/student/xkjg/wdkb/kb_xs.aspx",
            mockGetScoreUri = "http://59.77.226.35/student/xyzk/cjyl/score_sheet.aspx",
            mockGetExamRoomUri = "http://59.77.226.35/student/xkjg/examination/exam_list.aspx",
            mockGetCurrentUserUri = "http://59.77.226.35/jcxx/xsxx/StudentInformation.aspx",
            mockJwchCalendarUri = "http://59.77.226.32/tt.asp";

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
                    NotifyPopup notifyPopup = new NotifyPopup("账号或密码错误");
                    notifyPopup.Show();
                    request.Dispose();
                    return false;
                }

                //response.Headers.GetValues("Set-Cookie");
                Uri redirectUri = response.Headers.Location;
                request.DefaultRequestHeaders.Remove("Origin");
                response = await request.GetAsync(redirectUri);
                string responseStr2 = await response.Content.ReadAsStringAsync();

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

                if (responseStr2.Contains("alert"))
                {
                    NotifyPopup notifyPopup = new NotifyPopup("账号或密码错误");
                    notifyPopup.Show();
                    request.Dispose();
                    return false;
                }

                //id
                string queryId = response.Headers.Location.Query.Substring(4);

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
                string uri = mockGetCurrentUserUri + "?id=" + localSettings.Values["QueryId"].ToString();
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

        public static async Task<bool> MockGetCurrentCalendar()
        {
            HttpClient request = CreateHttpClient();
            try
            {
                HttpResponseMessage response = new HttpResponseMessage();
                string week = "", term = "";
                //Get response
                response = await request.GetAsync(mockJwchCalendarUri);
                string responseStr = await response.Content.ReadAsStringAsync();

                string weekRegexStr = @"<font.*?(?=</font>)";
                string termRegexStr = @"\d{4}.{3}\d{2}.{3}";
                try
                {
                    Match weekMt = Regex.Match(responseStr, weekRegexStr);
                    week = "第" + weekMt.Value.Substring(22) + "周";
                }
                catch
                {

                }
                try
                {
                    Match termMt = Regex.Match(responseStr, termRegexStr);
                    term = termMt.Value.Substring(0, 4) + "学年" + termMt.Value.Substring(7, 2) + "学期";
                }
                catch
                {

                }

                //Set week
                try
                {
                    localSettings.Values["week"] = week;
                }
                catch
                {

                }
                //Set term
                try
                {
                    localSettings.Values["term"] = term;
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
    }
}
