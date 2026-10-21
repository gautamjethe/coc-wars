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
                    // CRITICAL: libg.so 0x86d8d reads a global war-manager singleton then calls into
                    // the war's packet handler list. If the war state > 3 it may allocate structures
                    // that are not yet initialized in village mode, causing a null-ptr crash at 0x86d97.
                    // Cap state to 3 (WAR_FOUND) in village mode; AllianceWarDataMessage (24331) is
                    // only used to set the war HUD button text - the display is driven by state stored
                    // from AllianceFullEntryMessage which is also capped to 3 in village mode.
                    bool inWarMode = this.Device?.PlayerState == Logic.Enums.State.WAR_EMODE;
                    int stateToSend = (int)m_vWar.State;
                    if (!inWarMode && stateToSend > 3)
                        stateToSend = 3;

                    this.Data.AddInt(stateToSend);
                    this.Data.AddLong(m_vWar.WarId);
                }
                else
                {
                    // No active war - send NOT_IN_WAR (0) with warId=0.
                    // Client at libg.so 0x86cb1: cmp r8, #0 / beq #0x8703d → safely skips all processing.
                    int warState = 0;
                    if (playerClanId > 0 && ClanWarManager.IsClanSearching(playerClanId))
                        warState = (int)ClanWarState.SEARCHING;

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
