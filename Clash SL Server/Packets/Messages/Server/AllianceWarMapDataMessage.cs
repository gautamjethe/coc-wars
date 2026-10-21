using System;
using System.Collections.Generic;
using CSS.Core;
using CSS.Helpers.List;
using CSS.Logic;

namespace CSS.Packets.Messages.Server
{
    // Packet 24335
    internal class AllianceWarMapDataMessage : Message
    {
        private ClanWar m_vWar;

        public AllianceWarMapDataMessage(Device client, ClanWar war = null) : base(client)
        {
            this.Identifier = 24335;
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
                    EncodeWarMap(m_vWar);
                }
                else
                {
                    EncodeDefaultWarMap();
                }
            }
            catch (Exception ex)
            {
                Logger.Write($"[Error] AllianceWarMapDataMessage encode error: {ex.Message}");
            }
        }

        private void EncodeWarMap(ClanWar war)
        {
            // War State: 4 = Preparation Day, 5 = Battle Day, 6 = War Ended
            this.Data.AddInt((int)war.State);
            this.Data.AddInt(war.RemainingTimeSeconds);

            // Determine which clan is our player's clan (Clan 1) and enemy clan (Clan 2)
            long playerClanId = this.Device?.Player?.Avatar?.AllianceId ?? 0;
            WarClan c1 = war.Clan1;
            WarClan c2 = war.Clan2;

            if (playerClanId > 0 && war.Clan2 != null && war.Clan2.ClanId == playerClanId)
            {
                // Swap so player's clan is always Clan 1 in their perspective
                c1 = war.Clan2;
                c2 = war.Clan1;
            }

            // Clan 1 Header
            this.Data.AddLong(c1.ClanId);
            this.Data.AddString(c1.ClanName);
            this.Data.AddInt(c1.ClanBadge);
            this.Data.AddInt(c1.ClanLevel);
            this.Data.AddInt(c1.TotalStars);
            bool isWarMode = this.Device?.PlayerState == Logic.Enums.State.WAR_EMODE;

            // Clan 1 Members
            if (isWarMode)
            {
                this.Data.AddInt(c1.Members.Count);
                foreach (var m in c1.Members)
                {
                    EncodeMember(war, m);
                }
            }
            else
            {
                this.Data.AddInt(0);
            }

            // Clan 1 Tail
            this.Data.Add((byte)1);
            this.Data.Add((byte)1);
            this.Data.AddInt(-1);
            this.Data.AddInt(1);
            this.Data.AddInt(war.WarSize);
            this.Data.AddInt(25);
            this.Data.AddInt(50);
            this.Data.AddInt(40);
            this.Data.AddInt(60);

            // Clan 2 Boolean
            this.Data.Add((byte)1);

            // Clan 2 Header
            this.Data.AddLong(c2.ClanId);
            this.Data.AddString(c2.ClanName);
            this.Data.AddInt(c2.ClanBadge);
            this.Data.AddInt(c2.ClanLevel);
            this.Data.AddInt(c2.TotalStars);

            // Clan 2 Members
            if (isWarMode)
            {
                this.Data.AddInt(c2.Members.Count);
                foreach (var m in c2.Members)
                {
                    EncodeMember(war, m);
                }
            }
            else
            {
                this.Data.AddInt(0);
            }

            // Clan 2 Tail
            this.Data.Add((byte)1);
            this.Data.Add((byte)1);
            this.Data.AddInt(-1);
            this.Data.AddInt(1);
            this.Data.AddInt(war.WarSize);
            this.Data.AddInt(25);
            this.Data.AddInt(50);
            this.Data.AddInt(40);
            this.Data.AddInt(60);

            // War Map Tail
            this.Data.Add((byte)1);
            this.Data.AddLong(war.WarId);
            this.Data.AddInt(0);
            this.Data.Add((byte)1);
            this.Data.AddLong(c2.ClanId);
            this.Data.AddInt(0); // Attack logs count
        }

        private void EncodeMember(ClanWar war, ClanWarMember m)
        {
            this.Data.AddLong(m.ClanId);
            this.Data.AddLong(m.UserId);
            this.Data.AddLong(m.HomeId);
            this.Data.AddString(m.Name ?? "Player");

            this.Data.AddInt(m.StarsConceded);
            this.Data.AddInt(m.DamageConceded);
            this.Data.AddInt(0);
            this.Data.AddInt(m.AttacksUsed);
            this.Data.AddInt(m.GotAttackedCount);
            this.Data.AddInt(m.GoldGain);
            this.Data.AddInt(m.ElixirGain);
            this.Data.AddInt(m.DarkElixirGain);
            this.Data.AddInt(m.GoldAvailable);
            this.Data.AddInt(m.ElixirAvailable);
            this.Data.AddInt(m.DarkElixirAvailable);
            this.Data.AddInt(0);
            this.Data.AddInt(m.OffenceWeight);
            this.Data.AddInt(m.DefenceWeight);
            this.Data.AddInt(1);
            this.Data.AddInt(Math.Max(0, m.TownHallLevel - 1));
            this.Data.AddInt(m.MapPosition);

            // Clan Castle data flag (byte_0x8c)
            this.Data.Add((byte)1);
            this.Data.AddString(m.ClanCastleMessage ?? "");
            this.Data.AddInt(m.ClanCastleMaxCapacity);
            this.Data.AddInt(m.GetClanCastleUsedCapacity());
            this.Data.AddInt(0);
            this.Data.AddInt(0);

            // WarId
            this.Data.Add((byte)1);
            this.Data.AddLong(war.WarId);

            // Optional Long 1
            this.Data.Add((byte)0);

            // Optional Long 2
            this.Data.Add((byte)0);

            // Trailing fields
            this.Data.AddInt(m.ClanCastleLevel);
            this.Data.AddInt(0);
            this.Data.AddInt(0);
            this.Data.AddString("");
            this.Data.AddInt(0);

            // Attacks conceded count
            this.Data.AddInt(0);
        }

        private void EncodeDefaultWarMap()
        {
            long clanId = this.Device?.Player?.Avatar?.AllianceId ?? 1;
            bool isSearching = clanId > 0 && ClanWarManager.IsClanSearching(clanId);
            int warState = isSearching ? (int)ClanWarState.SEARCHING : (int)ClanWarState.NOT_IN_WAR;
            int searchDuration = 0;
            if (isSearching && ClanWarManager.SearchingClans.TryGetValue(clanId, out var info))
            {
                searchDuration = Math.Max(0, (int)(DateTime.UtcNow - info.SearchStartTime).TotalSeconds);
            }

            this.Data.AddInt(warState);
            this.Data.AddInt(searchDuration);

            string clanName = "Clan";
            Alliance a = ObjectManager.GetAlliance(clanId);
            if (a != null)
            {
                clanName = a.m_vAllianceName;
            }

            this.Data.AddLong(clanId);
            this.Data.AddString(clanName);
            this.Data.AddInt(a != null ? a.m_vAllianceBadgeData : 0);
            this.Data.AddInt(a != null ? a.m_vAllianceLevel : 1);
            this.Data.AddInt(0);
            this.Data.AddInt(0); // 0 members

            // Clan 1 Tail
            this.Data.Add((byte)1);
            this.Data.Add((byte)1);
            this.Data.AddInt(-1);
            this.Data.AddInt(1);
            this.Data.AddInt(0);
            this.Data.AddInt(25);
            this.Data.AddInt(50);
            this.Data.AddInt(40);
            this.Data.AddInt(60);

            // Clan 2 Boolean: 0 (No enemy clan)
            this.Data.Add((byte)0);

            // WarId Boolean: 0 (No active war id)
            this.Data.Add((byte)0);

            // Unknown Int
            this.Data.AddInt(0);

            // Enemy ClanId Boolean: 0
            this.Data.Add((byte)0);

            // Attack logs count: 0
            this.Data.AddInt(0);
        }
    }
}