using System;
using System.Configuration;
using System.Linq;
using System.Timers;
using CSS.Core.Network;
using CSS.Helpers;
using CSS.Logic;
using CSS.Packets.Messages.Server;

namespace CSS.Core
{
    internal static class DailyMaintenanceManager
    {
        private static System.Timers.Timer _timer;
        private static bool _isMaintenanceActive;
        private static bool _warningSentToday;
        private static DateTime _lastWarningDate = DateTime.MinValue;
        private static DateTime _lastWarningBroadcast = DateTime.MinValue;

        private static bool _enabled = true;
        private static int _startHourIST = 11;
        private static int _startMinuteIST = 0;
        private static int _durationMinutes = 30;
        private static int _warningMinutes = 10;

        public static void Init()
        {
            LoadConfig();

            _timer = new System.Timers.Timer(10000); // Check every 10 seconds
            _timer.Elapsed += OnTimerElapsed;
            _timer.AutoReset = true;
            _timer.Start();

            // Run an immediate check on startup
            CheckSchedule();

            Logger.Write($"[MAINTENANCE] DailyMaintenanceManager initialized. Scheduled: {_startHourIST:D2}:{_startMinuteIST:D2} IST daily for {_durationMinutes} mins (Warning: {_warningMinutes} mins prior, Enabled: {_enabled}).");
        }

        private static void LoadConfig()
        {
            try
            {
                if (!string.IsNullOrEmpty(ConfigurationManager.AppSettings["DailyMaintenanceEnabled"]))
                {
                    _enabled = Convert.ToBoolean(ConfigurationManager.AppSettings["DailyMaintenanceEnabled"]);
                }

                if (!string.IsNullOrEmpty(ConfigurationManager.AppSettings["DailyMaintenanceHourIST"]))
                {
                    _startHourIST = Convert.ToInt32(ConfigurationManager.AppSettings["DailyMaintenanceHourIST"]);
                }

                if (!string.IsNullOrEmpty(ConfigurationManager.AppSettings["DailyMaintenanceMinuteIST"]))
                {
                    _startMinuteIST = Convert.ToInt32(ConfigurationManager.AppSettings["DailyMaintenanceMinuteIST"]);
                }

                if (!string.IsNullOrEmpty(ConfigurationManager.AppSettings["DailyMaintenanceDurationMinutes"]))
                {
                    _durationMinutes = Convert.ToInt32(ConfigurationManager.AppSettings["DailyMaintenanceDurationMinutes"]);
                }

                if (!string.IsNullOrEmpty(ConfigurationManager.AppSettings["DailyMaintenanceWarningMinutes"]))
                {
                    _warningMinutes = Convert.ToInt32(ConfigurationManager.AppSettings["DailyMaintenanceWarningMinutes"]);
                }
            }
            catch (Exception ex)
            {
                Logger.Write($"[MAINTENANCE] Error loading config: {ex.Message}. Using default 11:00 AM IST for 30 mins (10 mins warning).");
            }
        }

        private static void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            CheckSchedule();
        }

        public static DateTime GetIstNow() => DateTime.UtcNow.AddHours(5.5);

        private static void CheckSchedule()
        {
            if (!_enabled)
                return;

            DateTime istNow = GetIstNow();
            DateTime todayStart = new DateTime(istNow.Year, istNow.Month, istNow.Day, _startHourIST, _startMinuteIST, 0);
            DateTime todayEnd = todayStart.AddMinutes(_durationMinutes);
            DateTime todayWarning = todayStart.AddMinutes(-_warningMinutes);

            // 1. Warning Phase (e.g. 10:50 AM IST to 11:00 AM IST)
            if (istNow >= todayWarning && istNow < todayStart)
            {
                int secondsUntilStart = (int)Math.Max(1, (todayStart - istNow).TotalSeconds);

                // Broadcast on first entry, and rebroadcast every 30 seconds to keep all online and reconnected players synced
                if (!_warningSentToday || (istNow - _lastWarningBroadcast).TotalSeconds >= 30)
                {
                    BroadcastWarning(secondsUntilStart);
                    _lastWarningBroadcast = istNow;

                    if (!_warningSentToday)
                    {
                        _warningSentToday = true;
                        _lastWarningDate = istNow.Date;
                        Logger.Write($"[MAINTENANCE] {_warningMinutes}-minute maintenance countdown broadcasted to online players ({secondsUntilStart}s).");
                    }
                }
            }
            // 2. Maintenance Active Phase (11:00 AM IST to 11:30 AM IST)
            else if (istNow >= todayStart && istNow < todayEnd)
            {
                if (!_isMaintenanceActive)
                {
                    _isMaintenanceActive = true;
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Logger.Write($"[MAINTENANCE] >>> Daily Maintenance started at {istNow:HH:mm:ss} IST! Scheduled until {todayEnd:HH:mm:ss} IST. <<<");
                    Console.ResetColor();

                    // Disconnect all connected players
                    foreach (Level p in ResourcesManager.m_vInMemoryLevels.Values.ToList())
                    {
                        if (p?.Client != null)
                        {
                            try
                            {
                                new OutOfSyncMessage(p.Client).Send();
                                ResourcesManager.DropClient(p.Client.SocketHandle);
                            }
                            catch
                            {
                            }
                        }
                    }

                    // Flush and persist everything to database
                    try
                    {
                        Resources.DatabaseManager.Save(ResourcesManager.m_vInMemoryLevels.Values.ToList());
                        Resources.DatabaseManager.Save(ResourcesManager.GetInMemoryAlliances());
                        Logger.Write("[MAINTENANCE] All in-memory players and clans saved to MySQL database successfully.");
                    }
                    catch (Exception dbEx)
                    {
                        Logger.Write($"[MAINTENANCE] Error saving database during maintenance start: {dbEx.Message}");
                    }
                }
            }
            // 3. Maintenance Ended Phase
            else if (_isMaintenanceActive)
            {
                _isMaintenanceActive = false;
                _warningSentToday = false;
                Console.ForegroundColor = ConsoleColor.Green;
                Logger.Write($"[MAINTENANCE] >>> Daily Maintenance finished at {istNow:HH:mm:ss} IST! Server is now open for players. <<<");
                Console.ResetColor();
            }
            else
            {
                // Reset daily warning flag if we passed the window
                if (_warningSentToday && _lastWarningDate.Date != istNow.Date)
                {
                    _warningSentToday = false;
                }
            }
        }

        public static void BroadcastWarning(int secondsUntilStart)
        {
            foreach (Level p in ResourcesManager.m_vOnlinePlayers.ToList())
            {
                if (p?.Client != null)
                {
                    try
                    {
                        new ShutdownStartedMessage(p.Client) { Code = secondsUntilStart }.Send();
                    }
                    catch
                    {
                    }
                }
            }
        }

        public static bool IsWarningActive()
        {
            if (!_enabled)
                return false;

            DateTime istNow = GetIstNow();
            DateTime todayStart = new DateTime(istNow.Year, istNow.Month, istNow.Day, _startHourIST, _startMinuteIST, 0);
            DateTime todayWarning = todayStart.AddMinutes(-_warningMinutes);

            return istNow >= todayWarning && istNow < todayStart;
        }

        public static int GetWarningRemainingSeconds()
        {
            if (!_enabled)
                return 0;

            DateTime istNow = GetIstNow();
            DateTime todayStart = new DateTime(istNow.Year, istNow.Month, istNow.Day, _startHourIST, _startMinuteIST, 0);
            DateTime todayWarning = todayStart.AddMinutes(-_warningMinutes);

            if (istNow >= todayWarning && istNow < todayStart)
            {
                return (int)Math.Max(1, (todayStart - istNow).TotalSeconds);
            }

            return 0;
        }

        public static bool IsMaintenanceActive()
        {
            if (!_enabled)
                return false;

            return _isMaintenanceActive;
        }

        public static int GetRemainingSeconds()
        {
            if (!_enabled)
                return 0;

            DateTime istNow = GetIstNow();
            DateTime todayStart = new DateTime(istNow.Year, istNow.Month, istNow.Day, _startHourIST, _startMinuteIST, 0);
            DateTime todayEnd = todayStart.AddMinutes(_durationMinutes);

            if (istNow >= todayStart && istNow < todayEnd)
            {
                return (int)Math.Max(1, (todayEnd - istNow).TotalSeconds);
            }

            return 0;
        }

        public static bool IsAttackDisabled()
        {
            return IsWarningActive() || ParserThread.GetMaintenanceMode();
        }

        public static int GetShutdownRemainingSeconds()
        {
            if (IsWarningActive())
                return GetWarningRemainingSeconds();
            if (IsMaintenanceActive())
                return GetRemainingSeconds();
            return ParserThread.GetMaintenanceTime();
        }
    }
}
