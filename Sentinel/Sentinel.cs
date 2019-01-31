using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.ServiceModel;
using System.ServiceProcess;
using System.Threading.Tasks;
using System.Timers;
using SentinelData;

namespace Sentinel
{
    public partial class Sentinel : ServiceBase
    {
        private readonly Timer _timer = new Timer();
        private readonly SentinelData.SentinelData _sentinelData = new SentinelData.SentinelData();
        public List<User> UsersList = new List<User>();
        private User _activeUser = null;
        public Sentinel()
        {
            InitializeComponent();
            CanHandleSessionChangeEvent = true;
        }


        [DllImport("wtsapi32.dll", SetLastError = true)]
        static extern bool WTSDisconnectSession(IntPtr hServer, int sessionId, bool bWait);

        [DllImport("wtsapi32.dll", SetLastError = true)]
        static extern int WTSEnumerateSessions(IntPtr hServer, int Reserved, int Version, ref IntPtr ppSessionInfo, ref int pCount);

        [DllImport("wtsapi32.dll")]
        static extern void WTSFreeMemory(IntPtr pMemory);

        [StructLayout(LayoutKind.Sequential)]
        private struct WTS_SESSION_INFO
        {
            public Int32 SessionID;

            [MarshalAs(UnmanagedType.LPStr)]
            public String pWinStationName;

            public WTS_CONNECTSTATE_CLASS State;
        }

        private enum WTS_INFO_CLASS
        {
            WTSInitialProgram,
            WTSApplicationName,
            WTSWorkingDirectory,
            WTSOEMId,
            WTSSessionId,
            WTSUserName,
            WTSWinStationName,
            WTSDomainName,
            WTSConnectState,
            WTSClientBuildNumber,
            WTSClientName,
            WTSClientDirectory,
            WTSClientProductId,
            WTSClientHardwareId,
            WTSClientAddress,
            WTSClientDisplay,
            WTSClientProtocolType
        }

        private enum WTS_CONNECTSTATE_CLASS
        {
            WTSActive,
            WTSConnected,
            WTSConnectQuery,
            WTSShadow,
            WTSDisconnected,
            WTSIdle,
            WTSListen,
            WTSReset,
            WTSDown,
            WTSInit
        }

        public static void LockWorkStation()
        {
            IntPtr ppSessionInfo = IntPtr.Zero;
            Int32 count = 0;
            Int32 retval = WTSEnumerateSessions(IntPtr.Zero, 0, 1, ref ppSessionInfo, ref count);
            Int32 dataSize = Marshal.SizeOf(typeof(WTS_SESSION_INFO));
            Int32 currentSession = (int)ppSessionInfo;

            if (retval == 0) return;

            for (int i = 0; i < count; i++)
            {
                WTS_SESSION_INFO si = (WTS_SESSION_INFO)Marshal.PtrToStructure((System.IntPtr)currentSession, typeof(WTS_SESSION_INFO));
                if (si.State == WTS_CONNECTSTATE_CLASS.WTSActive) WTSDisconnectSession(IntPtr.Zero, si.SessionID, false);
                currentSession += dataSize;
            }
            WTSFreeMemory(ppSessionInfo);
        }

        protected override async void OnStart(string[] args)
        {
            await SetActiveUser();
            WriteToFile("Service started");
            WriteToFile("Getting Users");
            WriteToFile("Finished Getting Users");
            _timer.Elapsed += OnElapsedTime;
            //_timer.Interval = 60000;
            _timer.Interval = 10000;
            _timer.Enabled = true;
        }

        private async Task<bool> SetActiveUser()
        {
            _activeUser = null;
            UsersList = await _sentinelData.GetUsers();
            var ms = new ManagementScope("\\\\.\\root\\cimv2");
            var query = new ObjectQuery("SELECT * FROM Win32_ComputerSystem");
            var searcher = new ManagementObjectSearcher(ms, query);
            foreach (var mo in searcher.Get())
            {
                var username = mo["UserName"].ToString();
                var usernamenodom = mo["UserName"].ToString().Split('\\').Last();
                WriteToFile("looking for user : " + username);
                WriteToFile("user no dom : " + usernamenodom);
                // var username = usernamearray.Last();
                var user = UsersList.Find(_ => _.Name == username);
                if (user == null)
                {
                    WriteToFile("user not found, trying with " + usernamenodom);
                    user = UsersList.Find(_ => _.Name == usernamenodom);
                    WriteToFile(user == null ? "user not found" : "user found");
                }
                _activeUser = user;
            }
            return true;
        }

        protected override void OnStop()
        {
            WriteToFile("Service stopped");
        }
        private async void OnElapsedTime(object source, ElapsedEventArgs e)
        {
            try
            {
                WriteToFile("Service Timer Check");
                if (_activeUser == null) return;
                var loginData = _activeUser.LoginData.SingleOrDefault(_ =>
                    _.Date.DayOfYear == DateTime.Today.DayOfYear && _.Date.Year == DateTime.Today.Year);
                if (loginData == null)
                {
                    var dow = DateTime.Today.DayOfWeek;
                    int timeAllocated = 0;
                    switch (dow)
                    {
                        case DayOfWeek.Monday:
                            timeAllocated = _activeUser.Monday;
                            break;
                        case DayOfWeek.Tuesday:
                            timeAllocated = _activeUser.Tuesday;
                            break;
                        case DayOfWeek.Wednesday:
                            timeAllocated = _activeUser.Wednesday;
                            break;
                        case DayOfWeek.Thursday:
                            timeAllocated = _activeUser.Thursday;
                            break;
                        case DayOfWeek.Friday:
                            timeAllocated = _activeUser.Friday;
                            break;
                        case DayOfWeek.Saturday:
                            timeAllocated = _activeUser.Saturday;
                            break;
                        case DayOfWeek.Sunday:
                            timeAllocated = _activeUser.Sunday;
                            break;
                    }

                    loginData = new LoginData { Date = DateTime.Now, TimeAllocated = timeAllocated };
                    _activeUser.LoginData.Add(loginData);
                }
                loginData.TimeElapsed += 1;
                WriteToFile("Checking time");
                WriteToFile("Allocated " + loginData.TimeAllocated);
                WriteToFile("TimeElapsed " + loginData.TimeElapsed);
                WriteToFile("TimeLEFT " + (loginData.TimeAllocated - loginData.TimeElapsed));
                if ((loginData.TimeAllocated - loginData.TimeElapsed) <= 0)
                {
                    WriteToFile("Time Exceeded!!");
                    LockWorkStation();
                }
                await _sentinelData.UpdateUser(_activeUser);
            }
            catch (Exception err)
            {
                WriteToFile("Error :  " + err.GetBaseException().Message);
            }
        }

        protected override async void OnSessionChange(SessionChangeDescription changeDescription)
        {
            try
            {
                await SetActiveUser();
                if (_activeUser == null) return;

                WriteToFile("Session Change Detected : " + changeDescription.Reason);

                // check for refesh
                switch (changeDescription.Reason)
                {
                    case SessionChangeReason.SessionLogon:
                        _activeUser.LastLogin = DateTime.Now;
                        WriteToFile(changeDescription.SessionId + " logon");
                        break;
                    case SessionChangeReason.SessionLogoff:
                        _activeUser = null;
                        WriteToFile(changeDescription.SessionId + " logoff");
                        break;
                    case SessionChangeReason.SessionLock:
                        _activeUser = null;
                        WriteToFile(changeDescription.SessionId + " lock");
                        break;
                    case SessionChangeReason.SessionUnlock:
                        _activeUser.LastLogin = DateTime.Now;
                        WriteToFile(changeDescription.SessionId + " unlock");
                        break;
                }

                await _sentinelData.UpdateUser(_activeUser);

                base.OnSessionChange(changeDescription);
            }
            catch (Exception e)
            {
                WriteToFile("Error :  " + e.GetBaseException().Message);
            }
        }
        public void WriteToFile(string message)
        {
            File.AppendAllText("c:\\temp\\slog.txt", $@"{Environment.NewLine} {DateTime.Now} - : {message}");
            try
            {
                MessageQueue.AddMessage($@"{Environment.NewLine} {DateTime.Now} - : {message}");
            }
            catch (Exception err)
            {
                File.AppendAllText("c:\\temp\\slog.txt", $@"error adding message {err.GetBaseException().Message}");
            }
        }
    }
}
