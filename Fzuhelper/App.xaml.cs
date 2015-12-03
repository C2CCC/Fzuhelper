using Fzuhelper.Views;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Background;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace Fzuhelper
{
    /// <summary>
    /// 提供特定于应用程序的行为，以补充默认的应用程序类。
    /// </summary>
    sealed partial class App : Application
    {
        private StorageFolder localFolder = ApplicationData.Current.LocalFolder;
        private ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
        /// <summary>
        /// 初始化单一实例应用程序对象。这是执行的创作代码的第一行，
        /// 已执行，逻辑上等同于 main() 或 WinMain()。
        /// </summary>
        public App()
        {
            Microsoft.ApplicationInsights.WindowsAppInitializer.InitializeAsync(
                Microsoft.ApplicationInsights.WindowsCollectors.Metadata |
                Microsoft.ApplicationInsights.WindowsCollectors.Session);
            this.InitializeComponent();
            this.Suspending += OnSuspending;
        }

        /// <summary>
        /// 在应用程序由最终用户正常启动时进行调用。
        /// 将在启动应用程序以打开特定文件等情况下使用。
        /// </summary>
        /// <param name="e">有关启动请求和过程的详细信息。</param>
        protected override async void OnLaunched(LaunchActivatedEventArgs e)
        {

#if DEBUG
            if (System.Diagnostics.Debugger.IsAttached)
            {
                this.DebugSettings.EnableFrameRateCounter = true;
            }
#endif

            Frame rootFrame = Window.Current.Content as Frame;

            // 不要在窗口已包含内容时重复应用程序初始化，
            // 只需确保窗口处于活动状态
            if (rootFrame == null)
            {
                // 创建要充当导航上下文的框架，并导航到第一页
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: 从之前挂起的应用程序加载状态
                }

                // 将框架放在当前窗口中
                Window.Current.Content = rootFrame;
            }

            if (rootFrame.Content == null)
            {
                // 当导航堆栈尚未还原时，导航到第一页，
                // 并通过将所需信息作为导航参数传入来配置
                // 参数
                object IsLogedIn = localSettings.Values["IsLogedIn"];
                try
                {
                    if ((bool)IsLogedIn)
                    {
                        rootFrame.Navigate(typeof(AppShell), e.Arguments);
                    }
                    else
                    {
                        StorageFolder fzuhelperDataFolder = await localFolder.CreateFolderAsync("FzuhelperData", CreationCollisionOption.ReplaceExisting);
                        rootFrame.Navigate(typeof(MainPage), e.Arguments);
                    }
                }
                catch
                {
                    try
                    {
                        StorageFolder fzuhelperDataFolder = await localFolder.CreateFolderAsync("FzuhelperData", CreationCollisionOption.ReplaceExisting);
                    }
                    catch
                    {

                    }
                    rootFrame.Navigate(typeof(MainPage), e.Arguments);
                    
                }
            }
            // 确保当前窗口处于活动状态
            Window.Current.Activate();

            //获取学期，当前周
            string term = "",week = "";
            term = await HttpRequest.TryGetTerm();
            localSettings.Values["term"] = term;
            week = await HttpRequest.TryGetWeek();
            localSettings.Values["week"] = week;

            // set up brushes
            SolidColorBrush bkgColor = Current.Resources["TitleBarBackgroundThemeBrush"] as SolidColorBrush;
            SolidColorBrush fgColor = Current.Resources["TitleBarForegroundThemeBrush"] as SolidColorBrush;
            //手机statusbar颜色
            try
            {
                var statusBar = Windows.UI.ViewManagement.StatusBar.GetForCurrentView();
                statusBar.ForegroundColor = fgColor.Color;
            }
            catch
            {

            }

            //桌面titlebar颜色
            try
            {
                var titleBar = Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().TitleBar;
                titleBar.BackgroundColor = bkgColor.Color;
                titleBar.ForegroundColor = fgColor.Color;
                titleBar.ButtonHoverBackgroundColor = Colors.LightBlue;
                titleBar.ButtonBackgroundColor = bkgColor.Color;
                titleBar.ButtonForegroundColor = fgColor.Color;
            }
            catch
            {

            }


            //后台任务注册
            var taskRegistered = false;
            var exampleTaskName = "BackgroundRefresh";

            foreach (var task in BackgroundTaskRegistration.AllTasks)
            {
                if (task.Value.Name == exampleTaskName)
                {
                    taskRegistered = true;
                    break;
                }
            }


            if (!taskRegistered)
            {
                try
                {
                    var builder = new BackgroundTaskBuilder();
                    builder.Name = exampleTaskName;
                    builder.TaskEntryPoint = "Fzuhelper.BackgroundRefresh";
                    builder.SetTrigger(new TimeTrigger(1440, false));
                    builder.AddCondition(new SystemCondition(SystemConditionType.InternetAvailable));
                    BackgroundTaskRegistration task = builder.Register();
                }
                catch
                {

                }
            }


        }

        /// <summary>
        /// 导航到特定页失败时调用
        /// </summary>
        ///<param name="sender">导航失败的框架</param>
        ///<param name="e">有关导航失败的详细信息</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// 在将要挂起应用程序执行时调用。  在不知道应用程序
        /// 无需知道应用程序会被终止还是会恢复，
        /// 并让内存内容保持不变。
        /// </summary>
        /// <param name="sender">挂起的请求的源。</param>
        /// <param name="e">有关挂起请求的详细信息。</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: 保存应用程序状态并停止任何后台活动
            deferral.Complete();
        }
    }
}
