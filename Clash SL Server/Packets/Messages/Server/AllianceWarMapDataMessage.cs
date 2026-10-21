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

            // Clone the member lists so we NEVER mutate the live war object.
            // Mutating war.Clan1.Members corrupts roster for every subsequent login.
            var c1Members = c1.Members != null ? new List<ClanWarMember>(c1.Members) : new List<ClanWarMember>();
            var c2Members = c2.Members != null ? new List<ClanWarMember>(c2.Members) : new List<ClanWarMember>();

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
                int idx = c1Members.FindIndex(m => m.UserId == myUserId);
                if (idx < 0)
                {
                    if (c1Members.Count > 0)
                    {
                        // Clone the first member and replace identity fields only.
                        var replacement = new ClanWarMember
                        {
                            UserId              = myUserId,
                            HomeId              = myHomeId,
                            ClanId              = c1Members[0].ClanId,
                            Name                = myName,
                            TownHallLevel       = myThLevel,
                            MapPosition         = c1Members[0].MapPosition,
                            GoldAvailable       = c1Members[0].GoldAvailable,
                            ElixirAvailable     = c1Members[0].ElixirAvailable,
                            DarkElixirAvailable = c1Members[0].DarkElixirAvailable,
                            OffenceWeight       = c1Members[0].OffenceWeight,
                            DefenceWeight       = c1Members[0].DefenceWeight,
                            ClanCastleLevel     = c1Members[0].ClanCastleLevel,
                            ClanCastleMaxCapacity = c1Members[0].ClanCastleMaxCapacity,
                        };
                        c1Members[0] = replacement;
                    }
                    else
                    {
                        // Roster is empty – add a minimal synthetic member so the lookup succeeds.
                        c1Members.Add(new ClanWarMember
                        {
                            UserId        = myUserId,
                            HomeId        = myHomeId,
                            ClanId        = c1.ClanId,
                            Name          = myName,
                            TownHallLevel = myThLevel,
                            MapPosition   = 1,
                            GoldAvailable     = 100000,
                            ElixirAvailable   = 100000,
                            OffenceWeight     = 500 + myThLevel * 30,
                            DefenceWeight     = 500 + myThLevel * 25,
                            ClanCastleLevel   = (byte)Math.Min(6, Math.Max(1, myThLevel / 2)),
                            ClanCastleMaxCapacity = 30,
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
            this.Data.AddInt(c1Members.Count);
            foreach (var m in c1Members)
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
            this.Data.AddInt(c2Members.Count);
            foreach (var m in c2Members)
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
            // Fields 5-7: ReadLong x3 (vtable[0x6c])
            this.Data.AddLong(m.ClanId);
            this.Data.AddLong(m.UserId);
            this.Data.AddLong(m.HomeId);

            // Field 8: ReadString (vtable[0x48])
            this.Data.AddString(m.Name ?? "Player");

            // Fields 9-25: 17× ReadInt32 (vtable[0x54])
            this.Data.AddInt(m.StarsConceded);       // 9
            this.Data.AddInt(m.DamageConceded);      // 10
            this.Data.AddInt(0);                     // 11
            this.Data.AddInt(m.AttacksUsed);         // 12
            this.Data.AddInt(m.GotAttackedCount);    // 13
            this.Data.AddInt(m.GoldGain);            // 14
            this.Data.AddInt(m.ElixirGain);          // 15
            this.Data.AddInt(m.DarkElixirGain);      // 16
            this.Data.AddInt(m.GoldAvailable);       // 17
            this.Data.AddInt(m.ElixirAvailable);     // 18
            this.Data.AddInt(m.DarkElixirAvailable); // 19
            this.Data.AddInt(0);                     // 20
            this.Data.AddInt(m.OffenceWeight);       // 21
            this.Data.AddInt(m.DefenceWeight);       // 22
            this.Data.AddInt(1);                     // 23 (has hero or troop data)
            this.Data.AddInt(Math.Max(0, m.TownHallLevel - 1)); // 24 (TH level 0-based)
            this.Data.AddInt(m.MapPosition);         // 25 (map slot)

            // Field 26: ReadBool (vtable[0x50]) - clan castle flag
            this.Data.Add((byte)1);

            // Field 27 (if flag=1): ReadString (vtable[0x48]) - CC message
            this.Data.AddString(m.ClanCastleMessage ?? "");

            // Fields 28-31: 4× ReadInt32 - CC capacity data
            int ccCapacity = m.ClanCastleMaxCapacity > 0 ? m.ClanCastleMaxCapacity : 30;
            int ccUsed = 0;
            try { ccUsed = m.GetClanCastleUsedCapacity(); } catch { }
            this.Data.AddInt(ccCapacity);  // 28 [r8+0x40]
            this.Data.AddInt(ccUsed);      // 29 [r8+0x90]
            this.Data.AddInt(0);           // 30 [r8+0x94]
            this.Data.AddInt(0);           // 31 [r8+0xa0]

            // Field 32: ReadBool (vtable[0x50]) - optional long 1 flag
            this.Data.Add((byte)1);
            // Field 33 (if flag=1): ReadLong (vtable[0x6c]) - war ID
            this.Data.AddLong(war.WarId);

            // Field 34: ReadBool (vtable[0x50]) - optional long 2 flag
            this.Data.Add((byte)0);        // no second optional long

            // Fields after optional longs (always present):
            // [r8+0xa8]: ReadBool - additional flag
            this.Data.Add((byte)0);

            // [r8+0x88]: ReadInt32 - CC level
            this.Data.AddInt(m.ClanCastleLevel);
            // [r8+0xac]: ReadInt32
            this.Data.AddInt(0);
            // [r8+0xb0]: ReadInt32
            this.Data.AddInt(0);

            // CRITICAL: client reads vtable[0x44] = ReadLong here (NOT a string!)
            // Previous bug: AddString("") wrote 4 bytes, client read 8 → 4-byte shift → SIGSEGV
            this.Data.AddLong(0);          // [r8+4] = supertroop/extra data long

            // Attacks count + attack entries (ReadInt32, then N × ReadLong pairs)
            this.Data.AddInt(0);           // 0 attacks = no attack entries follow
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