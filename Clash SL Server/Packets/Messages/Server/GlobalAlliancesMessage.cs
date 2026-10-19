using System;
using System.Collections.Generic;
using System.Linq;
using CSS.Core;
using CSS.Helpers.List;
using CSS.Logic;

namespace CSS.Packets.Messages.Server
{
    // Packet 24401
    internal class GlobalAlliancesMessage : Message
    {
        public GlobalAlliancesMessage(Device client) : base(client)
        {
            this.Identifier = 24401;
        }

        internal override void Encode()
        {
            try
            {
                var topClans = DatabaseManager.GetTopClans(100);

                List<byte> clanData = new List<byte>();
                int rank = 1;

                foreach (Alliance alliance in topClans)
                {
                    if (alliance == null) continue;

                    clanData.AddLong(alliance.m_vAllianceId);
                    clanData.AddString(alliance.m_vAllianceName ?? "Clan");
                    clanData.AddInt(rank);
                    clanData.AddInt(alliance.m_vScore);
                    clanData.AddInt(rank);
                    clanData.AddInt(alliance.m_vAllianceBadgeData);
                    clanData.AddInt(alliance.GetAllianceMembers().Count);
                    clanData.AddInt(0);
                    clanData.AddInt(alliance.m_vAllianceLevel);

                    rank++;
                }

                this.Data.AddInt(rank - 1);
                this.Data.AddRange(clanData);

                // Clan tournament timer & rewards (persisted in database)
                DatabaseManager db = Resources.DatabaseManager ?? new DatabaseManager();
                var season = db.GetOrCreateLeaderboardSeason("ClanSeason", 1209600); // 14-day tournament

                this.Data.AddInt(season.RemainingSeconds);
                this.Data.AddInt(3);
                this.Data.AddInt(50000);
                this.Data.AddInt(30000);
                this.Data.AddInt(15000);
            }
            catch (Exception ex)
            {
                Logger.Write($"[Error] GlobalAlliancesMessage encode error: {ex.Message}");
            }
        }
    }
}
