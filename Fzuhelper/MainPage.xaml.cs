using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http;
using Windows.Storage;
using Windows.UI.Notifications;
using Windows.Data.Xml.Dom;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Net;
using System.Text.RegularExpressions;

//“空白页”项模板在 http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409 上有介绍

namespace Fzuhelper
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        //private StorageFolder localFolder = ApplicationData.Current.LocalFolder;

        //private StorageFolder fzuhelperDataFolder = await ApplicationData.Current.LocalFolder.GetFolderAsync("FzuhelperData");

        private ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

        public LogInReturnValue l = new LogInReturnValue();

        public MainPage()
        {
            this.InitializeComponent();
        }

        private void loginBtn_Click(object sender, RoutedEventArgs e)
        {
            //Check not null
            if (username.Text == "" || password.Password == "")
            {
                //Notify
                SendToast("账号或密码不能为空！");
                return;
            }
            //Get usr and pwd
            string stunum = username.Text,
                passwd = password.Password;
            //base64 for pwd
            byte[] bytes = Encoding.UTF8.GetBytes(passwd);
            passwd = Convert.ToBase64String(bytes);
            LogInVerification(stunum,passwd);
        }

        private void toggleLoginState()
        {
            logingIn.IsActive = !logingIn.IsActive;
            username.IsEnabled = !username.IsEnabled;
            password.IsEnabled = !password.IsEnabled;
            loginBtn.IsEnabled = !loginBtn.IsEnabled;
        }

        private async void LogInVerification(string stunum,string passwd)
        {
            toggleLoginState();
            //post to verify
            HttpClient httpClient = new HttpClient();
            Uri requestUri = new Uri("http://219.229.132.35/api/api.php/Jwch/login.html");

            HttpFormUrlEncodedContent content = new HttpFormUrlEncodedContent(
                new[]
                {
                    new KeyValuePair<string,string>("stunum",stunum),
                    new KeyValuePair<string,string>("passwd",passwd),
                });
            

            HttpResponseMessage httpResponse = new HttpResponseMessage();
            string httpResponseBody = "";
            try
            {
                httpResponse = await httpClient.PostAsync(requestUri, content);
                httpResponse.EnsureSuccessStatusCode();
                httpResponseBody = await httpResponse.Content.ReadAsStringAsync();
            }
            catch
            {
                SendToast("网络错误");
                toggleLoginState();
                return ;
            }
            DirectToIndex(httpResponseBody);
            //System.Diagnostics.Debug.WriteLine(httpResponseBody);

            //Create local storage
            /*  
                stunum
                passwd
            
            */
            StorageFolder fzuhelperDataFolder = await ApplicationData.Current.LocalFolder.GetFolderAsync("FzuhelperData");
            StorageFile accInfo = await fzuhelperDataFolder.CreateFileAsync("accInfo.dat", CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteTextAsync(accInfo, stunum+"\n"+passwd);

            //Update login state
            localSettings.Values["IsLogedIn"] = true;
        }

        private async void DirectToIndex(string response)
        {
            try
            {
                //Login
                l = JsonConvert.DeserializeObject<LogInReturnValue>(response);
                //Store token etc.
                try
                {
                    /*
                        stuname
                        token
                    */
                    StorageFolder fzuhelperDataFolder = await ApplicationData.Current.LocalFolder.GetFolderAsync("FzuhelperData");
                    StorageFile usrInfo = await fzuhelperDataFolder.CreateFileAsync("usrInfo.dat", CreationCollisionOption.ReplaceExisting);
                    await FileIO.WriteTextAsync(usrInfo, l.data["stuname"] + "\n" + l.data["token"]);
                }
                catch
                {

                }
                localSettings.Values["stuname"] = l.data["stuname"];
                Frame.Navigate(typeof(AppShell));
            }
            catch
            {
                localSettings.Values["IsLogedIn"] = false;
                SendToast("账号或密码错误");
                toggleLoginState();
            }
        }

            


        public class LogInReturnValue
        {
            public bool status { get; set; }

            public string errMsg { get; set; }

            public Dictionary<string,string> data { get; set; }
        }

        public static void SendToast(string content)
        {
            ToastTemplateType toastTemplate = ToastTemplateType.ToastText01;
            XmlDocument toastXml = ToastNotificationManager.GetTemplateContent(toastTemplate);
            XmlNodeList toastTextElements = toastXml.GetElementsByTagName("text");
            toastTextElements[0].AppendChild(toastXml.CreateTextNode(content));
            ToastNotification toast = new ToastNotification(toastXml);
            ToastNotificationManager.CreateToastNotifier().Show(toast);
        }
    }
}
