using System;
using System.Collections.Generic;
using CSS.Helpers.List;
using CSS.Logic;

namespace CSS.Packets.Messages.Server
{
    // Packet 24324
    internal class AllianceFullEntryMessage : Message
    {
        readonly Alliance m_vAlliance;

        public AllianceFullEntryMessage(Device client, Alliance alliance) : base(client)
        {
            this.Identifier = 24324;
            this.m_vAlliance = alliance;
        }

        internal override void Encode()
        {
            try
            {
                this.Data.AddString(m_vAlliance.m_vAllianceDescription ?? "");

                ClanWar war = ClanWarManager.GetWarByClanId(m_vAlliance.m_vAllianceId);
                bool isSearching = m_vAlliance.m_vAllianceId > 0 && ClanWarManager.IsClanSearching(m_vAlliance.m_vAllianceId);
                int warState = war != null ? (int)war.State : (isSearching ? (int)ClanWarState.SEARCHING : m_vAlliance.m_vWarState);
                int remainingTime = 0;
                if (war != null)
                {
                    remainingTime = war.RemainingTimeSeconds;
                }
                else if (isSearching && ClanWarManager.SearchingClans.TryGetValue(m_vAlliance.m_vAllianceId, out var info))
                {
                    remainingTime = Math.Max(0, (int)(DateTime.UtcNow - info.SearchStartTime).TotalSeconds);
                }

                this.Data.AddInt(warState);      // Field 2: War state (offset 0x48)
                this.Data.AddInt(remainingTime); // Field 3: Remaining countdown time in seconds (offset 0x54)

                if (war != null && war.State != ClanWarState.NOT_IN_WAR)
                {
                    this.Data.Add((byte)1);
                    this.Data.AddLong(war.WarId); // Field 4: War ID (offset 0x58)
                }
                else
                {
                    this.Data.Add((byte)0);
                }

                this.Data.AddInt(0);             // Field 5: Unknown int32 (offset 0x4c)
                this.Data.Add((byte)0);          // Field 6: Optional long flag (offset 0x50)
                this.Data.AddRange(m_vAlliance.EncodeFullEntry());
            }
            catch (Exception ex)
            {
                Core.Logger.Write($"[Error] AllianceFullEntryMessage encode error: {ex.Message}");
            }
        }
    }
}
