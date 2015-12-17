using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
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
using Windows.Web.Http;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Text;
using Windows.UI;
using System.Text.RegularExpressions;

// “空白页”项模板在 http://go.microsoft.com/fwlink/?LinkId=234238 上提供

namespace Fzuhelper.Views
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class Timetable : Page
    {
        //private StorageFolder fzuhelperDataFolder = await ApplicationData.Current.LocalFolder.GetFolderAsync("FzuhelperData");

        //private static bool initialAgain = true;

        private static bool firstTimeLoad = true;

        private ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

        private string jsonData;

        private TimetableReturnValue ttrv;

        private static List<TableInfoArr> tableInfo;

        private string currentWeek;

        private static List<List<SingleDayCourseArr>> singleDayCourseList = new List<List<SingleDayCourseArr>>();

        private static WeekToInt weekToInt = new WeekToInt();

        private static List<WeekDays> weekDays = new List<WeekDays>();

        private static byte courseColorAlpha = 180;

        private static List<SolidColorBrush> courseColor = new List<SolidColorBrush>
        {
            new SolidColorBrush(Color.FromArgb(courseColorAlpha,236,151,148)),
            new SolidColorBrush(Color.FromArgb(courseColorAlpha,230,136,170)),
            new SolidColorBrush(Color.FromArgb(courseColorAlpha,152,158,208)),
            new SolidColorBrush(Color.FromArgb(courseColorAlpha,122,175,168)),
            new SolidColorBrush(Color.FromArgb(courseColorAlpha,139,191,239)),
            new SolidColorBrush(Color.FromArgb(courseColorAlpha,156,201,158)),
            new SolidColorBrush(Color.FromArgb(courseColorAlpha,249,192,123)),
            new SolidColorBrush(Color.FromArgb(courseColorAlpha,249,212,123)),
            new SolidColorBrush(Color.FromArgb(courseColorAlpha,192,139,207)),
            new SolidColorBrush(Color.FromArgb(courseColorAlpha,142,142,56))
        };

        private static int courseColorCount;

        private static int courseColorAmount = 10;

        private static Dictionary<string, SolidColorBrush> courseColorDict = new Dictionary<string, SolidColorBrush>();

        private static BitmapImage img = new BitmapImage();

        public Timetable()
        {
            this.InitializeComponent();

            SetTimetableBackground();

            //IniList(false);

            InitTimetable();
        }

        private async void InitTimetable()
        {
            await MockGet();

            await FormatTimetable();
        }

        private async Task MockGet()
        {
            try
            {
                await MockJwch.MockGetTimetable();
            }
            catch
            {
                MainPage.SendToast("获取课表失败");
            }
        }

        private async Task FormatTimetable()
        {
            try
            {
                //Get data from storage
                StorageFolder fzuhelperDataFolder = await ApplicationData.Current.LocalFolder.GetFolderAsync("FzuhelperData");
                StorageFile timetable = await fzuhelperDataFolder.GetFileAsync("timetable.dat");
                string htmlStr = await FileIO.ReadTextAsync(timetable);

                //Simplify htmlStr
                string simplifyRegex = @"星期日.*备注";
                Match simplifyMatch = Regex.Match(htmlStr, simplifyRegex);
                string simplifiedStr = simplifyMatch.Value;

                //Start regex match
                string regexStr = @"<td((?!上午)(?!下午)(?!晚上)(?!\d{1,2}\:\d\d).)*?</td>";
                MatchCollection jies = Regex.Matches(simplifiedStr, regexStr);
                
                System.Diagnostics.Debug.WriteLine(jies.Count);
            }
            catch
            {

            }
        }

        private void SetTimetableBackground()
        {
            string timetableUri = localSettings.Values["timetable_background"].ToString();
            img.UriSource = new Uri(timetableUri);
            timetableBackgroundImg.ImageSource = img;
        }

        private async void IniList(bool IsRefresh)
        {
            refreshIndicator.IsActive = true;
            if (!IsRefresh)
            {
                try
                {
                    //Get data from storage
                    StorageFolder fzuhelperDataFolder = await ApplicationData.Current.LocalFolder.GetFolderAsync("FzuhelperData");
                    StorageFile timetable = await fzuhelperDataFolder.GetFileAsync("timetable.dat");
                    jsonData = await FileIO.ReadTextAsync(timetable);
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
                    jsonData = await HttpRequest.GetTimetable();
                    if(jsonData == "error")
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
                ttrv = JsonConvert.DeserializeObject<TimetableReturnValue>(jsonData);
                //A list of every week courses ,and the first item is current week courses
                tableInfo = JsonConvert.DeserializeObject<List<TableInfoArr>>(ttrv.data["tableInfo"].ToString());
                currentWeek = tableInfo[0].week;
                ShowOneWeekCourse(0);
                IniWeekOption();
            }
            catch
            {
                MainPage.SendToast("获取数据出错，请刷新");
            }
            refreshIndicator.IsActive = false;
        }

        private void ShowOneWeekCourse(int week)
        {
            //System.Diagnostics.Debug.WriteLine(timeTableMain.Children.Count);
            //Clear timetable
            while (timeTableMain.Children.Count != 18)
            {
                timeTableMain.Children.Remove(timeTableMain.Children.Last());
            }
            //Clear single day course arr list
            singleDayCourseList.Clear();

            List<DayCourseArr> dayCourseList = new List<DayCourseArr>();

            DayCourseArr dayCourse = new DayCourseArr();

            for (int i = 1; i <= 7; i++)
            {
                for(int jie = 1; jie <= 11; jie++)
                {
                    dayCourse = JsonConvert.DeserializeObject<DayCourseArr>(tableInfo[week].courseArr[i.ToString()][jie.ToString()].ToString());
                    dayCourseList.Add(dayCourse);
                }
            }

            //Preparation for course color
            courseColorCount = 0;
            courseColorDict.Clear();

            for(int i = 1; i <= 7; i++)
            {
                //Initialize single day course arr list
                singleDayCourseList.Add(new List<SingleDayCourseArr>());
                //System.Diagnostics.Debug.WriteLine(tableInfo[week].courseArr[i.ToString()]["1"]);
                //i represents the column index that course should be placed and jie represents the row index
                for (int jie = 1; jie <= 11; jie++)
                {
                    DayCourseArr currentCourse = dayCourseList[(i - 1) * 11 + jie - 1];
                    //If the course is empty
                    if (currentCourse.courseName == "")
                    {
                        continue;
                    }
                    //Check rowspan
                    int rowspans = 0;
                    while (true)
                    {
                        //System.Diagnostics.Debug.WriteLine(rowspans);
                        if(currentCourse.courseName== dayCourseList[(i - 1) * 11 + jie + rowspans].courseName&& currentCourse.place == dayCourseList[(i - 1) * 11 + jie + rowspans].place&& currentCourse.teacherName == dayCourseList[(i - 1) * 11 + jie + rowspans].teacherName&& currentCourse.betweenWeek == dayCourseList[(i - 1) * 11 + jie + rowspans].betweenWeek)
                        {
                            if(( ((i - 1) * 11) < ((i - 1) * 11 + jie + rowspans) && ((i - 1) * 11 + jie + rowspans) <= ((i - 1) * 11 + 10)))
                            {
                                rowspans++;
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                    //jieDuration = jie ~ jie+rowspans
                    string jieDur = jie.ToString() + "-" + (jie + rowspans).ToString();
                    singleDayCourseList[i - 1].Add(new SingleDayCourseArr(currentCourse.courseName, currentCourse.place, currentCourse.teacherName, currentCourse.betweenWeek+"周", jieDur));
                    //Create stackpanel and visible textblocks
                    StackPanel stackPanel = new StackPanel() { Orientation=Orientation.Vertical};
                    TextBlock textBlock1 = new TextBlock() { Text = currentCourse.courseName, MaxLines = 2, Foreground = new SolidColorBrush(Color.FromArgb(255,255,255,255)), FontSize=12 , HorizontalAlignment=HorizontalAlignment.Stretch , TextWrapping=TextWrapping.WrapWholeWords};
                    TextBlock textBlock2 = new TextBlock() {Text = currentCourse.place , Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255)), FontSize = 12, HorizontalAlignment = HorizontalAlignment.Stretch , TextWrapping = TextWrapping.WrapWholeWords};
                    //Invisible textblocks
                    TextBlock textBlock3 = new TextBlock() {Text = currentCourse.teacherName , Visibility = Visibility.Collapsed};
                    TextBlock textBlock4 = new TextBlock() {Text = currentCourse.betweenWeek, Visibility = Visibility.Collapsed };
                    TextBlock textBlock5 = new TextBlock() {Text = jieDur, Visibility = Visibility.Collapsed };

                    //Add elements to stackpanel
                    stackPanel.Children.Add(textBlock1);
                    stackPanel.Children.Add(textBlock2);
                    stackPanel.Children.Add(textBlock3);
                    stackPanel.Children.Add(textBlock4);
                    stackPanel.Children.Add(textBlock5);

                    //Create button
                    Button singleCourse = new Button() {  Margin=new Thickness(2) , Padding=new Thickness(2) , HorizontalAlignment=HorizontalAlignment.Stretch , VerticalAlignment=VerticalAlignment.Stretch };
                    singleCourse.Content = stackPanel;
                    singleCourse.Flyout = AddCourseFlyout(stackPanel);
                    singleCourse.Click += SingleCourse_Click;
                    //Set course background color
                    if (courseColorDict.ContainsKey(currentCourse.courseName))
                    {
                        singleCourse.Background = courseColorDict[currentCourse.courseName];
                    }
                    else
                    {
                        singleCourse.Background = courseColor[courseColorCount];
                        courseColorDict.Add(currentCourse.courseName, courseColor[courseColorCount]);
                        courseColorCount++;
                        if(courseColorCount == courseColorAmount)
                        {
                            courseColorCount = 0;
                        }
                    }

                    Grid.SetColumn(singleCourse, i);
                    Grid.SetRow(singleCourse, jie);
                    if (rowspans != 0)
                    {
                        Grid.SetRowSpan(singleCourse, rowspans + 1);
                    }
                    timeTableMain.Children.Add(singleCourse);
                    jie += rowspans;
                }
            }
            //Set selected week
            selectedWeek.Text = tableInfo[week].week;
            if (tableInfo[week].week.Equals(tableInfo[0].week))
            {
                selectedWeekBtn.Label = "(本周)";
            }
            else
            {
                selectedWeekBtn.Label = "(非本周)";
            }
            //Set single day course data binding
            MonCourse.ItemsSource = singleDayCourseList[0];
            TueCourse.ItemsSource = singleDayCourseList[1];
            WedCourse.ItemsSource = singleDayCourseList[2];
            ThuCourse.ItemsSource = singleDayCourseList[3];
            FriCourse.ItemsSource = singleDayCourseList[4];
            SatCourse.ItemsSource = singleDayCourseList[5];
            SunCourse.ItemsSource = singleDayCourseList[6];
            //Display current day
            int currentDayIndex = weekToInt.w2i[DateTime.Now.DayOfWeek.ToString()];
            singleDayCourseView.SelectedItem = singleDayCourseView.Items.ElementAt(currentDayIndex);
            //Check empty
            foreach(List<SingleDayCourseArr> item in singleDayCourseList)
            {
                if (item.Count == 0)
                {
                    TextBlock tb = new TextBlock() { Text = "没课啦", FontSize = 24, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                    ListViewItem lvi = new ListViewItem();
                    lvi.Content = tb;
                    switch (singleDayCourseList.IndexOf(item))
                    {
                        case 0:
                            MonCourse.ItemsSource = null;
                            MonCourse.Items.Clear();
                            MonCourse.Items.Add(lvi);
                            break;
                        case 1:
                            TueCourse.ItemsSource = null;
                            TueCourse.Items.Clear();
                            TueCourse.Items.Add(lvi);
                            break;
                        case 2:
                            WedCourse.ItemsSource = null;
                            WedCourse.Items.Clear();
                            WedCourse.Items.Add(lvi);
                            break;
                        case 3:
                            ThuCourse.ItemsSource = null;
                            ThuCourse.Items.Clear();
                            ThuCourse.Items.Add(lvi);
                            break;
                        case 4:
                            FriCourse.ItemsSource = null;
                            FriCourse.Items.Clear();
                            FriCourse.Items.Add(lvi);
                            break;
                        case 5:
                            SatCourse.ItemsSource = null;
                            SatCourse.Items.Clear();
                            SatCourse.Items.Add(lvi);
                            break;
                        case 6:
                            SunCourse.ItemsSource = null;
                            SunCourse.Items.Clear();
                            SunCourse.Items.Add(lvi);
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        private Flyout AddCourseFlyout(StackPanel sp)
        {
            TextBlock t1 = (TextBlock)sp.Children.ElementAt(0);//courseName
            TextBlock t2 = (TextBlock)sp.Children.ElementAt(1);//place
            TextBlock t3 = (TextBlock)sp.Children.ElementAt(2);//teacherName
            TextBlock t4 = (TextBlock)sp.Children.ElementAt(3);//betweenWeek
            TextBlock t5 = (TextBlock)sp.Children.ElementAt(4);//jie
            //Create main stackpanel
            StackPanel spMain = new StackPanel() { Padding = new Thickness(5), Margin = new Thickness(10), HorizontalAlignment = HorizontalAlignment.Stretch, Orientation = Orientation.Horizontal };
            //Create left and right stackpanel
            StackPanel spLeft = new StackPanel() { Margin = new Thickness(0,0,20,0), HorizontalAlignment = HorizontalAlignment.Stretch, Orientation = Orientation.Vertical };
            StackPanel spRight = new StackPanel() { HorizontalAlignment = HorizontalAlignment.Stretch, Orientation = Orientation.Vertical };
            //Create new textblocks
            //indicators
            TextBlock tbi1 = new TextBlock() { Text = "课程", Margin = new Thickness(0, 5, 0, 5) };
            TextBlock tbi2 = new TextBlock() { Text = "教室", Margin = new Thickness(0, 5, 0, 5) };
            TextBlock tbi3 = new TextBlock() { Text = "老师", Margin = new Thickness(0, 5, 0, 5) };
            TextBlock tbi4 = new TextBlock() { Text = "周数", Margin = new Thickness(0, 5, 0, 5) };
            TextBlock tbi5 = new TextBlock() { Text = "节数", Margin = new Thickness(0, 5, 0, 5) };
            //values
            TextBlock tb1 = new TextBlock() { Text = t1.Text, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 5, 0, 5) };
            TextBlock tb2 = new TextBlock() { Text = t2.Text, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 5, 0, 5) };
            TextBlock tb3 = new TextBlock() { Text = t3.Text, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 5, 0, 5) };
            TextBlock tb4 = new TextBlock() { Text = t4.Text, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 5, 0, 5) };
            TextBlock tb5 = new TextBlock() { Text = t5.Text, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 5, 0, 5) };
            //Add textblocks to stackpanels
            spLeft.Children.Add(tbi1);
            spLeft.Children.Add(tbi2);
            spLeft.Children.Add(tbi3);
            spLeft.Children.Add(tbi4);
            spLeft.Children.Add(tbi5);
            spRight.Children.Add(tb1);
            spRight.Children.Add(tb2);
            spRight.Children.Add(tb3);
            spRight.Children.Add(tb4);
            spRight.Children.Add(tb5);
            spMain.Children.Add(spLeft);
            spMain.Children.Add(spRight);
            Flyout flyout = new Flyout();
            flyout.Content = spMain;
            //FlyoutBase.SetAttachedFlyout(sp, flyout);
            return flyout;
        }

        private void selectedWeekBtn_Click(object sender, RoutedEventArgs e)
        {
            topbar.IsOpen = !topbar.IsOpen;
        }

        private void SingleDayCourseToggle_Click(object sender, RoutedEventArgs e)
        {
            AppBarToggleButton sdct = (AppBarToggleButton)sender;
            if ((bool)sdct.IsChecked)
            {
                weekDays.Clear();
                foreach(string item in days)
                {
                    WeekDays day = new WeekDays() { day = item };
                    weekDays.Add(day);
                }
                //singleDayCourseView.ItemsSource = null;
                //singleDayCourseView.ItemsSource = weekDays;
                timeTableView.Visibility = Visibility.Collapsed;
                singleDayCourseView.Visibility = Visibility.Visible;
            }
            else
            {
                timeTableView.Visibility = Visibility.Visible;
                singleDayCourseView.Visibility = Visibility.Collapsed;
            }
        }

        private void refreshTimetable_Click(object sender, RoutedEventArgs e)
        {
            IniList(true);
        }

        private void SingleCourse_Click(object sender, RoutedEventArgs e)
        {
            Button singleCourseBtn = (Button)sender;
        }

        private void IniWeekOption()
        {
            topbar.SecondaryCommands.Clear();
            foreach (TableInfoArr t in tableInfo)
            {
                AppBarButton weekOption = new AppBarButton();
                weekOption.Label = t.week;
                if (t.week.Equals(tableInfo[0].week))
                {
                    weekOption.Label += "(本周)";
                }
                weekOption.Click += Week_Switch;
                topbar.SecondaryCommands.Add(weekOption);
            }
        }

        private void Week_Switch(object sender, RoutedEventArgs e)
        {
            AppBarButton selectedWeek = (AppBarButton)sender;
            ShowOneWeekCourse(topbar.SecondaryCommands.IndexOf(selectedWeek));
        }

        private class TimetableReturnValue
        {
            public bool status { get; set; }

            public string errMsg { get; set; }

            public PropertySet data { get; set; }
        }


        private class TableInfoArr
        {
            public string week { get; set; }

            public Dictionary<string,PropertySet> courseArr { get; set; }
        }

        public class DayCourseArr
        {
            public string courseName { get; set; }

            public string place { get; set; }

            public string teacherName { get; set; }

            public string betweenWeek { get; set; }

            //public string jie { get; set; }
        }

        private class SingleDayCourseArr : DayCourseArr
        {
            public string jieDuration { get; set; }

            public SingleDayCourseArr() { }

            public SingleDayCourseArr(string courseName,string place,string teacherName,string betweenWeek,string jieDuration)
            {
                this.courseName = courseName;
                this.place = place;
                this.teacherName = teacherName;
                this.betweenWeek = betweenWeek;
                this.jieDuration = jieDuration;
            }
        }

        private class WeekToInt
        {
            public Dictionary<string, int> w2i = new Dictionary<string, int>();

            public WeekToInt()
            {
                w2i.Add("Monday", 0);
                w2i.Add("Tuesday", 1);
                w2i.Add("Wednesday", 2);
                w2i.Add("Thursday", 3);
                w2i.Add("Friday", 4);
                w2i.Add("Saturday", 5);
                w2i.Add("Sunday", 6);
            }
        }

        private static List<string> days = new List<string> { "周一", "周二", "周三", "周四", "周五", "周六", "周日" };

        private class WeekDays
        {
            public string day { get; set; }
        }

        private void singleDayCourseView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach(PivotItem item in singleDayCourseView.Items)
            {
                if(item == singleDayCourseView.Items[singleDayCourseView.SelectedIndex])
                {
                    //selected
                    ((TextBlock)item.Header).FontWeight = FontWeights.ExtraBold;
                    ((TextBlock)item.Header).Opacity = 1;
                }
                else
                {
                    //unselected
                    ((TextBlock)item.Header).FontWeight = FontWeights.Normal;
                    ((TextBlock)item.Header).Opacity = 0.5;
                }
            }
        }
    }
}
