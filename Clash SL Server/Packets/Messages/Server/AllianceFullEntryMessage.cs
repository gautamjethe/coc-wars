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

                // CRITICAL: libg.so 0xb7505 reads [fp+8] (war state stored by THIS packet 24324)
                // inside the 24335 decode handler and creates TID_ALLIANCE_WAR_EVENT_WAR_STARTED banner
                // when that value > 3. The banner tick at 0xe2f24 then crashes (getMemberByPlayerId NULL).
                // Fix: cap state at 3 in village mode. The HUD button text is set independently by
                // AllianceWarDataMessage (24331) which calls setWarState(4) → "Preparation Day".
                bool inWarMode = this.Device?.PlayerState == Logic.Enums.State.WAR_EMODE;
                int stateToSend = warState;
                if (!inWarMode && stateToSend > 3)
                    stateToSend = 3;  // WAR_FOUND — prevents banner creation in 0xb7505

                this.Data.AddInt(stateToSend);   // Field 2: War state (offset 0x48 / fp+8)
                this.Data.AddInt(remainingTime);  // Field 3: Remaining countdown (offset 0x54)

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
