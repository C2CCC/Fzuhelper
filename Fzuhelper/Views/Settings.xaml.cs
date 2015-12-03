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

// “空白页”项模板在 http://go.microsoft.com/fwlink/?LinkId=234238 上提供

namespace Fzuhelper.Views
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class Settings : Page
    {
        private ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

        public Settings()
        {
            this.InitializeComponent();

            SetSelectedTimetableBackground();
        }

        private void SetSelectedTimetableBackground()
        {
            string selected = localSettings.Values["timetable_background"].ToString();
            int selectedIndex = 0;
            switch (selected)
            {
                case "ms-appx:///Assets/Timetable_Background_01.jpg":
                    selectedIndex = 0;
                    break;
                case "ms-appx:///Assets/Timetable_Background_02.jpg":
                    selectedIndex = 1;
                    break;
                case "white":
                    selectedIndex = 2;
                    break;
                default:
                    break;
            }
            timetableBackgroundSelector.SelectedIndex = selectedIndex;
        }

        private void TimetableBackground_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int selectedIndex = timetableBackgroundSelector.SelectedIndex;
            switch (selectedIndex)
            {
                case 0:
                    localSettings.Values["timetable_background"] = "ms-appx:///Assets/Timetable_Background_01.jpg";
                    break;
                case 1:
                    localSettings.Values["timetable_background"] = "ms-appx:///Assets/Timetable_Background_02.jpg";
                    break;
                case 2:
                    localSettings.Values["timetable_background"] = "white";
                    break;
                default:
                    break;
            }
        }
    }
}
