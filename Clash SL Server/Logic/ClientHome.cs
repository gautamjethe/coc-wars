using System;
using System.Collections.Generic;
using System.IO;
using CSS.Helpers;
using CSS.Helpers.List;

namespace CSS.Logic
{
    internal class ClientHome
    {
        internal long Id;
        internal string Village;
        internal int ShieldTime;
        internal int ProtectionTime;

        private static string s_calendarJson = null;
        private static DateTime s_calendarLastModified = DateTime.MinValue;

        public static string GetCalendarJson()
        {
            try
            {
                string path = "calendar.json";
                if (!File.Exists(path) && File.Exists("Gamefiles/calendar.json"))
                {
                    path = "Gamefiles/calendar.json";
                }

                if (File.Exists(path))
                {
                    DateTime lastWrite = File.GetLastWriteTimeUtc(path);
                    if (s_calendarJson == null || lastWrite > s_calendarLastModified)
                    {
                        string content = File.ReadAllText(path).Trim();
                        if (content.Contains("\"events\""))
                        {
                            s_calendarJson = content;
                            s_calendarLastModified = lastWrite;
                        }
                    }
                }
            }
            catch (Exception)
            {
            }

            if (string.IsNullOrEmpty(s_calendarJson))
            {
                return "{\"events\":[]}";
            }
            return s_calendarJson;
        }

        public ClientHome()
        {
        }

        public byte[] Encode
        {
            get
            {
                List<byte> data = new List<byte>();
                data.AddLong(this.Id);

                data.AddInt(this.ShieldTime); // Shield
                data.AddInt(this.ProtectionTime); // Protection

                data.AddInt(0);
                data.AddCompressed(Village);
                data.AddCompressed(GetCalendarJson());
                return data.ToArray();
            }
        }
    }
}
