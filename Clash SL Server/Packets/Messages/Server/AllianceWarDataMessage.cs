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
                long playerClanId = this.Device?.Player?.Avatar?.AllianceId ?? 0;
                if (m_vWar == null && playerClanId > 0)
                {
                    m_vWar = ClanWarManager.GetWarByClanId(playerClanId);
                }

                if (m_vWar != null)
                {
                    this.Data.AddInt((int)m_vWar.State);
                    this.Data.AddLong(m_vWar.WarId);
                }
                else
                {
                    int warState = 0;
                    if (playerClanId > 0 && ClanWarManager.IsClanSearching(playerClanId))
                    {
                        warState = (int)ClanWarState.SEARCHING;
                    }

                    this.Data.AddInt(warState);
                    this.Data.AddLong(0);
                }
            }
            catch (Exception ex)
            {
                Logger.Write($"[Error] AllianceWarDataMessage encode error: {ex.Message}");
            }
        }
    }
}
