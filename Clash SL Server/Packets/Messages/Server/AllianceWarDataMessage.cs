using System;
using System.Collections.Generic;
using CSS.Core;
using CSS.Helpers.List;
using CSS.Logic;

namespace CSS.Packets.Messages.Server
{
    // Packet 24331
    internal class AllianceWarDataMessage : Message
    {
        private ClanWar m_vWar;

        public AllianceWarDataMessage(Device client, ClanWar war = null) : base(client)
        {
            this.Identifier = 24331;
            this.m_vWar = war;
        }

        internal override void Encode()
        {
            try
            {
                long clanId = this.Device?.Player?.Avatar?.AllianceId ?? 0;
                if (m_vWar == null && clanId > 0)
                {
                    m_vWar = ClanWarManager.GetWarByClanId(clanId);
                }

                int warState = 0;
                long warId = 0;

                if (m_vWar != null)
                {
                    warState = (int)m_vWar.State;
                    warId = m_vWar.WarId;
                }
                else if (clanId > 0 && ClanWarManager.IsClanSearching(clanId))
                {
                    warState = (int)ClanWarState.SEARCHING; // 1
                    warId = 0;
                }

                this.Data.AddInt(warState);
                this.Data.AddLong(warId);
            }
            catch (Exception ex)
            {
                Logger.Write($"[Error] AllianceWarDataMessage encode error: {ex.Message}");
            }
        }
    }
}
