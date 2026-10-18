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

namespace CSS.Core
{
    internal class DatabaseManager 
    {
        internal string Mysql;

        private string EnsureConnectionString()
        {
            if (string.IsNullOrEmpty(this.Mysql))
            {
                MySqlConnectionStringBuilder builder = new MySqlConnectionStringBuilder()
                {
                    Server = Utils.ParseConfigString("MysqlIPAddress"),
                    UserID = Utils.ParseConfigString("MysqlUsername"),
                    Port = (uint)Utils.ParseConfigInt("MysqlPort"),
                    Pooling = false,
                    Database = Utils.ParseConfigString("MysqlDatabase"),
                    MinimumPoolSize = 1,
                    CharacterSet = "utf8mb4",
                    ConvertZeroDateTime = true
                };

                if (!string.IsNullOrWhiteSpace(Utils.ParseConfigString("MysqlPassword")))
                {
                    builder.Password = Utils.ParseConfigString("MysqlPassword");
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
            const string SQL = "SELECT coalesce(MAX(ClanId), 0) FROM Clan";
            int Seed = -1;

            using (MySqlConnection Conn = new MySqlConnection(EnsureConnectionString()))
            {
                Conn.Open();

                using (MySqlCommand CMD = new MySqlCommand(SQL, Conn))
                {
                    CMD.Prepare();
                    Seed = Convert.ToInt32(CMD.ExecuteScalar());
                }
            }

            return Seed;
        }

        public long GetMaxPlayerId()
        {
            try
            {
                const string SQL = "SELECT coalesce(MAX(PlayerId), 0) FROM Player";
                int Seed = -1;

                using (MySqlConnection Conn = new MySqlConnection(EnsureConnectionString()))
                {
                    Conn.Open();

                    using (MySqlCommand CMD = new MySqlCommand(SQL, Conn))
                    {
                        CMD.Prepare();
                        Seed = Convert.ToInt32(CMD.ExecuteScalar());
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
                Console.ReadKey();
                CSSControl.CSSRestart();
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
    }
}
