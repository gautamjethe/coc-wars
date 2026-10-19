using System;
using System.Collections.Generic;
using System.Linq;
using CSS.Core;
using CSS.Helpers.List;
using CSS.Logic;

namespace CSS.Packets.Messages.Server
{
    // Packet 24402
    internal class LocalAlliancesMessage : Message
    {
        public LocalAlliancesMessage(Device client) : base(client)
        {
            this.Identifier = 24402;
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
            }
            catch (Exception ex)
            {
                Logger.Write($"[Error] LocalAlliancesMessage encode error: {ex.Message}");
            }
        }
    }
}
