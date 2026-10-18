using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using CSS.Core;
using CSS.Core.Network;
using CSS.Files.Logic;
using CSS.Helpers.List;
using CSS.Logic.Enums;
using CSS.Logic.StreamEntry;
using CSS.Packets.Messages.Server;

namespace CSS.Logic
{
    public enum ClanWarState
    {
        NOT_IN_WAR = 0,
        SEARCHING = 1,
        WAR_FOUND = 2,
        PREPARATION_DAY = 4,
        BATTLE_DAY = 5,
        WAR_ENDED = 6
    }

    public class ActiveWarBattleSession
    {
        public long WarId { get; set; }
        public long AttackerUserId { get; set; }
        public long DefenderUserId { get; set; }
        public DateTime StartTime { get; set; }
        public int HousingSpaceDeployed { get; set; }
        public int TotalHousingSpace { get; set; }
        public int AttackerTownHall { get; set; }
        public int DefenderTownHall { get; set; }
    }

    public class WarDonationEntry
    {
        public long DonorId { get; set; }
        public int TroopCount { get; set; }
        public int TroopDataId { get; set; }
        public int TroopLevel { get; set; }

        public WarDonationEntry() { }

        public WarDonationEntry(long donorId, int count, int dataId, int level)
        {
            DonorId = donorId;
            TroopCount = count;
            TroopDataId = dataId;
            TroopLevel = level;
        }

        public JObject Save()
        {
            return new JObject
            {
                { "donor_id", DonorId },
                { "count", TroopCount },
                { "data_id", TroopDataId },
                { "level", TroopLevel }
            };
        }

        public void Load(JObject json)
        {
            DonorId = json["donor_id"]?.ToObject<long>() ?? 0;
            TroopCount = json["count"]?.ToObject<int>() ?? 0;
            TroopDataId = json["data_id"]?.ToObject<int>() ?? 0;
            TroopLevel = json["level"]?.ToObject<int>() ?? 0;
        }
    }

    public class WarAttackEntry
    {
        public int Order { get; set; }
        public long AttackerClanId { get; set; }
        public long AttackerUserId { get; set; }
        public string AttackerName { get; set; }
        public long DefenderClanId { get; set; }
        public long DefenderUserId { get; set; }
        public string DefenderName { get; set; }
        public int DefenderMapPosition { get; set; }
        public int Stars { get; set; }
        public int DestructionPercentage { get; set; }
        public int Duration { get; set; }
        public long Timestamp { get; set; }
        public long ReplayId { get; set; }

        public WarAttackEntry()
        {
            Timestamp = (long)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
            ReplayId = DateTime.UtcNow.Ticks;
        }

        public JObject Save()
        {
            return new JObject
            {
                { "order", Order },
                { "attacker_clan_id", AttackerClanId },
                { "attacker_user_id", AttackerUserId },
                { "attacker_name", AttackerName ?? "" },
                { "defender_clan_id", DefenderClanId },
                { "defender_user_id", DefenderUserId },
                { "defender_name", DefenderName ?? "" },
                { "defender_pos", DefenderMapPosition },
                { "stars", Stars },
                { "destruction", DestructionPercentage },
                { "duration", Duration },
                { "timestamp", Timestamp },
                { "replay_id", ReplayId }
            };
        }

        public void Load(JObject json)
        {
            Order = json["order"]?.ToObject<int>() ?? 0;
            AttackerClanId = json["attacker_clan_id"]?.ToObject<long>() ?? 0;
            AttackerUserId = json["attacker_user_id"]?.ToObject<long>() ?? 0;
            AttackerName = json["attacker_name"]?.ToObject<string>() ?? "";
            DefenderClanId = json["defender_clan_id"]?.ToObject<long>() ?? 0;
            DefenderUserId = json["defender_user_id"]?.ToObject<long>() ?? 0;
            DefenderName = json["defender_name"]?.ToObject<string>() ?? "";
            DefenderMapPosition = json["defender_pos"]?.ToObject<int>() ?? 0;
            Stars = json["stars"]?.ToObject<int>() ?? 0;
            DestructionPercentage = json["destruction"]?.ToObject<int>() ?? 0;
            Duration = json["duration"]?.ToObject<int>() ?? 0;
            Timestamp = json["timestamp"]?.ToObject<long>() ?? 0;
            ReplayId = json["replay_id"]?.ToObject<long>() ?? 0;
        }
    }

    public class ClanWarMember
    {
        public long UserId { get; set; }
        public long HomeId { get; set; }
        public long ClanId { get; set; }
        public string Name { get; set; }
        public int MapPosition { get; set; }
        public int TownHallLevel { get; set; }
        public int StarsConceded { get; set; }
        public int DamageConceded { get; set; }
        public int AttacksUsed { get; set; }
        public int GotAttackedCount { get; set; }
        public int GoldGain { get; set; }
        public int ElixirGain { get; set; }
        public int DarkElixirGain { get; set; }
        public int GoldAvailable { get; set; }
        public int ElixirAvailable { get; set; }
        public int DarkElixirAvailable { get; set; }
        public int OffenceWeight { get; set; }
        public int DefenceWeight { get; set; }
        public byte ClanCastleLevel { get; set; }
        public int ClanCastleMaxCapacity { get; set; }
        public string ClanCastleMessage { get; set; }
        public List<WarDonationEntry> Donations { get; set; }
        public List<WarAttackEntry> AttacksConceded { get; set; }
        public string VillageJson { get; set; }

        public ClanWarMember()
        {
            Donations = new List<WarDonationEntry>();
            AttacksConceded = new List<WarAttackEntry>();
            ClanCastleMessage = "";
            ClanCastleMaxCapacity = 30;
            ClanCastleLevel = 4;
            TownHallLevel = 9;
        }

        internal ClanWarMember(ClientAvatar avatar, int position, long clanId)
        {
            UserId = avatar.UserId;
            HomeId = avatar.CurrentHomeId > 0 ? avatar.CurrentHomeId : avatar.UserId;
            ClanId = clanId;
            Name = avatar.AvatarName ?? "Player";
            MapPosition = position;
            TownHallLevel = avatar.m_vTownHallLevel > 0 ? avatar.m_vTownHallLevel : 9;
            OffenceWeight = 500 + TownHallLevel * 30;
            DefenceWeight = 500 + TownHallLevel * 25;
            ClanCastleLevel = (byte)Math.Min(6, Math.Max(1, TownHallLevel / 2));
            ClanCastleMaxCapacity = 10 + ClanCastleLevel * 5;
            ClanCastleMessage = "";
            GoldAvailable = 100000 + TownHallLevel * 15000;
            ElixirAvailable = 100000 + TownHallLevel * 15000;
            DarkElixirAvailable = TownHallLevel >= 7 ? 500 + TownHallLevel * 80 : 0;
            Donations = new List<WarDonationEntry>();
            AttacksConceded = new List<WarAttackEntry>();
        }

        public int GetClanCastleUsedCapacity()
        {
            int used = 0;
            foreach (var d in Donations)
            {
                Data data = CSVManager.DataTables.GetDataById(d.TroopDataId);
                if (data is CombatItemData cid)
                {
                    used += cid.GetHousingSpace() * d.TroopCount;
                }
                else
                {
                    used += d.TroopCount;
                }
            }
            return used;
        }

        public bool AddDonation(long donorId, int troopDataId, int count, int level)
        {
            Data data = CSVManager.DataTables.GetDataById(troopDataId);
            int spacePerTroop = 1;
            if (data is CombatItemData cid)
            {
                spacePerTroop = cid.GetHousingSpace();
            }

            int needed = spacePerTroop * count;
            if (GetClanCastleUsedCapacity() + needed > ClanCastleMaxCapacity)
            {
                return false;
            }

            var existing = Donations.FirstOrDefault(d => d.DonorId == donorId && d.TroopDataId == troopDataId && d.TroopLevel == level);
            if (existing != null)
            {
                existing.TroopCount += count;
            }
            else
            {
                Donations.Add(new WarDonationEntry(donorId, count, troopDataId, level));
            }
            return true;
        }

        public JObject Save()
        {
            var json = new JObject
            {
                { "user_id", UserId },
                { "home_id", HomeId },
                { "clan_id", ClanId },
                { "name", Name ?? "" },
                { "map_pos", MapPosition },
                { "th_lvl", TownHallLevel },
                { "stars_conceded", StarsConceded },
                { "damage_conceded", DamageConceded },
                { "attacks_used", AttacksUsed },
                { "got_attacked", GotAttackedCount },
                { "gold_gain", GoldGain },
                { "elixir_gain", ElixirGain },
                { "delixir_gain", DarkElixirGain },
                { "gold_avail", GoldAvailable },
                { "elixir_avail", ElixirAvailable },
                { "delixir_avail", DarkElixirAvailable },
                { "off_weight", OffenceWeight },
                { "def_weight", DefenceWeight },
                { "cc_lvl", (int)ClanCastleLevel },
                { "cc_cap", ClanCastleMaxCapacity },
                { "cc_msg", ClanCastleMessage ?? "" },
                { "village", VillageJson ?? "" }
            };

            var donArray = new JArray();
            foreach (var d in Donations)
            {
                donArray.Add(d.Save());
            }
            json.Add("donations", donArray);

            var atkArray = new JArray();
            foreach (var a in AttacksConceded)
            {
                atkArray.Add(a.Save());
            }
            json.Add("attacks_conceded", atkArray);

            return json;
        }

        public void Load(JObject json)
        {
            UserId = json["user_id"]?.ToObject<long>() ?? 0;
            HomeId = json["home_id"]?.ToObject<long>() ?? 0;
            ClanId = json["clan_id"]?.ToObject<long>() ?? 0;
            Name = json["name"]?.ToObject<string>() ?? "";
            MapPosition = json["map_pos"]?.ToObject<int>() ?? 0;
            TownHallLevel = json["th_lvl"]?.ToObject<int>() ?? 9;
            StarsConceded = json["stars_conceded"]?.ToObject<int>() ?? 0;
            DamageConceded = json["damage_conceded"]?.ToObject<int>() ?? 0;
            AttacksUsed = json["attacks_used"]?.ToObject<int>() ?? 0;
            GotAttackedCount = json["got_attacked"]?.ToObject<int>() ?? 0;
            GoldGain = json["gold_gain"]?.ToObject<int>() ?? 0;
            ElixirGain = json["elixir_gain"]?.ToObject<int>() ?? 0;
            DarkElixirGain = json["delixir_gain"]?.ToObject<int>() ?? 0;
            GoldAvailable = json["gold_avail"]?.ToObject<int>() ?? 0;
            ElixirAvailable = json["elixir_avail"]?.ToObject<int>() ?? 0;
            DarkElixirAvailable = json["delixir_avail"]?.ToObject<int>() ?? 0;
            OffenceWeight = json["off_weight"]?.ToObject<int>() ?? 0;
            DefenceWeight = json["def_weight"]?.ToObject<int>() ?? 0;
            ClanCastleLevel = (byte)(json["cc_lvl"]?.ToObject<int>() ?? 4);
            ClanCastleMaxCapacity = json["cc_cap"]?.ToObject<int>() ?? 30;
            ClanCastleMessage = json["cc_msg"]?.ToObject<string>() ?? "";
            VillageJson = json["village"]?.ToObject<string>() ?? "";

            Donations = new List<WarDonationEntry>();
            if (json["donations"] is JArray donArray)
            {
                foreach (JObject item in donArray)
                {
                    var don = new WarDonationEntry();
                    don.Load(item);
                    Donations.Add(don);
                }
            }

            AttacksConceded = new List<WarAttackEntry>();
            if (json["attacks_conceded"] is JArray atkArray)
            {
                foreach (JObject item in atkArray)
                {
                    var atk = new WarAttackEntry();
                    atk.Load(item);
                    AttacksConceded.Add(atk);
                }
            }
        }
    }

    public class WarClan
    {
        public long ClanId { get; set; }
        public string ClanName { get; set; }
        public int ClanBadge { get; set; }
        public int ClanLevel { get; set; }
        public List<ClanWarMember> Members { get; set; }
        public int TotalStars { get; set; }
        public double TotalDestruction { get; set; }
        public int AttacksUsed { get; set; }

        public WarClan()
        {
            Members = new List<ClanWarMember>();
            ClanName = "Clan";
            ClanLevel = 1;
        }

        internal WarClan(Alliance alliance)
        {
            ClanId = alliance.m_vAllianceId;
            ClanName = alliance.m_vAllianceName;
            ClanBadge = alliance.m_vAllianceBadgeData;
            ClanLevel = alliance.m_vAllianceLevel;
            Members = new List<ClanWarMember>();
        }

        public ClanWarMember GetMember(long userId) => Members.FirstOrDefault(m => m.UserId == userId);

        public void RecalculateScoreAgainst(WarClan enemyClan)
        {
            int stars = 0;
            double totalDmg = 0;
            int enemyCount = enemyClan.Members.Count;

            foreach (var enemy in enemyClan.Members)
            {
                stars += enemy.StarsConceded;
                totalDmg += enemy.DamageConceded;
            }

            TotalStars = stars;
            TotalDestruction = enemyCount > 0 ? (totalDmg / enemyCount) : 0;

            int atks = 0;
            foreach (var m in Members)
            {
                atks += m.AttacksUsed;
            }
            AttacksUsed = atks;
        }

        public JObject Save()
        {
            var json = new JObject
            {
                { "clan_id", ClanId },
                { "clan_name", ClanName ?? "" },
                { "clan_badge", ClanBadge },
                { "clan_level", ClanLevel },
                { "total_stars", TotalStars },
                { "total_destruction", TotalDestruction },
                { "attacks_used", AttacksUsed }
            };

            var membersArray = new JArray();
            foreach (var m in Members)
            {
                membersArray.Add(m.Save());
            }
            json.Add("members", membersArray);
            return json;
        }

        public void Load(JObject json)
        {
            ClanId = json["clan_id"]?.ToObject<long>() ?? 0;
            ClanName = json["clan_name"]?.ToObject<string>() ?? "";
            ClanBadge = json["clan_badge"]?.ToObject<int>() ?? 0;
            ClanLevel = json["clan_level"]?.ToObject<int>() ?? 1;
            TotalStars = json["total_stars"]?.ToObject<int>() ?? 0;
            TotalDestruction = json["total_destruction"]?.ToObject<double>() ?? 0.0;
            AttacksUsed = json["attacks_used"]?.ToObject<int>() ?? 0;

            Members = new List<ClanWarMember>();
            if (json["members"] is JArray arr)
            {
                foreach (JObject item in arr)
                {
                    var m = new ClanWarMember();
                    m.Load(item);
                    Members.Add(m);
                }
            }
        }
    }

    public class ClanWar
    {
        public long WarId { get; set; }
        public ClanWarState State { get; set; }
        public WarClan Clan1 { get; set; }
        public WarClan Clan2 { get; set; }
        public int WarSize { get; set; }
        public DateTime PreparationStartTime { get; set; }
        public DateTime PreparationEndTime { get; set; }
        public DateTime BattleStartTime { get; set; }
        public DateTime BattleEndTime { get; set; }
        public DateTime WarEndedTime { get; set; }
        public List<WarAttackEntry> AttackLogs { get; set; }

        public ClanWar()
        {
            AttackLogs = new List<WarAttackEntry>();
            Clan1 = new WarClan();
            Clan2 = new WarClan();
            State = ClanWarState.NOT_IN_WAR;
        }

        public int RemainingTimeSeconds
        {
            get
            {
                DateTime now = DateTime.UtcNow;
                if (State == ClanWarState.PREPARATION_DAY)
                {
                    double diff = (PreparationEndTime - now).TotalSeconds;
                    return diff > 0 ? (int)diff : 0;
                }
                if (State == ClanWarState.BATTLE_DAY)
                {
                    double diff = (BattleEndTime - now).TotalSeconds;
                    return diff > 0 ? (int)diff : 0;
                }
                return 0;
            }
        }

        public WarClan GetClan(long clanId)
        {
            if (Clan1 != null && Clan1.ClanId == clanId) return Clan1;
            if (Clan2 != null && Clan2.ClanId == clanId) return Clan2;
            return null;
        }

        public WarClan GetOpponentClan(long clanId)
        {
            if (Clan1 != null && Clan1.ClanId == clanId) return Clan2;
            if (Clan2 != null && Clan2.ClanId == clanId) return Clan1;
            return null;
        }

        public ClanWarMember GetMember(long userId)
        {
            var m1 = Clan1?.GetMember(userId);
            if (m1 != null) return m1;
            return Clan2?.GetMember(userId);
        }

        public bool RecordAttack(long attackerUserId, long defenderUserId, int stars, int percentage, int duration)
        {
            if (State != ClanWarState.BATTLE_DAY)
            {
                return false;
            }

            var attacker = GetMember(attackerUserId);
            var defender = GetMember(defenderUserId);
            if (attacker == null || defender == null) return false;

            var attackerClan = GetClan(attacker.ClanId);
            var defenderClan = GetClan(defender.ClanId);
            if (attackerClan == null || defenderClan == null || attackerClan == defenderClan) return false;

            if (attacker.AttacksUsed >= 2)
            {
                return false;
            }

            attacker.AttacksUsed++;
            defender.GotAttackedCount++;

            if (stars > defender.StarsConceded)
            {
                defender.StarsConceded = stars;
            }
            if (percentage > defender.DamageConceded)
            {
                defender.DamageConceded = percentage;
            }

            // Calculate war win loot for attacker
            int starsGained = stars;
            if (starsGained > 0)
            {
                attacker.GoldGain += (int)(defender.GoldAvailable * (starsGained / 3.0));
                attacker.ElixirGain += (int)(defender.ElixirAvailable * (starsGained / 3.0));
                attacker.DarkElixirGain += (int)(defender.DarkElixirAvailable * (starsGained / 3.0));
            }

            var attackEntry = new WarAttackEntry
            {
                Order = AttackLogs.Count + 1,
                AttackerClanId = attackerClan.ClanId,
                AttackerUserId = attacker.UserId,
                AttackerName = attacker.Name,
                DefenderClanId = defenderClan.ClanId,
                DefenderUserId = defender.UserId,
                DefenderName = defender.Name,
                DefenderMapPosition = defender.MapPosition,
                Stars = stars,
                DestructionPercentage = percentage,
                Duration = duration > 0 ? duration : 120,
                Timestamp = (long)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds,
                ReplayId = DateTime.UtcNow.Ticks
            };

            defender.AttacksConceded.Add(attackEntry);
            AttackLogs.Add(attackEntry);

            attackerClan.RecalculateScoreAgainst(defenderClan);

            // Broadcast war map update to online clan members of both clans
            BroadcastWarMapUpdate();

            // Broadcast event in clan chat
            BroadcastAttackChatEvent(attackerClan, attackEntry);

            return true;
        }

        public async void BroadcastAttackChatEvent(WarClan clan, WarAttackEntry attack)
        {
            try
            {
                Alliance alliance = ObjectManager.GetAlliance(clan.ClanId);
                if (alliance != null)
                {
                    AllianceEventStreamEntry eventStreamEntry = new AllianceEventStreamEntry();
                    eventStreamEntry.ID = (int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
                    var attackerLevel = await ResourcesManager.GetPlayer(attack.AttackerUserId);
                    if (attackerLevel != null)
                    {
                        eventStreamEntry.SetSender(attackerLevel.Avatar);
                    }
                    eventStreamEntry.EventType = 8; // War attack event
                    alliance.AddChatMessage(eventStreamEntry);

                    foreach (var mem in alliance.GetAllianceMembers())
                    {
                        var player = await ResourcesManager.GetPlayer(mem.AvatarId);
                        if (player?.Client != null)
                        {
                            new AllianceStreamEntryMessage(player.Client) { StreamEntry = eventStreamEntry }.Send();
                        }
                    }
                }
            }
            catch (Exception) { }
        }

        public async void BroadcastWarMapUpdate()
        {
            try
            {
                foreach (var c in new[] { Clan1, Clan2 })
                {
                    if (c == null) continue;
                    Alliance a = ObjectManager.GetAlliance(c.ClanId);
                    if (a != null)
                    {
                        foreach (var mem in a.GetAllianceMembers())
                        {
                            var player = await ResourcesManager.GetPlayer(mem.AvatarId);
                            if (player?.Client != null)
                            {
                                new AllianceWarMapDataMessage(player.Client, this).Send();
                            }
                        }
                    }
                }
            }
            catch (Exception) { }
        }

        public void FastForward()
        {
            if (State == ClanWarState.PREPARATION_DAY)
            {
                PreparationEndTime = DateTime.UtcNow;
                Tick();
            }
            else if (State == ClanWarState.BATTLE_DAY)
            {
                BattleEndTime = DateTime.UtcNow;
                Tick();
            }
        }

        public void Tick()
        {
            DateTime now = DateTime.UtcNow;
            if (State == ClanWarState.PREPARATION_DAY)
            {
                if (now >= PreparationEndTime)
                {
                    State = ClanWarState.BATTLE_DAY;
                    BattleStartTime = now;
                    // Standard battle duration: 24h (or 15 min if fast mode)
                    int durationMinutes = ClanWarManager.WarBattleDurationMinutes;
                    BattleEndTime = now.AddMinutes(durationMinutes);

                    UpdateAllianceWarStates(ClanWarState.BATTLE_DAY);
                    BroadcastWarMapUpdate();
                    Logger.Say($"Clan War {WarId}: Battle Day has started between '{Clan1?.ClanName}' and '{Clan2?.ClanName}'!");
                }
            }
            else if (State == ClanWarState.BATTLE_DAY)
            {
                if (now >= BattleEndTime)
                {
                    State = ClanWarState.WAR_ENDED;
                    WarEndedTime = now;
                    EndWar();
                    UpdateAllianceWarStates(ClanWarState.WAR_ENDED);
                    BroadcastWarMapUpdate();
                    Logger.Say($"Clan War {WarId}: War has concluded! Final Score: '{Clan1?.ClanName}' ({Clan1?.TotalStars}*) vs '{Clan2?.ClanName}' ({Clan2?.TotalStars}*)");
                }
            }
        }

        private void UpdateAllianceWarStates(ClanWarState state)
        {
            foreach (var c in new[] { Clan1, Clan2 })
            {
                if (c == null) continue;
                Alliance a = ObjectManager.GetAlliance(c.ClanId);
                if (a != null)
                {
                    a.m_vWarState = (int)state;
                    a.m_vActiveWarId = WarId;
                }
            }
        }

        private void EndWar()
        {
            if (Clan1 == null || Clan2 == null) return;

            Alliance a1 = ObjectManager.GetAlliance(Clan1.ClanId);
            Alliance a2 = ObjectManager.GetAlliance(Clan2.ClanId);

            int xpClan1 = 50 + Clan1.TotalStars * 5;
            int xpClan2 = 50 + Clan2.TotalStars * 5;

            bool clan1Won = false;
            bool clan2Won = false;
            bool draw = false;

            if (Clan1.TotalStars > Clan2.TotalStars)
            {
                clan1Won = true;
                xpClan1 += 130;
            }
            else if (Clan2.TotalStars > Clan1.TotalStars)
            {
                clan2Won = true;
                xpClan2 += 130;
            }
            else
            {
                // Stars tied -> check total destruction
                if (Clan1.TotalDestruction > Clan2.TotalDestruction)
                {
                    clan1Won = true;
                    xpClan1 += 130;
                }
                else if (Clan2.TotalDestruction > Clan1.TotalDestruction)
                {
                    clan2Won = true;
                    xpClan2 += 130;
                }
                else
                {
                    draw = true;
                    xpClan1 += 65;
                    xpClan2 += 65;
                }
            }

            if (a1 != null)
            {
                if (clan1Won) a1.m_vWonWars++;
                else if (clan2Won) a1.m_vLostWars++;
                else a1.m_vDrawWars++;

                a1.m_vAllianceExperience += xpClan1;
                while (a1.m_vAllianceExperience >= a1.m_vAllianceLevel * 1000)
                {
                    a1.m_vAllianceExperience -= a1.m_vAllianceLevel * 1000;
                    a1.m_vAllianceLevel++;
                }
            }

            if (a2 != null)
            {
                if (clan2Won) a2.m_vWonWars++;
                else if (clan1Won) a2.m_vLostWars++;
                else a2.m_vDrawWars++;

                a2.m_vAllianceExperience += xpClan2;
                while (a2.m_vAllianceExperience >= a2.m_vAllianceLevel * 1000)
                {
                    a2.m_vAllianceExperience -= a2.m_vAllianceLevel * 1000;
                    a2.m_vAllianceLevel++;
                }
            }

            // Distribute war win bonus to players who participated
            DistributeWarLoot(Clan1, clan1Won, draw);
            DistributeWarLoot(Clan2, clan2Won, draw);

            // Record to clan war history
            ClanWarManager.RecordWarHistory(this);
        }

        private async void DistributeWarLoot(WarClan clan, bool won, bool draw)
        {
            if (clan == null) return;
            double factor = won ? 1.0 : (draw ? 0.5 : 0.0);
            if (factor <= 0) return;

            foreach (var m in clan.Members)
            {
                if (m.AttacksUsed > 0 && (m.GoldGain > 0 || m.ElixirGain > 0))
                {
                    int goldReward = (int)(m.GoldGain * factor);
                    int elixirReward = (int)(m.ElixirGain * factor);
                    int darkReward = (int)(m.DarkElixirGain * factor);

                    Level player = await ResourcesManager.GetPlayer(m.UserId);
                    if (player?.Avatar != null)
                    {
                        var goldRes = CSVManager.DataTables.GetResourceByName("Gold");
                        var elixirRes = CSVManager.DataTables.GetResourceByName("Elixir");
                        var darkRes = CSVManager.DataTables.GetResourceByName("DarkElixir");

                        if (goldRes != null && goldReward > 0)
                        {
                            player.Avatar.SetResourceCount(goldRes, player.Avatar.GetResourceCount(goldRes) + goldReward);
                        }
                        if (elixirRes != null && elixirReward > 0)
                        {
                            player.Avatar.SetResourceCount(elixirRes, player.Avatar.GetResourceCount(elixirRes) + elixirReward);
                        }
                        if (darkRes != null && darkReward > 0)
                        {
                            player.Avatar.SetResourceCount(darkRes, player.Avatar.GetResourceCount(darkRes) + darkReward);
                        }

                        if (player.Client != null)
                        {
                            new OwnHomeDataMessage(player.Client, player).Send();
                        }
                    }
                }
            }
            ClanWarManager.SaveState();
        }

        public JObject SaveToJson()
        {
            var json = new JObject
            {
                { "war_id", WarId },
                { "state", (int)State },
                { "war_size", WarSize },
                { "prep_start", PreparationStartTime.ToString("o") },
                { "prep_end", PreparationEndTime.ToString("o") },
                { "battle_start", BattleStartTime.ToString("o") },
                { "battle_end", BattleEndTime.ToString("o") },
                { "war_ended", WarEndedTime.ToString("o") }
            };

            if (Clan1 != null) json.Add("clan1", Clan1.Save());
            if (Clan2 != null) json.Add("clan2", Clan2.Save());

            var atkArray = new JArray();
            foreach (var a in AttackLogs)
            {
                atkArray.Add(a.Save());
            }
            json.Add("attack_logs", atkArray);

            return json;
        }

        public void LoadFromJson(JObject json)
        {
            WarId = json["war_id"]?.ToObject<long>() ?? 0;
            State = (ClanWarState)(json["state"]?.ToObject<int>() ?? 0);
            WarSize = json["war_size"]?.ToObject<int>() ?? 10;

            if (DateTime.TryParse(json["prep_start"]?.ToString(), out DateTime ps)) PreparationStartTime = ps;
            if (DateTime.TryParse(json["prep_end"]?.ToString(), out DateTime pe)) PreparationEndTime = pe;
            if (DateTime.TryParse(json["battle_start"]?.ToString(), out DateTime bs)) BattleStartTime = bs;
            if (DateTime.TryParse(json["battle_end"]?.ToString(), out DateTime be)) BattleEndTime = be;
            if (DateTime.TryParse(json["war_ended"]?.ToString(), out DateTime we)) WarEndedTime = we;

            if (json["clan1"] is JObject c1)
            {
                Clan1 = new WarClan();
                Clan1.Load(c1);
            }
            if (json["clan2"] is JObject c2)
            {
                Clan2 = new WarClan();
                Clan2.Load(c2);
            }

            AttackLogs = new List<WarAttackEntry>();
            if (json["attack_logs"] is JArray arr)
            {
                foreach (JObject item in arr)
                {
                    var a = new WarAttackEntry();
                    a.Load(item);
                    AttackLogs.Add(a);
                }
            }
        }
    }

    public static class ClanWarManager
    {
        public static ConcurrentDictionary<long, ClanWar> Wars = new ConcurrentDictionary<long, ClanWar>();
        public static ConcurrentDictionary<long, long> ClanToWarMap = new ConcurrentDictionary<long, long>();
        public static List<long> SearchingClans = new List<long>();
        public static ConcurrentDictionary<long, ActiveWarBattleSession> ActiveBattles = new ConcurrentDictionary<long, ActiveWarBattleSession>();
        public static ConcurrentDictionary<long, List<ClanWar>> ClanWarHistories = new ConcurrentDictionary<long, List<ClanWar>>();

        public static int WarPreparationDurationMinutes = 1440; // 24 hours
        public static int WarBattleDurationMinutes = 1440;      // 24 hours
        public static bool WarFastMode = false;                 // If true, 2-minute phases for testing

        private static long m_vWarSeed = 1000;
        private static System.Threading.Timer m_vTickTimer;
        private static readonly string WarsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Gamefiles", "database", "clan_wars.json");
        private static readonly string HistoryFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Gamefiles", "database", "clan_war_history.json");

        public static void SaveState()
        {
            try
            {
                var dir = Path.GetDirectoryName(WarsFilePath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                var warsArray = new JArray();
                foreach (var w in Wars.Values)
                {
                    warsArray.Add(w.SaveToJson());
                }
                File.WriteAllText(WarsFilePath, warsArray.ToString(Formatting.None));

                var histObj = new JObject();
                foreach (var kvp in ClanWarHistories)
                {
                    var arr = new JArray();
                    lock (kvp.Value)
                    {
                        foreach (var w in kvp.Value)
                        {
                            arr.Add(w.SaveToJson());
                        }
                    }
                    histObj.Add(kvp.Key.ToString(), arr);
                }
                File.WriteAllText(HistoryFilePath, histObj.ToString(Formatting.None));
            }
            catch (Exception ex)
            {
                Logger.Write($"[Error] ClanWarManager.SaveState error: {ex.Message}");
            }
        }

        public static void LoadState()
        {
            try
            {
                if (File.Exists(WarsFilePath))
                {
                    string content = File.ReadAllText(WarsFilePath);
                    if (!string.IsNullOrWhiteSpace(content))
                    {
                        var arr = JArray.Parse(content);
                        foreach (JObject item in arr)
                        {
                            var war = new ClanWar();
                            war.LoadFromJson(item);
                            Wars[war.WarId] = war;
                            if (war.Clan1 != null) ClanToWarMap[war.Clan1.ClanId] = war.WarId;
                            if (war.Clan2 != null) ClanToWarMap[war.Clan2.ClanId] = war.WarId;
                            if (war.WarId >= m_vWarSeed) m_vWarSeed = war.WarId + 1;
                        }
                    }
                }

                if (File.Exists(HistoryFilePath))
                {
                    string content = File.ReadAllText(HistoryFilePath);
                    if (!string.IsNullOrWhiteSpace(content))
                    {
                        var obj = JObject.Parse(content);
                        foreach (var prop in obj.Properties())
                        {
                            if (long.TryParse(prop.Name, out long clanId) && prop.Value is JArray arr)
                            {
                                var list = ClanWarHistories.GetOrAdd(clanId, k => new List<ClanWar>());
                                lock (list)
                                {
                                    list.Clear();
                                    foreach (JObject item in arr)
                                    {
                                        var war = new ClanWar();
                                        war.LoadFromJson(item);
                                        list.Add(war);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Write($"[Error] ClanWarManager.LoadState error: {ex.Message}");
            }
        }

        public static void RecordWarHistory(ClanWar war)
        {
            if (war?.Clan1 != null)
            {
                var list1 = ClanWarHistories.GetOrAdd(war.Clan1.ClanId, k => new List<ClanWar>());
                lock (list1) { list1.Insert(0, war); }
            }
            if (war?.Clan2 != null)
            {
                var list2 = ClanWarHistories.GetOrAdd(war.Clan2.ClanId, k => new List<ClanWar>());
                lock (list2) { list2.Insert(0, war); }
            }
            SaveState();
        }

        public static List<ClanWar> GetWarHistory(long clanId)
        {
            if (ClanWarHistories.TryGetValue(clanId, out var list))
            {
                lock (list) { return list.ToList(); }
            }
            return new List<ClanWar>();
        }

        public static void SetActiveBattle(long attackerUserId, long warId, long defenderUserId, int attackerTh, int defenderTh, int totalHousingSpace)
        {
            ActiveBattles[attackerUserId] = new ActiveWarBattleSession
            {
                WarId = warId,
                AttackerUserId = attackerUserId,
                DefenderUserId = defenderUserId,
                StartTime = DateTime.UtcNow,
                AttackerTownHall = attackerTh,
                DefenderTownHall = defenderTh,
                TotalHousingSpace = totalHousingSpace,
                HousingSpaceDeployed = 0
            };
        }

        public static void TrackTroopDeployment(long attackerUserId, int housingSpace)
        {
            if (ActiveBattles.TryGetValue(attackerUserId, out var session))
            {
                session.HousingSpaceDeployed += housingSpace;
            }
        }

        public static bool FinalizeBattle(long attackerId, int forcedStars = -1, int forcedPercentage = -1)
        {
            if (!ActiveBattles.TryRemove(attackerId, out ActiveWarBattleSession session))
            {
                return false;
            }

            ClanWar war = GetWar(session.WarId);
            if (war == null) return false;

            int duration = (int)Math.Max(15, (DateTime.UtcNow - session.StartTime).TotalSeconds);

            int stars;
            int percentage;

            if (forcedStars >= 0 && forcedPercentage >= 0)
            {
                stars = Math.Min(3, Math.Max(0, forcedStars));
                percentage = Math.Min(100, Math.Max(0, forcedPercentage));
            }
            else
            {
                double deployRatio = session.TotalHousingSpace > 0
                    ? (double)session.HousingSpaceDeployed / session.TotalHousingSpace
                    : (session.HousingSpaceDeployed > 0 ? 1.0 : 0.0);

                if (session.HousingSpaceDeployed == 0)
                {
                    stars = 0;
                    percentage = 0;
                }
                else
                {
                    int thDelta = session.AttackerTownHall - session.DefenderTownHall;
                    if (deployRatio >= 0.6)
                    {
                        if (thDelta >= 0)
                        {
                            stars = 3;
                            percentage = 100;
                        }
                        else if (thDelta == -1)
                        {
                            stars = 2;
                            percentage = 70 + new Random().Next(0, 20);
                        }
                        else
                        {
                            stars = 1;
                            percentage = 50 + new Random().Next(0, 20);
                        }
                    }
                    else if (deployRatio >= 0.3)
                    {
                        if (thDelta >= 0)
                        {
                            stars = 2;
                            percentage = 60 + new Random().Next(0, 15);
                        }
                        else
                        {
                            stars = 1;
                            percentage = 50 + new Random().Next(0, 10);
                        }
                    }
                    else
                    {
                        stars = (thDelta >= 2) ? 1 : 0;
                        percentage = 20 + new Random().Next(0, 25);
                    }
                }
            }

            bool recorded = war.RecordAttack(session.AttackerUserId, session.DefenderUserId, stars, percentage, duration);
            if (recorded)
            {
                SaveState();
            }
            return recorded;
        }

        public static void SkipPhase(ClanWar war)
        {
            if (war == null) return;
            if (war.State == ClanWarState.PREPARATION_DAY)
            {
                war.PreparationEndTime = DateTime.UtcNow;
                war.Tick();
            }
            else if (war.State == ClanWarState.BATTLE_DAY)
            {
                war.BattleEndTime = DateTime.UtcNow;
                war.Tick();
            }
        }

        public static void ForceEnd(ClanWar war)
        {
            if (war == null) return;
            war.PreparationEndTime = DateTime.UtcNow.AddMinutes(-10);
            war.BattleEndTime = DateTime.UtcNow.AddMinutes(-5);
            war.State = ClanWarState.BATTLE_DAY;
            war.Tick();
        }

        static ClanWarManager()
        {
            LoadState();
            m_vTickTimer = new System.Threading.Timer(OnTick, null, 5000, 5000);
        }

        private static void OnTick(object state)
        {
            try
            {
                foreach (var war in Wars.Values)
                {
                    war.Tick();
                }
            }
            catch (Exception) { }
        }

        public static ClanWar GetWar(long warId)
        {
            if (Wars.TryGetValue(warId, out ClanWar war))
            {
                return war;
            }
            return null;
        }

        public static ClanWar GetWarByClanId(long clanId)
        {
            if (ClanToWarMap.TryGetValue(clanId, out long warId))
            {
                return GetWar(warId);
            }
            return null;
        }

        public static ClanWarState GetClanWarState(long clanId)
        {
            var war = GetWarByClanId(clanId);
            if (war != null)
            {
                return war.State;
            }
            if (SearchingClans.Contains(clanId))
            {
                return ClanWarState.SEARCHING;
            }
            return ClanWarState.NOT_IN_WAR;
        }

        internal static async Task<ClanWar> StartWar(Alliance clan1, Alliance clan2 = null, int teamSize = 10)
        {
            if (clan1 == null) return null;

            long warId = ++m_vWarSeed;
            ClanWar war = new ClanWar
            {
                WarId = warId,
                WarSize = teamSize,
                State = ClanWarState.PREPARATION_DAY,
                PreparationStartTime = DateTime.UtcNow
            };

            int prepMinutes = WarFastMode ? 2 : WarPreparationDurationMinutes;
            int battleMinutes = WarFastMode ? 5 : WarBattleDurationMinutes;

            war.PreparationEndTime = war.PreparationStartTime.AddMinutes(prepMinutes);
            war.BattleStartTime = war.PreparationEndTime;
            war.BattleEndTime = war.BattleStartTime.AddMinutes(battleMinutes);

            // Populate Clan 1
            war.Clan1 = new WarClan(clan1);
            var members1 = clan1.GetAllianceMembers().Where(m => m.WarOptInStatus == 1).ToList();
            if (members1.Count < teamSize)
            {
                members1 = clan1.GetAllianceMembers();
            }

            int pos = 0;
            foreach (var m in members1.Take(teamSize))
            {
                Level p = await ResourcesManager.GetPlayer(m.AvatarId);
                if (p != null)
                {
                    var warMember = new ClanWarMember(p.Avatar, pos, clan1.m_vAllianceId)
                    {
                        VillageJson = p.SaveToJSON()
                    };
                    war.Clan1.Members.Add(warMember);
                    pos++;
                }
            }

            // Fill remaining slots if clan has fewer than teamSize members
            while (war.Clan1.Members.Count < teamSize)
            {
                int dummyPos = war.Clan1.Members.Count;
                var dummy = CreatePresetWarMember(clan1.m_vAllianceId, dummyPos, 8, $"Warrior {dummyPos + 1}");
                war.Clan1.Members.Add(dummy);
            }

            // Sort members by TownHall level / defense weight descending, so map positions 0..teamSize-1 match strength!
            war.Clan1.Members = war.Clan1.Members.OrderByDescending(m => m.TownHallLevel).ThenByDescending(m => m.DefenceWeight).ToList();
            for (int i = 0; i < war.Clan1.Members.Count; i++)
            {
                war.Clan1.Members[i].MapPosition = i;
            }

            // Populate Clan 2
            if (clan2 != null)
            {
                war.Clan2 = new WarClan(clan2);
                var members2 = clan2.GetAllianceMembers().Where(m => m.WarOptInStatus == 1).ToList();
                if (members2.Count < teamSize) members2 = clan2.GetAllianceMembers();

                pos = 0;
                foreach (var m in members2.Take(teamSize))
                {
                    Level p = await ResourcesManager.GetPlayer(m.AvatarId);
                    if (p != null)
                    {
                        var warMember = new ClanWarMember(p.Avatar, pos, clan2.m_vAllianceId)
                        {
                            VillageJson = p.SaveToJSON()
                        };
                        war.Clan2.Members.Add(warMember);
                        pos++;
                    }
                }
                while (war.Clan2.Members.Count < teamSize)
                {
                    int dummyPos = war.Clan2.Members.Count;
                    var dummy = CreatePresetWarMember(clan2.m_vAllianceId, dummyPos, 8, $"Defender {dummyPos + 1}");
                    war.Clan2.Members.Add(dummy);
                }
                war.Clan2.Members = war.Clan2.Members.OrderByDescending(m => m.TownHallLevel).ThenByDescending(m => m.DefenceWeight).ToList();
                for (int i = 0; i < war.Clan2.Members.Count; i++)
                {
                    war.Clan2.Members[i].MapPosition = i;
                }
            }
            else
            {
                // Generate AI Opponent Clan using PVP Base Presets
                war.Clan2 = GenerateAIOpponentClan(teamSize);
            }

            Wars[warId] = war;
            ClanToWarMap[clan1.m_vAllianceId] = warId;
            if (clan2 != null)
            {
                ClanToWarMap[clan2.m_vAllianceId] = warId;
            }
            SaveState();

            // Update clan state
            clan1.m_vWarState = (int)ClanWarState.PREPARATION_DAY;
            clan1.m_vActiveWarId = warId;
            if (clan2 != null)
            {
                clan2.m_vWarState = (int)ClanWarState.PREPARATION_DAY;
                clan2.m_vActiveWarId = warId;
            }

            // Broadcast War started message & map update
            war.BroadcastWarMapUpdate();

            Logger.Say($"Started Clan War {warId}: '{war.Clan1.ClanName}' vs '{war.Clan2.ClanName}' ({teamSize}v{teamSize})");
            return war;
        }

        public static ClanWarMember CreatePresetWarMember(long clanId, int pos, int th, string name)
        {
            long dummyId = clanId * 1000 + pos + 1;
            string presetVillage = GetPresetBase(th);
            return new ClanWarMember
            {
                UserId = dummyId,
                HomeId = dummyId,
                ClanId = clanId,
                Name = name,
                MapPosition = pos,
                TownHallLevel = th,
                OffenceWeight = 500 + th * 30,
                DefenceWeight = 500 + th * 25,
                ClanCastleLevel = (byte)Math.Min(6, Math.Max(1, th / 2)),
                ClanCastleMaxCapacity = 10 + Math.Min(6, Math.Max(1, th / 2)) * 5,
                ClanCastleMessage = "",
                GoldAvailable = 100000 + th * 15000,
                ElixirAvailable = 100000 + th * 15000,
                DarkElixirAvailable = th >= 7 ? 500 + th * 80 : 0,
                VillageJson = presetVillage
            };
        }

        private static WarClan GenerateAIOpponentClan(int teamSize)
        {
            long aiClanId = 99990000 + new Random().Next(100, 999);
            string[] names = { "Shadow Legion", "Goblin Horde", "Valkyrie Storm", "Dragon Slayers", "P.E.K.K.A Force", "Clash Kings" };
            string chosenName = names[new Random().Next(0, names.Length)];

            var aiClan = new WarClan
            {
                ClanId = aiClanId,
                ClanName = chosenName,
                ClanBadge = 0x5B002B54,
                ClanLevel = 5,
                Members = new List<ClanWarMember>()
            };

            var aiAlliance = new Alliance(aiClanId)
            {
                m_vAllianceName = chosenName,
                m_vAllianceBadgeData = 0x5B002B54,
                m_vAllianceLevel = 5,
                m_vAllianceType = 0
            };
            ResourcesManager.AddAllianceInMemory(aiAlliance);

            for (int i = 0; i < teamSize; i++)
            {
                int th = Math.Max(6, 11 - (i * 5 / teamSize));
                string name = $"Enemy #{i + 1}";
                aiClan.Members.Add(CreatePresetWarMember(aiClanId, i, th, name));
            }

            aiClan.Members = aiClan.Members.OrderByDescending(m => m.TownHallLevel).ThenByDescending(m => m.DefenceWeight).ToList();
            for (int i = 0; i < aiClan.Members.Count; i++)
            {
                aiClan.Members[i].MapPosition = i;
            }

            return aiClan;
        }

        public static string GetPresetBase(int thLevel)
        {
            try
            {
                string path = $@"Gamefiles/level/PVP/Base{Math.Min(68, Math.Max(1, thLevel * 6))}.json";
                if (File.Exists(path))
                {
                    return File.ReadAllText(path);
                }
            }
            catch (Exception) { }

            // Fallback starting home
            return File.ReadAllText(@"Gamefiles/starting_home.json");
        }
    }
}
