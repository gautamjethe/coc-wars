using System;
using System.Configuration;
using System.Linq;
using System.Timers;
using CSS.Core.Network;
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

        private static bool _enabled = true;
        private static int _startHourIST = 11;
        private static int _startMinuteIST = 0;
        private static int _durationMinutes = 30;

        public static void Init()
        {
            LoadConfig();

            _timer = new System.Timers.Timer(10000); // Check every 10 seconds
            _timer.Elapsed += OnTimerElapsed;
            _timer.AutoReset = true;
            _timer.Start();

            // Run an immediate check on startup
            CheckSchedule();

            Logger.Write($"[MAINTENANCE] DailyMaintenanceManager initialized. Scheduled: {_startHourIST:D2}:{_startMinuteIST:D2} IST daily for {_durationMinutes} mins (Enabled: {_enabled}).");
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
            }
            catch (Exception ex)
            {
                Logger.Write($"[MAINTENANCE] Error loading config: {ex.Message}. Using default 11:00 AM IST for 30 mins.");
            }
        }

        private static void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            CheckSchedule();
        }

        private static void CheckSchedule()
        {
            if (!_enabled)
                return;

            DateTime istNow = DateTime.UtcNow.AddHours(5.5);
            TimeSpan istTime = istNow.TimeOfDay;

            TimeSpan maintenanceStart = new TimeSpan(_startHourIST, _startMinuteIST, 0);
            TimeSpan maintenanceEnd = maintenanceStart.Add(TimeSpan.FromMinutes(_durationMinutes));
            TimeSpan warningStart = maintenanceStart.Subtract(TimeSpan.FromMinutes(5));

            // 1. 5-Minute Warning Phase (10:55 AM IST to 11:00 AM IST)
            if (istTime >= warningStart && istTime < maintenanceStart)
            {
                if (!_warningSentToday || _lastWarningDate.Date != istNow.Date)
                {
                    int secondsUntilStart = (int)Math.Max(1, (maintenanceStart - istTime).TotalSeconds);
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

                    _warningSentToday = true;
                    _lastWarningDate = istNow.Date;
                    Logger.Write($"[MAINTENANCE] 5-minute maintenance countdown broadcasted to online players ({secondsUntilStart}s).");
                }
            }

            // 2. Maintenance Active Phase (11:00 AM IST to 11:30 AM IST)
            if (istTime >= maintenanceStart && istTime < maintenanceEnd)
            {
                if (!_isMaintenanceActive)
                {
                    _isMaintenanceActive = true;
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Logger.Write($"[MAINTENANCE] >>> Daily Maintenance started at {istNow:HH:mm:ss} IST! Scheduled until {_startHourIST:D2}:{_startMinuteIST + _durationMinutes:D2} IST. <<<");
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

            DateTime istNow = DateTime.UtcNow.AddHours(5.5);
            TimeSpan istTime = istNow.TimeOfDay;

            TimeSpan maintenanceStart = new TimeSpan(_startHourIST, _startMinuteIST, 0);
            TimeSpan maintenanceEnd = maintenanceStart.Add(TimeSpan.FromMinutes(_durationMinutes));

            if (istTime >= maintenanceStart && istTime < maintenanceEnd)
            {
                return (int)Math.Max(1, (maintenanceEnd - istTime).TotalSeconds);
            }

            return 0;
        }
    }
}
