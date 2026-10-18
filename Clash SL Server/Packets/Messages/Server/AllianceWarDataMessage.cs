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

                if (m_vWar != null)
                {
                    long playerClanId = this.Device?.Player?.Avatar?.AllianceId ?? 0;
                    WarClan c1 = m_vWar.Clan1;
                    WarClan c2 = m_vWar.Clan2;
                    if (playerClanId > 0 && m_vWar.Clan2 != null && m_vWar.Clan2.ClanId == playerClanId)
                    {
                        c1 = m_vWar.Clan2;
                        c2 = m_vWar.Clan1;
                    }

                    this.Data.AddInt((int)m_vWar.State);
                    this.Data.AddInt(m_vWar.RemainingTimeSeconds);

                    // Team 1
                    this.Data.AddLong(c1.ClanId);
                    this.Data.AddString(c1.ClanName);
                    this.Data.AddInt(c1.ClanBadge);
                    this.Data.AddInt(c1.ClanLevel);
                    this.Data.Add((byte)0);
                    this.Data.AddRange(new List<byte> { 1, 2, 3, 4 });
                    this.Data.AddInt(c1.TotalStars);
                    this.Data.AddInt((int)c1.TotalDestruction);
                    this.Data.AddInt(0);
                    this.Data.AddInt(c1.AttacksUsed);
                    this.Data.AddInt(0);

                    // Team 2
                    this.Data.AddLong(c2.ClanId);
                    this.Data.AddString(c2.ClanName);
                    this.Data.AddInt(c2.ClanBadge);
                    this.Data.AddInt(c2.ClanLevel);
                    this.Data.Add((byte)0);
                    this.Data.AddRange(new List<byte> { 1, 2, 3, 4 });
                    this.Data.AddInt(c2.TotalStars);
                    this.Data.AddInt((int)c2.TotalDestruction);
                    this.Data.AddInt(0);
                    this.Data.AddInt(c2.AttacksUsed);
                    this.Data.AddInt(0);

                    this.Data.AddLong(m_vWar.WarId);

                    this.Data.AddInt(0);
                    this.Data.AddInt(0);

                    this.Data.AddInt(1);
                    this.Data.AddInt(m_vWar.RemainingTimeSeconds);
                    this.Data.AddLong(c1.ClanId);
                    this.Data.AddLong(c1.ClanId);
                    this.Data.AddLong(c2.ClanId);
                    this.Data.AddLong(c2.ClanId);

                    this.Data.AddString(c1.ClanName);
                    this.Data.AddString(c2.ClanName);

                    this.Data.AddInt(c1.ClanLevel);
                    this.Data.AddInt(c2.ClanLevel);
                    this.Data.AddInt(m_vWar.WarSize);

                    this.Data.AddInt(0);

                    this.Data.AddInt(8);
                    this.Data.AddInt(0);
                    this.Data.AddInt(0);
                    this.Data.Add((byte)0);
                    this.Data.AddInt(0);
                    this.Data.AddInt(0);
                    this.Data.AddInt(0);
                    this.Data.AddInt(0);
                }
                else
                {
                    // No active war
                    this.Data.AddInt(0);
                    this.Data.AddInt(0);

                    long clanId = this.Device?.Player?.Avatar?.AllianceId ?? 1;
                    string clanName = "Clan";
                    Alliance a = ObjectManager.GetAlliance(clanId);
                    if (a != null) clanName = a.m_vAllianceName;

                    this.Data.AddLong(clanId);
                    this.Data.AddString(clanName);
                    this.Data.AddInt(0);
                    this.Data.AddInt(1);
                    this.Data.Add((byte)0);
                    this.Data.AddRange(new List<byte> { 1, 2, 3, 4 });
                    this.Data.AddInt(0);
                    this.Data.AddInt(0);
                    this.Data.AddInt(0);
                    this.Data.AddInt(0);
                    this.Data.AddInt(0);

                    this.Data.AddLong(0);
                    this.Data.AddString("Enemy");
                    this.Data.AddInt(0);
                    this.Data.AddInt(1);
                    this.Data.Add((byte)0);
                    this.Data.AddRange(new List<byte> { 1, 2, 3, 4 });
                    this.Data.AddInt(0);
                    this.Data.AddInt(0);
                    this.Data.AddInt(0);
                    this.Data.AddInt(0);
                    this.Data.AddInt(0);

                    this.Data.AddLong(0);

                    this.Data.AddInt(0);
                    this.Data.AddInt(0);

                    this.Data.AddInt(0);
                    this.Data.AddInt(0);
                    this.Data.AddLong(0);
                    this.Data.AddLong(0);
                    this.Data.AddLong(0);
                    this.Data.AddLong(0);

                    this.Data.AddString(clanName);
                    this.Data.AddString("Enemy");

                    this.Data.AddInt(1);
                    this.Data.AddInt(1);
                    this.Data.AddInt(10);

                    this.Data.AddInt(0);

                    this.Data.AddInt(8);
                    this.Data.AddInt(0);
                    this.Data.AddInt(0);
                    this.Data.Add((byte)0);
                    this.Data.AddInt(0);
                    this.Data.AddInt(0);
                    this.Data.AddInt(0);
                    this.Data.AddInt(0);
                }
            }
            catch (Exception ex)
            {
                Logger.Write($"[Error] AllianceWarDataMessage encode error: {ex.Message}");
            }
        }
    }
}
