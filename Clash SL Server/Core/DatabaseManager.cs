using System;
using System.Collections.Generic;
using System.Configuration;
using MySql.Data.MySqlClient;
using System.Linq;
using CSS.Database;
using CSS.Logic;
using CSS.Core.Settings;
using static CSS.Core.Logger;
using System.Threading.Tasks;
using CSS.Logic.Enums;
using CSS.Helpers;
using Newtonsoft.Json.Linq;

namespace CSS.Core
{
    internal class DatabaseManager 
    {
        internal string Mysql;

        public DatabaseManager()
        {
            Task.Run(() => InitializeTables());
        }

        private string EnsureConnectionString()
        {
            if (string.IsNullOrEmpty(this.Mysql))
            {
                string ip = Utils.ParseConfigString("MysqlIPAddress") ?? "127.0.0.1";
                string user = Utils.ParseConfigString("MysqlUsername") ?? "root";
                uint port = 3306;
                try
                {
                    int p = Utils.ParseConfigInt("MysqlPort");
                    if (p > 0) port = (uint)p;
                }
                catch { }
                string db = Utils.ParseConfigString("MysqlDatabase") ?? "cssdb";

                MySqlConnectionStringBuilder builder = new MySqlConnectionStringBuilder()
                {
                    Server = ip,
                    UserID = user,
                    Port = port,
                    Pooling = false,
                    Database = db,
                    MinimumPoolSize = 1,
                    CharacterSet = "utf8mb4",
                    ConvertZeroDateTime = true
                };

                string pass = Utils.ParseConfigString("MysqlPassword");
                if (!string.IsNullOrWhiteSpace(pass))
                {
                    builder.Password = pass;
                }

                this.Mysql = builder.ToString();
            }
            return this.Mysql;
        }

        public void CreateAccount(Level l)
        {
            try
            {
                if (l == null || l.Avatar == null) return;

                if (Constants.UseCacheServer)
                    Redis.Players.StringSet(l.Avatar.UserId.ToString(), l.Avatar.SaveToJSON() + "#:#:#:#" + l.SaveToJSON(),
                        TimeSpan.FromDays(366));

                using (MySqlConnection conn = new MySqlConnection(EnsureConnectionString()))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand(
                        "INSERT INTO Player (PlayerId, Avatar, GameObjects, LastUpdateTime, IPAddress) " +
                        "VALUES (@id, @avatar, @go, NOW(), @ip) " +
                        "ON DUPLICATE KEY UPDATE Avatar=@avatar, GameObjects=@go, LastUpdateTime=NOW(), IPAddress=@ip;", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", l.Avatar.UserId);
                        cmd.Parameters.AddWithValue("@avatar", l.Avatar.SaveToJSON());
                        cmd.Parameters.AddWithValue("@go", l.SaveToJSON());
                        cmd.Parameters.AddWithValue("@ip", l.Avatar.IPAddress ?? "");
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception a)
            {
                Error($"DatabaseManager.CreateAccount failed for PlayerId {l?.Avatar?.UserId}: {a.Message}");
            }
        }

        public void CreateAlliance(Alliance a)
        {
            try
            {
                if (a == null) return;

                if (Constants.UseCacheServer)
                    Redis.Clans.StringSet(a.m_vAllianceId.ToString(), a.SaveToJSON(), TimeSpan.FromDays(366));

                using (MySqlConnection conn = new MySqlConnection(EnsureConnectionString()))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand(
                        "INSERT INTO Clan (ClanId, LastUpdateTime, Data) " +
                        "VALUES (@id, NOW(), @data) " +
                        "ON DUPLICATE KEY UPDATE Data=@data, LastUpdateTime=NOW();", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", a.m_vAllianceId);
                        cmd.Parameters.AddWithValue("@data", a.SaveToJSON());
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Error($"DatabaseManager.CreateAlliance failed for ClanId {a?.m_vAllianceId}: {ex.Message}");
            }
        }

        public async Task<Level> GetAccount(long playerId)
        {
            try
            {
                Level account = null;
                if (Constants.UseCacheServer)
                {
                    string _Data = Redis.Players.StringGet(playerId.ToString()).ToString();

                    if (!string.IsNullOrEmpty(_Data) && _Data.Contains("#:#:#:#"))
                    {
                        string[] _Datas = _Data.Split(new string[1] { "#:#:#:#" }, StringSplitOptions.None);

                        if (!string.IsNullOrEmpty(_Datas[0]) && !string.IsNullOrEmpty(_Datas[1]))
                        {
                            account = new Level();
                            account.Avatar.LoadFromJSON(_Datas[0]);
                            account.LoadFromJSON(_Datas[1]);
                            return account;
                        }
                    }
                }

                using (MySqlConnection conn = new MySqlConnection(EnsureConnectionString()))
                {
                    await conn.OpenAsync();
                    using (MySqlCommand cmd = new MySqlCommand("SELECT Avatar, GameObjects FROM Player WHERE PlayerId = @id LIMIT 1", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", playerId);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                string avatarJson = reader.GetString(0);
                                string gameObjectsJson = reader.GetString(1);
                                account = new Level();
                                account.Avatar.LoadFromJSON(avatarJson);
                                account.LoadFromJSON(gameObjectsJson);

                                if (Constants.UseCacheServer)
                                    Redis.Players.StringSet(playerId.ToString(), avatarJson + "#:#:#:#" + gameObjectsJson, TimeSpan.FromDays(366));
                            }
                        }
                    }
                }
                return account;
            }
            catch (Exception ex)
            {
                Error($"DatabaseManager.GetAccount failed for PlayerId {playerId}: {ex.Message}");
                return null;
            }
        }

        public Alliance GetAlliance(long allianceId)
        {
            try
            {
                Alliance alliance = null;
                if (Constants.UseCacheServer) 
                {
                    string _Data = Redis.Clans.StringGet(allianceId.ToString()).ToString();

                    if (!string.IsNullOrEmpty(_Data))
                    {
                        alliance = new Alliance();
                        alliance.LoadFromJSON(_Data);
                        return alliance;
                    }
                }

                using (MySqlConnection conn = new MySqlConnection(EnsureConnectionString()))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SELECT Data FROM Clan WHERE ClanId = @id LIMIT 1", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", allianceId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string data = reader.GetString(0);
                                alliance = new Alliance();
                                alliance.LoadFromJSON(data);

                                if (Constants.UseCacheServer)
                                    Redis.Clans.StringSet(allianceId.ToString(), data, TimeSpan.FromDays(366));
                            }
                        }
                    }
                }
                return alliance;
            }
            catch (Exception ex)
            {
                Error($"DatabaseManager.GetAlliance failed for ClanId {allianceId}: {ex.Message}");
                return null;
            }
        }

        public List<long> GetAllPlayerIds()
        {
            List<long> ids = new List<long>();
            try
            {
                using (MySqlConnection conn = new MySqlConnection(EnsureConnectionString()))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SELECT PlayerId FROM Player", conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            ids.Add(reader.GetInt64(0));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Error($"DatabaseManager.GetAllPlayerIds failed: {ex.Message}");
            }
            return ids;
        }

        public List<long> GetAllClanIds()
        {
            List<long> ids = new List<long>();
            try
            {
                using (MySqlConnection conn = new MySqlConnection(EnsureConnectionString()))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SELECT ClanId FROM Clan", conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            ids.Add(reader.GetInt64(0));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Error($"DatabaseManager.GetAllClanIds failed: {ex.Message}");
            }
            return ids;
        }

        public long GetMaxAllianceId()
        {
            try
            {
                const string SQL = "SELECT coalesce(MAX(ClanId), 0) FROM Clan";
                long Seed = -1;

                using (MySqlConnection Conn = new MySqlConnection(EnsureConnectionString()))
                {
                    Conn.Open();

                    using (MySqlCommand CMD = new MySqlCommand(SQL, Conn))
                    {
                        CMD.Prepare();
                        Seed = Convert.ToInt64(CMD.ExecuteScalar());
                    }
                }

                return Seed;
            }
            catch (Exception ex)
            {
                Error($"DatabaseManager.GetMaxAllianceId failed: {ex.Message}");
            }
            return 0;
        }

        public long GetMaxPlayerId()
        {
            try
            {
                const string SQL = "SELECT coalesce(MAX(PlayerId), 0) FROM Player";
                long Seed = -1;

                using (MySqlConnection Conn = new MySqlConnection(EnsureConnectionString()))
                {
                    Conn.Open();

                    using (MySqlCommand CMD = new MySqlCommand(SQL, Conn))
                    {
                        CMD.Prepare();
                        Seed = Convert.ToInt64(CMD.ExecuteScalar());
                    }
                }

                return Seed;
            }
            catch (Exception ex)
            {
                Say();
                Error("An exception occured when reconnecting to the MySQL Server.");
                Error("Please check your database configuration!");
                Error(ex.Message);
            }
            return 0;
        }

        public void RemoveAlliance(Alliance alliance)
        {
            try
            {
                if (alliance == null) return;
                long id = alliance.m_vAllianceId;
                using (MySqlConnection conn = new MySqlConnection(EnsureConnectionString()))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand("DELETE FROM Clan WHERE ClanId = @id", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.ExecuteNonQuery();
                    }
                }
                ObjectManager.RemoveInMemoryAlliance(id);
            }
            catch (Exception ex)
            {
                Error($"DatabaseManager.RemoveAlliance failed for ClanId {alliance?.m_vAllianceId}: {ex.Message}");
            }
        }

        public Level GetPlayerViaFacebook(string FacebookID)
        {
            try
            {
                Level account = null;
                using (MySqlConnection conn = new MySqlConnection(EnsureConnectionString()))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SELECT PlayerId, Avatar, GameObjects FROM Player WHERE Avatar LIKE @fb LIMIT 1", conn))
                    {
                        cmd.Parameters.AddWithValue("@fb", "%" + FacebookID + "%");
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                account = new Level();
                                string av = reader.GetString(1);
                                string go = reader.GetString(2);
                                account.Avatar.LoadFromJSON(av);
                                account.LoadFromJSON(go);
                                if (Constants.UseCacheServer)
                                    Redis.Players.StringSet(reader.GetInt64(0).ToString(), av + "#:#:#:#" + go, TimeSpan.FromDays(366));
                            }
                        }
                    }
                }
                return account;
            }
            catch (Exception ex)
            {
                Error($"DatabaseManager.GetPlayerViaFacebook failed: {ex.Message}");
                return null;
            }
        }

        public async Task Save(Alliance alliance)
        {
            try
            {
                if (alliance == null) return;
                if (Constants.UseCacheServer)
                    Redis.Clans.StringSet(alliance.m_vAllianceId.ToString(), alliance.SaveToJSON(), TimeSpan.FromDays(366));

                using (MySqlConnection conn = new MySqlConnection(EnsureConnectionString()))
                {
                    await conn.OpenAsync();
                    using (MySqlCommand cmd = new MySqlCommand(
                        "INSERT INTO Clan (ClanId, LastUpdateTime, Data) " +
                        "VALUES (@id, NOW(), @data) " +
                        "ON DUPLICATE KEY UPDATE Data = @data, LastUpdateTime = NOW()", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", alliance.m_vAllianceId);
                        cmd.Parameters.AddWithValue("@data", alliance.SaveToJSON());
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Error($"DatabaseManager.Save(Alliance) failed for ClanId {alliance?.m_vAllianceId}: {ex.Message}");
            }
        }

        public async Task Save(Level avatar)
        {
            try
            {
                if (avatar == null || avatar.Avatar == null) return;
                if (Constants.UseCacheServer)
                    Redis.Players.StringSet(avatar.Avatar.UserId.ToString(),
                        avatar.Avatar.SaveToJSON() + "#:#:#:#" + avatar.SaveToJSON(), TimeSpan.FromDays(366));

                using (MySqlConnection conn = new MySqlConnection(EnsureConnectionString()))
                {
                    await conn.OpenAsync();
                    using (MySqlCommand cmd = new MySqlCommand(
                        "INSERT INTO Player (PlayerId, Avatar, GameObjects, LastUpdateTime, IPAddress) " +
                        "VALUES (@id, @avatar, @go, NOW(), @ip) " +
                        "ON DUPLICATE KEY UPDATE Avatar = @avatar, GameObjects = @go, LastUpdateTime = NOW(), IPAddress = @ip", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", avatar.Avatar.UserId);
                        cmd.Parameters.AddWithValue("@avatar", avatar.Avatar.SaveToJSON());
                        cmd.Parameters.AddWithValue("@go", avatar.SaveToJSON());
                        cmd.Parameters.AddWithValue("@ip", avatar.Avatar.IPAddress ?? "");
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Error($"DatabaseManager.Save(Level) failed for PlayerId {avatar?.Avatar?.UserId}: {ex.Message}");
            }
        }

        public async Task Save(List<Level> avatars, Save Save = Logic.Enums.Save.Mysql)
        {
            try
            {
                if (avatars == null || avatars.Count == 0) return;
                switch (Save)
                {
                    case Save.Redis:
                        {
                            if (!Constants.UseCacheServer || Redis.Players == null) break;
                            foreach (Level pl in avatars)
                            {
                                if (pl != null && pl.Avatar != null)
                                {
                                    Redis.Players.StringSet(pl.Avatar.UserId.ToString(),
                                        pl.Avatar.SaveToJSON() + "#:#:#:#" + pl.SaveToJSON(), TimeSpan.FromDays(366));
                                }
                            }
                            break;
                        }

                    case Save.Mysql:
                        {
                            using (MySqlConnection conn = new MySqlConnection(EnsureConnectionString()))
                            {
                                await conn.OpenAsync();
                                foreach (Level pl in avatars)
                                {
                                    if (pl == null || pl.Avatar == null) continue;
                                    using (MySqlCommand cmd = new MySqlCommand(
                                        "INSERT INTO Player (PlayerId, Avatar, GameObjects, LastUpdateTime, IPAddress) " +
                                        "VALUES (@id, @avatar, @go, NOW(), @ip) " +
                                        "ON DUPLICATE KEY UPDATE Avatar = @avatar, GameObjects = @go, LastUpdateTime = NOW(), IPAddress = @ip", conn))
                                    {
                                        cmd.Parameters.AddWithValue("@id", pl.Avatar.UserId);
                                        cmd.Parameters.AddWithValue("@avatar", pl.Avatar.SaveToJSON());
                                        cmd.Parameters.AddWithValue("@go", pl.SaveToJSON());
                                        cmd.Parameters.AddWithValue("@ip", pl.Avatar.IPAddress ?? "");
                                        await cmd.ExecuteNonQueryAsync();
                                    }
                                }
                            }
                            break;
                        }
                    case Save.Both:
                        {
                            await this.Save(avatars, Save.Redis);
                            await this.Save(avatars, Save.Mysql);
                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                Error($"DatabaseManager.Save(List<Level>) failed: {ex.Message}");
            }
        }

        public async Task Save(List<Alliance> alliances, Save Save = Logic.Enums.Save.Mysql)
        {
            try
            {
                if (alliances == null || alliances.Count == 0) return;
                switch (Save)
                {
                    case Save.Redis:
                        {
                            if (!Constants.UseCacheServer || Redis.Clans == null) break;
                            foreach (Alliance alliance in alliances)
                            {
                                if (alliance != null)
                                {
                                    Redis.Clans.StringSet(alliance.m_vAllianceId.ToString(), alliance.SaveToJSON(),
                                        TimeSpan.FromDays(366));
                                }
                            }
                            break;
                        }
                    case Save.Mysql:
                        {
                            using (MySqlConnection conn = new MySqlConnection(EnsureConnectionString()))
                            {
                                await conn.OpenAsync();
                                foreach (Alliance alliance in alliances)
                                {
                                    if (alliance == null) continue;
                                    using (MySqlCommand cmd = new MySqlCommand(
                                        "INSERT INTO Clan (ClanId, LastUpdateTime, Data) " +
                                        "VALUES (@id, NOW(), @data) " +
                                        "ON DUPLICATE KEY UPDATE Data = @data, LastUpdateTime = NOW()", conn))
                                    {
                                        cmd.Parameters.AddWithValue("@id", alliance.m_vAllianceId);
                                        cmd.Parameters.AddWithValue("@data", alliance.SaveToJSON());
                                        await cmd.ExecuteNonQueryAsync();
                                    }
                                }
                            }
                            break;
                        }
                    case Save.Both:
                        {
                            await this.Save(alliances, Save.Redis);
                            await this.Save(alliances, Save.Mysql);
                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                Error($"DatabaseManager.Save(List<Alliance>) failed: {ex.Message}");
            }
        }

        public void InitializeTables()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(EnsureConnectionString()))
                {
                    conn.Open();
                    string sql = @"
                        CREATE TABLE IF NOT EXISTS `ClanWar` (
                          `WarId` bigint(20) NOT NULL,
                          `State` int(11) NOT NULL,
                          `WarSize` int(11) NOT NULL,
                          `Clan1Id` bigint(20) NOT NULL,
                          `Clan1Name` varchar(255) NOT NULL,
                          `Clan1Stars` int(11) NOT NULL DEFAULT '0',
                          `Clan1Destruction` double NOT NULL DEFAULT '0',
                          `Clan2Id` bigint(20) NOT NULL,
                          `Clan2Name` varchar(255) NOT NULL,
                          `Clan2Stars` int(11) NOT NULL DEFAULT '0',
                          `Clan2Destruction` double NOT NULL DEFAULT '0',
                          `PreparationStartTime` datetime DEFAULT NULL,
                          `PreparationEndTime` datetime DEFAULT NULL,
                          `BattleStartTime` datetime DEFAULT NULL,
                          `BattleEndTime` datetime DEFAULT NULL,
                          `WarEndedTime` datetime DEFAULT NULL,
                          `Data` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
                          `LastUpdateTime` datetime DEFAULT NULL,
                          PRIMARY KEY (`WarId`),
                          KEY `idx_clan1` (`Clan1Id`),
                          KEY `idx_clan2` (`Clan2Id`),
                          KEY `idx_state` (`State`)
                        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

                        CREATE TABLE IF NOT EXISTS `ClanWarLog` (
                          `LogId` bigint(20) NOT NULL AUTO_INCREMENT,
                          `WarId` bigint(20) NOT NULL,
                          `ClanId` bigint(20) NOT NULL,
                          `OpponentClanId` bigint(20) NOT NULL,
                          `OpponentClanName` varchar(255) NOT NULL,
                          `OpponentClanBadge` int(11) NOT NULL DEFAULT '0',
                          `OpponentClanLevel` int(11) NOT NULL DEFAULT '1',
                          `WarSize` int(11) NOT NULL,
                          `ClanStars` int(11) NOT NULL,
                          `OpponentStars` int(11) NOT NULL,
                          `ClanDestruction` double NOT NULL,
                          `OpponentDestruction` double NOT NULL,
                          `ClanAttacksUsed` int(11) NOT NULL DEFAULT '0',
                          `OpponentAttacksUsed` int(11) NOT NULL DEFAULT '0',
                          `ClanExperienceGained` int(11) NOT NULL DEFAULT '0',
                          `Result` tinyint(4) NOT NULL COMMENT '1=Won, 2=Lost, 3=Draw',
                          `WarEndedTime` datetime NOT NULL,
                          `Data` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
                          PRIMARY KEY (`LogId`),
                          KEY `idx_clan_history` (`ClanId`,`WarEndedTime`),
                          KEY `idx_war` (`WarId`)
                        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

                        CREATE TABLE IF NOT EXISTS `ClanWarAttack` (
                          `AttackId` bigint(20) NOT NULL AUTO_INCREMENT,
                          `WarId` bigint(20) NOT NULL,
                          `AttackerId` bigint(20) NOT NULL,
                          `AttackerName` varchar(255) NOT NULL,
                          `AttackerClanId` bigint(20) NOT NULL,
                          `AttackerMapPosition` int(11) NOT NULL,
                          `DefenderId` bigint(20) NOT NULL,
                          `DefenderName` varchar(255) NOT NULL,
                          `DefenderClanId` bigint(20) NOT NULL,
                          `DefenderMapPosition` int(11) NOT NULL,
                          `Stars` int(11) NOT NULL DEFAULT '0',
                          `DestructionPercentage` int(11) NOT NULL DEFAULT '0',
                          `Duration` int(11) NOT NULL DEFAULT '0',
                          `OrderNumber` int(11) NOT NULL DEFAULT '1',
                          `AttackTime` datetime DEFAULT NULL,
                          PRIMARY KEY (`AttackId`),
                          KEY `idx_war_attacks` (`WarId`),
                          KEY `idx_attacker` (`AttackerId`),
                          KEY `idx_defender` (`DefenderId`)
                        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

                        CREATE TABLE IF NOT EXISTS `ClanWarMember` (
                          `WarId` bigint(20) NOT NULL,
                          `UserId` bigint(20) NOT NULL,
                          `ClanId` bigint(20) NOT NULL,
                          `Name` varchar(255) NOT NULL,
                          `TownHallLevel` int(11) NOT NULL,
                          `MapPosition` int(11) NOT NULL,
                          `AttacksUsed` int(11) NOT NULL DEFAULT '0',
                          `StarsConceded` int(11) NOT NULL DEFAULT '0',
                          `DamageConceded` int(11) NOT NULL DEFAULT '0',
                          `GotAttackedCount` int(11) NOT NULL DEFAULT '0',
                          `GoldGain` int(11) NOT NULL DEFAULT '0',
                          `ElixirGain` int(11) NOT NULL DEFAULT '0',
                          `DarkElixirGain` int(11) NOT NULL DEFAULT '0',
                          PRIMARY KEY (`WarId`,`UserId`),
                          KEY `idx_clan` (`ClanId`)
                        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";

                    using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Error($"DatabaseManager.InitializeTables failed: {ex.Message}");
            }
        }

        public void SaveClanWar(ClanWar war)
        {
            try
            {
                if (war == null) return;

                using (MySqlConnection conn = new MySqlConnection(EnsureConnectionString()))
                {
                    conn.Open();

                    // 1. Upsert ClanWar
                    string sqlWar = @"
                        INSERT INTO ClanWar (
                            WarId, State, WarSize, 
                            Clan1Id, Clan1Name, Clan1Stars, Clan1Destruction,
                            Clan2Id, Clan2Name, Clan2Stars, Clan2Destruction,
                            PreparationStartTime, PreparationEndTime,
                            BattleStartTime, BattleEndTime, WarEndedTime,
                            Data, LastUpdateTime
                        ) VALUES (
                            @wid, @state, @size,
                            @c1id, @c1name, @c1stars, @c1dest,
                            @c2id, @c2name, @c2stars, @c2dest,
                            @pstart, @pend,
                            @bstart, @bend, @wend,
                            @data, NOW()
                        )
                        ON DUPLICATE KEY UPDATE
                            State = @state,
                            WarSize = @size,
                            Clan1Stars = @c1stars,
                            Clan1Destruction = @c1dest,
                            Clan2Stars = @c2stars,
                            Clan2Destruction = @c2dest,
                            PreparationStartTime = @pstart,
                            PreparationEndTime = @pend,
                            BattleStartTime = @bstart,
                            BattleEndTime = @bend,
                            WarEndedTime = @wend,
                            Data = @data,
                            LastUpdateTime = NOW();";

                    using (MySqlCommand cmd = new MySqlCommand(sqlWar, conn))
                    {
                        cmd.Parameters.AddWithValue("@wid", war.WarId);
                        cmd.Parameters.AddWithValue("@state", (int)war.State);
                        cmd.Parameters.AddWithValue("@size", war.WarSize);
                        cmd.Parameters.AddWithValue("@c1id", war.Clan1?.ClanId ?? 0);
                        cmd.Parameters.AddWithValue("@c1name", war.Clan1?.ClanName ?? "");
                        cmd.Parameters.AddWithValue("@c1stars", war.Clan1?.TotalStars ?? 0);
                        cmd.Parameters.AddWithValue("@c1dest", war.Clan1?.TotalDestruction ?? 0);
                        cmd.Parameters.AddWithValue("@c2id", war.Clan2?.ClanId ?? 0);
                        cmd.Parameters.AddWithValue("@c2name", war.Clan2?.ClanName ?? "");
                        cmd.Parameters.AddWithValue("@c2stars", war.Clan2?.TotalStars ?? 0);
                        cmd.Parameters.AddWithValue("@c2dest", war.Clan2?.TotalDestruction ?? 0);

                        cmd.Parameters.AddWithValue("@pstart", war.PreparationStartTime > DateTime.MinValue ? war.PreparationStartTime.ToString("yyyy-MM-dd HH:mm:ss") : (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@pend", war.PreparationEndTime > DateTime.MinValue ? war.PreparationEndTime.ToString("yyyy-MM-dd HH:mm:ss") : (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@bstart", war.BattleStartTime > DateTime.MinValue ? war.BattleStartTime.ToString("yyyy-MM-dd HH:mm:ss") : (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@bend", war.BattleEndTime > DateTime.MinValue ? war.BattleEndTime.ToString("yyyy-MM-dd HH:mm:ss") : (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@wend", war.WarEndedTime > DateTime.MinValue ? war.WarEndedTime.ToString("yyyy-MM-dd HH:mm:ss") : (object)DBNull.Value);

                        cmd.Parameters.AddWithValue("@data", war.SaveToJson().ToString(Newtonsoft.Json.Formatting.None));

                        cmd.ExecuteNonQuery();
                    }

                    // 2. Upsert Members
                    var allMembers = new List<ClanWarMember>();
                    if (war.Clan1?.Members != null) allMembers.AddRange(war.Clan1.Members);
                    if (war.Clan2?.Members != null) allMembers.AddRange(war.Clan2.Members);

                    if (allMembers.Count > 0)
                    {
                        string sqlMem = @"
                            INSERT INTO ClanWarMember (
                                WarId, UserId, ClanId, Name, TownHallLevel, MapPosition,
                                AttacksUsed, StarsConceded, DamageConceded, GotAttackedCount,
                                GoldGain, ElixirGain, DarkElixirGain
                            ) VALUES (
                                @wid, @uid, @cid, @name, @th, @pos,
                                @atks, @stars, @dmg, @gotatks,
                                @gold, @elixir, @dark
                            )
                            ON DUPLICATE KEY UPDATE
                                Name = @name,
                                TownHallLevel = @th,
                                MapPosition = @pos,
                                AttacksUsed = @atks,
                                StarsConceded = @stars,
                                DamageConceded = @dmg,
                                GotAttackedCount = @gotatks,
                                GoldGain = @gold,
                                ElixirGain = @elixir,
                                DarkElixirGain = @dark;";

                        foreach (var m in allMembers)
                        {
                            using (MySqlCommand cmd = new MySqlCommand(sqlMem, conn))
                            {
                                cmd.Parameters.AddWithValue("@wid", war.WarId);
                                cmd.Parameters.AddWithValue("@uid", m.UserId);
                                cmd.Parameters.AddWithValue("@cid", m.ClanId);
                                cmd.Parameters.AddWithValue("@name", m.Name ?? "");
                                cmd.Parameters.AddWithValue("@th", m.TownHallLevel);
                                cmd.Parameters.AddWithValue("@pos", m.MapPosition);
                                cmd.Parameters.AddWithValue("@atks", m.AttacksUsed);
                                cmd.Parameters.AddWithValue("@stars", m.StarsConceded);
                                cmd.Parameters.AddWithValue("@dmg", m.DamageConceded);
                                cmd.Parameters.AddWithValue("@gotatks", m.GotAttackedCount);
                                cmd.Parameters.AddWithValue("@gold", m.GoldGain);
                                cmd.Parameters.AddWithValue("@elixir", m.ElixirGain);
                                cmd.Parameters.AddWithValue("@dark", m.DarkElixirGain);
                                cmd.ExecuteNonQuery();
                            }
                        }
                    }

                    // 3. Upsert Attacks
                    if (war.AttackLogs != null && war.AttackLogs.Count > 0)
                    {
                        string sqlAtk = @"
                            INSERT INTO ClanWarAttack (
                                WarId, AttackerId, AttackerName, AttackerClanId, AttackerMapPosition,
                                DefenderId, DefenderName, DefenderClanId, DefenderMapPosition,
                                Stars, DestructionPercentage, Duration, OrderNumber, AttackTime
                            ) VALUES (
                                @wid, @atkid, @atkname, @atkclan, @atkpos,
                                @defid, @defname, @defclan, @defpos,
                                @stars, @pct, @dur, @ord, @time
                            );";

                        using (MySqlCommand delCmd = new MySqlCommand("DELETE FROM ClanWarAttack WHERE WarId = @wid", conn))
                        {
                            delCmd.Parameters.AddWithValue("@wid", war.WarId);
                            delCmd.ExecuteNonQuery();
                        }

                        foreach (var a in war.AttackLogs)
                        {
                            using (MySqlCommand cmd = new MySqlCommand(sqlAtk, conn))
                            {
                                cmd.Parameters.AddWithValue("@wid", war.WarId);
                                cmd.Parameters.AddWithValue("@atkid", a.AttackerUserId);
                                cmd.Parameters.AddWithValue("@atkname", a.AttackerName ?? "");
                                cmd.Parameters.AddWithValue("@atkclan", a.AttackerClanId);
                                cmd.Parameters.AddWithValue("@atkpos", a.AttackerMapPosition);
                                cmd.Parameters.AddWithValue("@defid", a.DefenderUserId);
                                cmd.Parameters.AddWithValue("@defname", a.DefenderName ?? "");
                                cmd.Parameters.AddWithValue("@defclan", a.DefenderClanId);
                                cmd.Parameters.AddWithValue("@defpos", a.DefenderMapPosition);
                                cmd.Parameters.AddWithValue("@stars", a.Stars);
                                cmd.Parameters.AddWithValue("@pct", a.DestructionPercentage);
                                cmd.Parameters.AddWithValue("@dur", a.Duration);
                                cmd.Parameters.AddWithValue("@ord", a.Order);
                                cmd.Parameters.AddWithValue("@time", a.AttackTime > DateTime.MinValue ? a.AttackTime.ToString("yyyy-MM-dd HH:mm:ss") : DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
                                cmd.ExecuteNonQuery();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Error($"DatabaseManager.SaveClanWar failed for WarId {war?.WarId}: {ex.Message}");
            }
        }

        public void SaveClanWarLog(ClanWar war, long clanId, int result, int xpGained)
        {
            try
            {
                if (war == null) return;
                var homeClan = war.GetClan(clanId) ?? war.Clan1;
                var enemyClan = war.GetOpponentClan(clanId) ?? war.Clan2;
                if (homeClan == null || enemyClan == null) return;

                using (MySqlConnection conn = new MySqlConnection(EnsureConnectionString()))
                {
                    conn.Open();
                    string sql = @"
                        INSERT INTO ClanWarLog (
                            WarId, ClanId, OpponentClanId, OpponentClanName, OpponentClanBadge,
                            OpponentClanLevel, WarSize, ClanStars, OpponentStars, ClanDestruction,
                            OpponentDestruction, ClanAttacksUsed, OpponentAttacksUsed, ClanExperienceGained,
                            Result, WarEndedTime, Data
                        ) VALUES (
                            @wid, @cid, @opid, @opname, @opbadge,
                            @oplevel, @size, @cstars, @opstars, @cdest,
                            @opdest, @catks, @opatks, @xp,
                            @res, @ended, @data
                        );";

                    using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@wid", war.WarId);
                        cmd.Parameters.AddWithValue("@cid", clanId);
                        cmd.Parameters.AddWithValue("@opid", enemyClan.ClanId);
                        cmd.Parameters.AddWithValue("@opname", enemyClan.ClanName ?? "");
                        cmd.Parameters.AddWithValue("@opbadge", enemyClan.ClanBadge);
                        cmd.Parameters.AddWithValue("@oplevel", enemyClan.ClanLevel);
                        cmd.Parameters.AddWithValue("@size", war.WarSize);
                        cmd.Parameters.AddWithValue("@cstars", homeClan.TotalStars);
                        cmd.Parameters.AddWithValue("@opstars", enemyClan.TotalStars);
                        cmd.Parameters.AddWithValue("@cdest", homeClan.TotalDestruction);
                        cmd.Parameters.AddWithValue("@opdest", enemyClan.TotalDestruction);
                        cmd.Parameters.AddWithValue("@catks", homeClan.AttacksUsed);
                        cmd.Parameters.AddWithValue("@opatks", enemyClan.AttacksUsed);
                        cmd.Parameters.AddWithValue("@xp", xpGained);
                        cmd.Parameters.AddWithValue("@res", result);
                        cmd.Parameters.AddWithValue("@ended", war.WarEndedTime > DateTime.MinValue ? war.WarEndedTime.ToString("yyyy-MM-dd HH:mm:ss") : DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
                        cmd.Parameters.AddWithValue("@data", war.SaveToJson().ToString(Newtonsoft.Json.Formatting.None));
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Error($"DatabaseManager.SaveClanWarLog failed for WarId {war?.WarId}, ClanId {clanId}: {ex.Message}");
            }
        }

        public List<ClanWar> GetActiveClanWars()
        {
            List<ClanWar> wars = new List<ClanWar>();
            try
            {
                using (MySqlConnection conn = new MySqlConnection(EnsureConnectionString()))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SELECT Data FROM ClanWar WHERE State != 6", conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string data = reader.GetString(0);
                            if (!string.IsNullOrWhiteSpace(data))
                            {
                                var w = new ClanWar();
                                w.LoadFromJson(JObject.Parse(data));
                                wars.Add(w);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Error($"DatabaseManager.GetActiveClanWars failed: {ex.Message}");
            }
            return wars;
        }

        public List<ClanWar> GetClanWarHistoryFromDb(long clanId)
        {
            List<ClanWar> history = new List<ClanWar>();
            try
            {
                using (MySqlConnection conn = new MySqlConnection(EnsureConnectionString()))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SELECT Data FROM ClanWarLog WHERE ClanId = @cid ORDER BY WarEndedTime DESC LIMIT 20", conn))
                    {
                        cmd.Parameters.AddWithValue("@cid", clanId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string data = reader.GetString(0);
                                if (!string.IsNullOrWhiteSpace(data))
                                {
                                    var w = new ClanWar();
                                    w.LoadFromJson(JObject.Parse(data));
                                    history.Add(w);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Error($"DatabaseManager.GetClanWarHistoryFromDb failed for ClanId {clanId}: {ex.Message}");
            }
            return history;
        }
    }
}
