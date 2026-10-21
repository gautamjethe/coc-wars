using System;
using System.Collections.Generic;
using System.Linq;
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
            if (war.Clan1 == null || war.Clan2 == null)
            {
                EncodeDefaultWarMap();
                return;
            }

            // War State written after clan swap + stateToEncode calculation below.
            // (actual AddInt call is deferred - see below after stateToEncode is determined)

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

            // libg.so 0x000b7508: cmp r0, #3 / ble #0xb78da
            // When warState > 3 in village mode it allocates TID_ALLIANCE_WAR_EVENT_WAR_STARTED banner.
            // Banner tick at 0xe2f24 does getMemberByPlayerId(myId) -> crash if player not found.
            // Fix: cap stateToEncode at 3 in village mode so banner is never created.
            int stateToEncode = (int)war.State;
            if (this.Device?.PlayerState != Logic.Enums.State.WAR_EMODE && stateToEncode > 3)
                stateToEncode = 3;

            if (c1.Members == null) c1.Members = new List<ClanWarMember>();
            if (c2.Members == null) c2.Members = new List<ClanWarMember>();

            long myUserId  = this.Device?.Player?.Avatar?.UserId ?? 0;
            long myHomeId  = this.Device?.Player?.Avatar?.CurrentHomeId ?? myUserId;
            string myName  = this.Device?.Player?.Avatar?.AvatarName ?? "Player";
            int myThLevel  = (this.Device?.Player?.Avatar?.m_vTownHallLevel > 0)
                             ? this.Device.Player.Avatar.m_vTownHallLevel : 9;

            // CRITICAL: libg.so 0xe2f22 calls getMemberByPlayerId(myUserId) and immediately
            // dereferences the result. If the player is absent the return value is NULL -> SIGSEGV.
            // We MUST guarantee the current player appears in c1.Members.
            if (myUserId > 0)
            {
                int idx = c1.Members.FindIndex(m => m.UserId == myUserId);
                if (idx < 0)
                {
                    if (c1.Members.Count > 0)
                    {
                        // Replace first member's identity so the list length stays correct.
                        c1.Members[0].UserId   = myUserId;
                        c1.Members[0].HomeId   = myHomeId;
                        c1.Members[0].Name     = myName;
                        c1.Members[0].TownHallLevel = myThLevel;
                        c1.Members[0].MapPosition   = 1;
                    }
                    else
                    {
                        // Roster is empty - add a minimal synthetic member so the lookup succeeds.
                        c1.Members.Add(new ClanWarMember
                        {
                            UserId        = myUserId,
                            HomeId        = myHomeId,
                            ClanId        = c1.ClanId,
                            Name          = myName,
                            TownHallLevel = myThLevel,
                            MapPosition   = 1,
                        });
                    }
                }
            }

            // Now write war state (capped at 3 in village mode) and remaining time
            this.Data.AddInt(stateToEncode);
            this.Data.AddInt(war.RemainingTimeSeconds);

            // Clan 1 Header
            this.Data.AddLong(c1.ClanId);
            this.Data.AddString(c1.ClanName ?? "Clan");
            this.Data.AddInt(c1.ClanBadge);
            this.Data.AddInt(c1.ClanLevel);
            this.Data.AddInt(c1.TotalStars);

            // Clan 1 Members
            this.Data.AddInt(c1.Members.Count);
            foreach (var m in c1.Members)
            {
                EncodeMember(war, m);
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
            this.Data.AddString(c2.ClanName ?? "Enemy Clan");
            this.Data.AddInt(c2.ClanBadge);
            this.Data.AddInt(c2.ClanLevel);
            this.Data.AddInt(c2.TotalStars);

            // Clan 2 Members
            this.Data.AddInt(c2.Members.Count);
            foreach (var m in c2.Members)
            {
                EncodeMember(war, m);
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
            int ccCapacity = m.ClanCastleMaxCapacity > 0 ? m.ClanCastleMaxCapacity : 30;
            int ccUsed = 0;
            try { ccUsed = m.GetClanCastleUsedCapacity(); } catch { }

            this.Data.AddInt(ccCapacity);
            this.Data.AddInt(ccUsed);
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