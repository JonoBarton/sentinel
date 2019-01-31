using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using SentinelData;
using DateTime = System.DateTime;
using Timer = System.Timers.Timer;
using ToastNotifications;
using ToastNotifications.Lifetime;
using ToastNotifications.Position;
using ToastNotifications.Messages;
namespace SentinelApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string serviceName = "Sentinel";
        SentinelData.SentinelData sentinelData = new SentinelData.SentinelData();
        public List<User> usersList = new List<User>();
        public ObservableCollection<ComboBoxItem> UserItems { get; set; }
        public ComboBoxItem SelectedUser { get; set; }
        public Timer _timer = new Timer();
        ServiceHost host;
        private Notifier notifier;
        public MainWindow()
        {
            InitializeComponent();

            host = new ServiceHost(
                typeof(MessageHandler),
                    new Uri("net.pipe://localhost"));

            host.AddServiceEndpoint(typeof(IMessage),
                new NetNamedPipeBinding(),
                "Message");
            host.Open();

            _timer.Interval = 1000;
            _timer.Enabled = true;
            _timer.Elapsed += _timer_Elapsed;
            DataContext = this;
            UserItems = new ObservableCollection<ComboBoxItem>();
            var cbItem = new ComboBoxItem { Content = "<--Select-->" };
            SelectedUser = cbItem;
            UserItems.Add(cbItem);

            //UserCombo.DisplayMemberPath = "Name";
            //UserCombo.SelectedValuePath = "SID";


            if (!ServiceHelper.ServiceIsRunning(serviceName))
            {
                ServiceHelper.StartService(serviceName);
            }

            Task.Run(async () =>
            {
                usersList = await sentinelData.GetUsers();
                await SyncUsers();
            }).Wait();
            // UserCombo.ItemsSource = usersList;
            foreach (var user in usersList)
            {
                UserItems.Add(new ComboBoxItem { Content = user.Name });
            }
        }

        private void _timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            var messages = MessageQueue.GetMessages();
            foreach (var message in messages)
            {
                Dispatcher.BeginInvoke(new ThreadStart(() => Messages.Text += Environment.NewLine + message));
                if (notifier != null) { Dispatcher.BeginInvoke(new ThreadStart(() => notifier.ShowInformation(message))); }
            }
        }

        private async Task<List<User>> SyncUsers()
        {
            var users = await sentinelData.InitUsers(usersList);
            return users;
        }

        private string FormatMinutes(int minutes)
        {
            var timeSpan = TimeSpan.FromMinutes(minutes);
            return timeSpan.ToString(@"hh\:mm");
        }

        private void UserCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var user = usersList.Find(_ => _.Name == SelectedUser.Content.ToString());
            if (user == null)
            {
                return;
            }


            UserNameTextBox.Text = user.Name;
            UserLasstLoginTextBox.Text = user.LastLogin.ToLongDateString();
            MondayTextBox.Text = FormatMinutes(user.Monday);
            TuesdayTextBox.Text = FormatMinutes(user.Tuesday);
            WednesdayTextBox.Text = FormatMinutes(user.Wednesday);
            ThursdayTextBox.Text = FormatMinutes(user.Thursday);
            FridayTextBox.Text = FormatMinutes(user.Friday);
            SaturdayTextBox.Text = FormatMinutes(user.Saturday);
            SundayTextBox.Text = FormatMinutes(user.Sunday);

            var elapsedControls = new List<TextBox>
            {
                UMondayTextBox,
                UTuesdayTextBox,
                UWednesdayTextBox,
                UThursdayTextBox,
                UFridayTextBox,
                USaturdayTextBox,
                USundayTextBox
            };

            var i = 0;
            var week = sentinelData.GetWeek();
            foreach (var day in week)
            {
                var dayData = user.LoginData.SingleOrDefault(_ =>
                    _.Date.DayOfYear == day.DayOfYear && _.Date.Year == day.Year);
                if (dayData == null)
                {
                    dayData = new LoginData { Date = day };
                    user.LoginData.Add(dayData);
                }

                elapsedControls[i].Text = FormatMinutes(dayData.TimeElapsed);
                i++;
            }
        }


        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.ShowDialog();
            if (dialog.FileName == "")
            {
                return;
            }

            ProcessStartInfo startInfo = new ProcessStartInfo($@"{AppDomain.CurrentDomain.BaseDirectory}Service/InstallUtil.exe",
                    $" /u \"{dialog.FileName}\"")
            { Verb = "runas" };
            Process.Start(startInfo);
            Thread.Sleep(2000);

            startInfo = new ProcessStartInfo($@"{System.AppDomain.CurrentDomain.BaseDirectory}Service/InstallUtil.exe",
                    $"\"{dialog.FileName}\"")
            { Verb = "runas" };
            Process.Start(startInfo);
            Thread.Sleep(2000);

            ServiceHelper.StartService(serviceName);
        }

        private async void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            var user = usersList.Find(_ => _.Name == SelectedUser.Content.ToString());

            var week = sentinelData.GetWeek();

            var allocatedControls = new List<TextBox>
            {
                MondayTextBox,
                TuesdayTextBox,
                WednesdayTextBox,
                ThursdayTextBox,
                FridayTextBox,
                SaturdayTextBox,
                SundayTextBox
            };
            var i = 0;
            foreach (var day in week)
            {
                var dayData = user.LoginData.SingleOrDefault(_ =>
                    _.Date.DayOfYear == day.DayOfYear && _.Date.Year == day.Year);
                if (dayData == null)
                {
                    dayData = new LoginData { Date = DateTime.Now };
                    user.LoginData.Add(dayData);
                }
                var dayArr = allocatedControls[i].Text.Split(':');
                var dayTime = (Convert.ToInt32(dayArr[0]) * 60) + Convert.ToInt32(dayArr[1]);
                dayData.TimeAllocated = dayTime;
                i++;
            }

            var monArr = MondayTextBox.Text.Split(':');
            var tueArr = TuesdayTextBox.Text.Split(':');
            var wedArr = WednesdayTextBox.Text.Split(':');
            var thuArr = ThursdayTextBox.Text.Split(':');
            var friArr = FridayTextBox.Text.Split(':');
            var satArr = SaturdayTextBox.Text.Split(':');
            var sunArr = SundayTextBox.Text.Split(':');

            var mon = (Convert.ToInt32(monArr[0]) * 60) + Convert.ToInt32(monArr[1]);
            var tue = (Convert.ToInt32(tueArr[0]) * 60) + Convert.ToInt32(tueArr[1]);
            var wed = (Convert.ToInt32(wedArr[0]) * 60) + Convert.ToInt32(wedArr[1]);
            var thu = (Convert.ToInt32(thuArr[0]) * 60) + Convert.ToInt32(thuArr[1]);
            var fri = (Convert.ToInt32(friArr[0]) * 60) + Convert.ToInt32(friArr[1]);
            var sat = (Convert.ToInt32(satArr[0]) * 60) + Convert.ToInt32(satArr[1]);
            var sun = (Convert.ToInt32(sunArr[0]) * 60) + Convert.ToInt32(sunArr[1]);

            user.Monday = mon;
            user.Tuesday = tue;
            user.Wednesday = wed;
            user.Thursday = thu;
            user.Friday = fri;
            user.Saturday = sat;
            user.Sunday = sun;
            await sentinelData.UpdateUser(user);
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            UserItems = new ObservableCollection<ComboBoxItem>();
            var cbItem = new ComboBoxItem { Content = "<--Select-->" };
            SelectedUser = cbItem;
            UserItems.Add(cbItem);
            usersList = await sentinelData.GetUsers();
            await SyncUsers();
            // UserCombo.ItemsSource = usersList;
            foreach (var user in usersList)
            {
                UserItems.Add(new ComboBoxItem { Content = user.Name });
            }
        }

        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {
            notifier.Dispose();
            host.Close();
            host = null;
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            notifier = new Notifier(cfg =>
            {
                cfg.PositionProvider = new WindowPositionProvider(
                    parentWindow: Application.Current.MainWindow,
                    corner: Corner.TopRight,
                    offsetX: 10,
                    offsetY: 10);

                cfg.LifetimeSupervisor = new TimeAndCountBasedLifetimeSupervisor(
                    notificationLifetime: TimeSpan.FromSeconds(3),
                    maximumNotificationCount: MaximumNotificationCount.FromCount(5));

                cfg.Dispatcher = Application.Current.Dispatcher;
            });

        }
    }
}
