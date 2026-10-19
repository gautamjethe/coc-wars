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
                    this.Data.AddLong(c1?.ClanId ?? 0);
                    this.Data.AddString(c1?.ClanName ?? "Clan");
                    this.Data.AddInt(c1?.ClanBadge ?? 0);
                    this.Data.AddInt(c1?.ClanLevel ?? 1);
                    this.Data.Add((byte)0);
                    this.Data.AddRange(new List<byte> { 1, 2, 3, 4 });
                    this.Data.AddInt(c1?.TotalStars ?? 0);
                    this.Data.AddInt((int)(c1?.TotalDestruction ?? 0));
                    this.Data.AddInt(0);
                    this.Data.AddInt(c1?.AttacksUsed ?? 0);
                    this.Data.AddInt(0);

                    // Team 2
                    this.Data.AddLong(c2?.ClanId ?? 0);
                    this.Data.AddString(c2?.ClanName ?? "Enemy");
                    this.Data.AddInt(c2?.ClanBadge ?? 0);
                    this.Data.AddInt(c2?.ClanLevel ?? 1);
                    this.Data.Add((byte)0);
                    this.Data.AddRange(new List<byte> { 1, 2, 3, 4 });
                    this.Data.AddInt(c2?.TotalStars ?? 0);
                    this.Data.AddInt((int)(c2?.TotalDestruction ?? 0));
                    this.Data.AddInt(0);
                    this.Data.AddInt(c2?.AttacksUsed ?? 0);
                    this.Data.AddInt(0);

                    this.Data.AddLong(m_vWar.WarId);

                    this.Data.AddInt(0);
                    this.Data.AddInt(0);

                    this.Data.AddInt(1);
                    this.Data.AddInt(m_vWar.RemainingTimeSeconds);
                    this.Data.AddLong(c1?.ClanId ?? 0);
                    this.Data.AddLong(c1?.ClanId ?? 0);
                    this.Data.AddLong(c2?.ClanId ?? 0);
                    this.Data.AddLong(c2?.ClanId ?? 0);

                    this.Data.AddString(c1?.ClanName ?? "Clan");
                    this.Data.AddString(c2?.ClanName ?? "Enemy");

                    this.Data.AddInt(c1?.ClanLevel ?? 1);
                    this.Data.AddInt(c2?.ClanLevel ?? 1);
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
                    int warState = 0;
                    if (playerClanId > 0 && ClanWarManager.IsClanSearching(playerClanId))
                    {
                        warState = (int)ClanWarState.SEARCHING; // 1
                    }

                    this.Data.AddInt(warState);
                    this.Data.AddInt(0);

                    string clanName = "Clan";
                    int clanBadge = 0;
                    int clanLevel = 1;
                    if (playerClanId > 0)
                    {
                        Alliance a = ObjectManager.GetAlliance(playerClanId);
                        if (a != null)
                        {
                            clanName = a.m_vAllianceName ?? "Clan";
                            clanBadge = a.m_vAllianceBadgeData;
                            clanLevel = a.m_vAllianceLevel;
                        }
                    }

                    this.Data.AddLong(playerClanId > 0 ? playerClanId : 1);
                    this.Data.AddString(clanName);
                    this.Data.AddInt(clanBadge);
                    this.Data.AddInt(clanLevel);
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

                    this.Data.AddInt(clanLevel);
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
