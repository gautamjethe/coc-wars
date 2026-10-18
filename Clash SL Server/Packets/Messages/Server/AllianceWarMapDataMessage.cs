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
            this.Data.AddInt(c1.Members.Count);

            // Clan 1 Members
            foreach (var m in c1.Members)
            {
                EncodeMember(war, m);
            }

            // Separator between Clan 1 and Clan 2
            this.Data.Add((byte)3);
            this.Data.AddInt(-1);
            this.Data.AddInt(1);
            this.Data.AddInt(war.WarSize);
            this.Data.AddInt(25);
            this.Data.AddInt(50);
            this.Data.AddInt(40);
            this.Data.AddInt(60);
            this.Data.Add((byte)1);

            // Clan 2 Header
            this.Data.AddLong(c2.ClanId);
            this.Data.AddString(c2.ClanName);
            this.Data.AddInt(c2.ClanBadge);
            this.Data.AddInt(c2.ClanLevel);
            this.Data.AddInt(c2.TotalStars);
            this.Data.AddInt(c2.Members.Count);

            // Clan 2 Members
            foreach (var m in c2.Members)
            {
                EncodeMember(war, m);
            }

            // War Map Tail
            this.Data.Add((byte)3);
            this.Data.AddInt(-1);
            this.Data.AddInt(1);
            this.Data.AddInt(war.WarSize);
            this.Data.AddInt(25);
            this.Data.AddInt(50);
            this.Data.AddInt(40);
            this.Data.AddInt(60);
            this.Data.Add((byte)1);
            this.Data.AddLong(war.WarId);
            this.Data.Add((byte)0);
            this.Data.Add((byte)1);
            this.Data.AddLong(c2.ClanId);

            // Attack Logs
            this.Data.AddInt(war.AttackLogs.Count);
            foreach (var a in war.AttackLogs)
            {
                this.Data.AddLong(a.AttackerClanId);
                this.Data.AddLong(a.AttackerUserId);
                this.Data.AddLong(a.DefenderClanId);
                this.Data.AddLong(a.DefenderUserId);
                this.Data.AddString(a.AttackerName ?? "Attacker");
                this.Data.AddString(a.DefenderName ?? "Defender");
                this.Data.AddInt(a.Stars);
                this.Data.AddInt(a.Order);
                this.Data.AddInt(a.DestructionPercentage);
                this.Data.AddInt(a.Stars);
                this.Data.AddInt(8);
                this.Data.AddInt(a.Duration > 0 ? a.Duration : 120);
                this.Data.AddInt(6);
                this.Data.Add((byte)1);
                this.Data.AddLong(a.ReplayId);
            }
        }

        private void EncodeMember(ClanWar war, ClanWarMember m)
        {
            this.Data.AddLong(m.ClanId);
            this.Data.AddLong(m.UserId);
            this.Data.AddLong(m.HomeId);
            this.Data.AddString(m.Name ?? "Player");

            this.Data.AddInt(m.StarsConceded);
            this.Data.AddInt(m.DamageConceded);
            this.Data.AddInt(0); // Unknown 1
            this.Data.AddInt(m.AttacksUsed);
            this.Data.AddInt(m.GotAttackedCount);
            this.Data.AddInt(m.GoldGain);
            this.Data.AddInt(m.ElixirGain);
            this.Data.AddInt(m.DarkElixirGain);
            this.Data.AddInt(m.GoldAvailable);
            this.Data.AddInt(m.ElixirAvailable);
            this.Data.AddInt(m.DarkElixirAvailable);
            this.Data.AddInt(0); // Unknown 2
            this.Data.AddInt(m.OffenceWeight);
            this.Data.AddInt(m.DefenceWeight);
            this.Data.AddInt(1); // Unknown 3
            this.Data.AddInt(Math.Max(0, m.TownHallLevel - 1));
            this.Data.AddInt(m.MapPosition);
            this.Data.Add((byte)Math.Max(0, 2 - m.AttacksUsed)); // Attacks remaining (2, 1, or 0)
            this.Data.AddLong(war.WarId);
            this.Data.AddLong(0L); // Unknown 64-bit
            this.Data.Add(m.ClanCastleLevel);
            this.Data.AddInt(m.ClanCastleMaxCapacity);
            this.Data.AddString(m.ClanCastleMessage ?? "");
            this.Data.AddInt(m.GetClanCastleUsedCapacity());

            this.Data.AddInt(m.Donations.Count);
            foreach (var d in m.Donations)
            {
                this.Data.AddLong(d.DonorId);
                this.Data.AddInt(d.TroopCount);
                this.Data.AddInt(d.TroopDataId);
            }

            this.Data.AddInt(m.AttacksConceded.Count);
            foreach (var a in m.AttacksConceded)
            {
                this.Data.AddLong(a.AttackerClanId);
                this.Data.AddLong(a.AttackerUserId);
                this.Data.AddLong(a.DefenderClanId);
                this.Data.AddLong(a.DefenderUserId);
                this.Data.AddString(a.AttackerName ?? "Attacker");
                this.Data.AddString(a.DefenderName ?? "Defender");
                this.Data.AddInt(a.Stars);
                this.Data.AddInt(a.Order);
                this.Data.AddInt(a.DestructionPercentage);
                this.Data.AddInt(a.Stars);
                this.Data.AddInt(8);
                this.Data.AddInt(a.Duration > 0 ? a.Duration : 120);
                this.Data.AddInt(6);
                this.Data.Add((byte)1);
                this.Data.AddLong(a.ReplayId);
            }
        }

        private void EncodeDefaultWarMap()
        {
            // Empty / default state when clan is not currently in a war
            this.Data.AddInt((int)ClanWarState.NOT_IN_WAR);
            this.Data.AddInt(0);

            long clanId = this.Device?.Player?.Avatar?.AllianceId ?? 1;
            string clanName = "Clan";
            Alliance a = ObjectManager.GetAlliance(clanId);
            if (a != null)
            {
                clanName = a.m_vAllianceName;
            }

            this.Data.AddLong(clanId);
            this.Data.AddString(clanName);
            this.Data.AddInt(0);
            this.Data.AddInt(1);
            this.Data.AddInt(0);
            this.Data.AddInt(0); // 0 members

            // Separator
            this.Data.Add((byte)3);
            this.Data.AddInt(-1);
            this.Data.AddInt(1);
            this.Data.AddInt(0);
            this.Data.AddInt(25);
            this.Data.AddInt(50);
            this.Data.AddInt(40);
            this.Data.AddInt(60);
            this.Data.Add((byte)1);

            // Clan 2 Header
            this.Data.AddLong(0);
            this.Data.AddString("Enemy Clan");
            this.Data.AddInt(0);
            this.Data.AddInt(1);
            this.Data.AddInt(0);
            this.Data.AddInt(0);

            // Tail
            this.Data.Add((byte)3);
            this.Data.AddInt(-1);
            this.Data.AddInt(1);
            this.Data.AddInt(0);
            this.Data.AddInt(25);
            this.Data.AddInt(50);
            this.Data.AddInt(40);
            this.Data.AddInt(60);
            this.Data.Add((byte)1);
            this.Data.AddLong(0);
            this.Data.Add((byte)0);
            this.Data.Add((byte)1);
            this.Data.AddLong(0);
            this.Data.AddInt(0); // 0 attacks
        }
    }
}