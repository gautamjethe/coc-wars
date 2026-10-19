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
                if (m_vWar == null && this.Device?.Player?.Avatar != null && this.Device.Player.Avatar.AllianceId > 0)
                {
                    m_vWar = ClanWarManager.GetWarByClanId(this.Device.Player.Avatar.AllianceId);
                }

                int warState = m_vWar != null ? (int)m_vWar.State : 0;
                long warId = m_vWar != null ? m_vWar.WarId : 0;

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
