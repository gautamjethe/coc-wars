using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CSS.Files;
using CSS.Logic;
using Timer = System.Threading.Timer;
using static CSS.Core.Logger;
using CSS.Logic.Enums;
using CSS.Core.Settings;

namespace CSS.Core
{
    internal class ObjectManager : IDisposable
    {
        private static long m_vAllianceSeed;
        private static long m_vAvatarSeed;
        public static int m_vDonationSeed;
        private static int m_vRandomBaseAmount;
        private static DatabaseManager m_vDatabase;
        private static string m_vHomeDefault;
        public static bool m_vTimerCanceled;
        public static Timer TimerReferenceRedis;
        public static Timer TimerReferenceMysql;
        public static Dictionary<int, string> NpcLevels;
        public static Dictionary<int, string> m_vRandomBases;
        public static FingerPrint FingerPrint;
        static long MaxPlayerID;
        static long MaxAllianceID;

        public ObjectManager()
        {
            m_vTimerCanceled       = false;

            m_vDatabase            = new DatabaseManager();

            NpcLevels              = new Dictionary<int, string>();
            m_vRandomBases         = new Dictionary<int, string>();
            FingerPrint            = new FingerPrint();

            MaxPlayerID            = m_vDatabase.GetMaxPlayerId() + 1;
            MaxAllianceID          = m_vDatabase.GetMaxAllianceId() + 1;

            m_vAvatarSeed          = MaxPlayerID;
            m_vAllianceSeed        = MaxAllianceID;

            using (StreamReader sr = new StreamReader(@"Gamefiles/starting_home.json"))
            {
                m_vHomeDefault     = sr.ReadToEnd();
            }

            LoadNpcLevels();
            LoadRandomBase();

            TimerReferenceRedis = new Timer(SaveRedis, null, 10000, 40000);
            TimerReferenceMysql = new Timer(SaveMysql, null, 40000, 27000);
            Say($"CSS Database has been succesfully loaded. ({MaxAllianceID + MaxPlayerID} Tables)");
        }

        private static void SaveRedis(object state)
        {
            if (!Constants.UseCacheServer) return;
            m_vDatabase.Save(ResourcesManager.m_vInMemoryLevels.Values.ToList(), Save.Redis);
            m_vDatabase.Save(ResourcesManager.GetInMemoryAlliances(), Save.Redis);
        }
        private static async void SaveMysql(object state)
        {
            m_vDatabase.Save(ResourcesManager.m_vInMemoryLevels.Values.ToList(), Save.Mysql).Wait();
            m_vDatabase.Save(ResourcesManager.GetInMemoryAlliances(), Save.Mysql).Wait();
        }

        public static Alliance CreateAlliance(long seed)
        {
            Alliance alliance;
            if (seed == 0)
            {
                seed = m_vAllianceSeed;
            }
            alliance = new Alliance(seed);
            m_vAllianceSeed++;
            m_vDatabase.CreateAlliance(alliance);
            ResourcesManager.AddAllianceInMemory(alliance);
            return alliance;
        }

        public static Level CreateAvatar(long seed, string token)
        {
            Level pl;
            if (seed == 0)
            {
                seed = m_vAvatarSeed;
            }
            pl = new Level(seed, token);
            if (seed >= m_vAvatarSeed)
            {
                m_vAvatarSeed = seed + 1;
            }
            else
            {
                m_vAvatarSeed++;
            }
            pl.LoadFromJSON(m_vHomeDefault);
            m_vDatabase.CreateAccount(pl);
            return pl;
        }

        /*public static void LoadAllAlliancesFromDB()
        {
            ResourcesManager.AddAllianceInMemory(m_vDatabase.GetAllAlliances());
        }*/

        public static Alliance GetAlliance(long allianceId)
        {
            Alliance alliance;
            if (ResourcesManager.InMemoryAlliancesContain(allianceId))
            {
                return ResourcesManager.GetInMemoryAlliance(allianceId);
            }
            else
            {
                alliance = m_vDatabase.GetAlliance(allianceId);
                if (alliance != null)
                    ResourcesManager.AddAllianceInMemory(alliance);
                else
                    return null;
                return alliance;
            }
        }

        public static List<Alliance> GetInMemoryAlliances() => ResourcesManager.GetInMemoryAlliances();

        public static Level GetRandomOnlinePlayer()
        {
            if (ResourcesManager.m_vInMemoryLevels.Count == 0) return null;
            int index = new Random().Next(0, ResourcesManager.m_vInMemoryLevels.Count);
            return ResourcesManager.m_vInMemoryLevels.Values.ToList().ElementAt(index);
        }

        public static async Task<Level> GetRandomOpponent(long attackerId)
        {
            try
            {
                Random rnd = new Random();

                // 1. Fetch all registered players from database
                List<long> dbPlayerIds = m_vDatabase != null ? m_vDatabase.GetAllPlayerIds() : new List<long>();
                dbPlayerIds.RemoveAll(id => id == attackerId);

                // 2. Also check in-memory players excluding attacker
                List<Level> inMemoryOpponents = ResourcesManager.m_vInMemoryLevels.Values
                    .Where(p => p != null && p.Avatar != null && p.Avatar.UserId != attackerId)
                    .ToList();

                // 3. If other database players exist, pick one at random regardless of trophies/TH/level
                if (dbPlayerIds.Count > 0)
                {
                    dbPlayerIds = dbPlayerIds.OrderBy(_ => rnd.Next()).ToList();
                    foreach (long candidateId in dbPlayerIds.Take(5))
                    {
                        Level opponent = await ResourcesManager.GetPlayer(candidateId);
                        if (opponent != null && opponent.Avatar != null && opponent.Avatar.UserId != attackerId)
                        {
                            return opponent;
                        }
                    }
                }

                // 4. Fallback to in-memory opponent if available
                if (inMemoryOpponents.Count > 0)
                {
                    return inMemoryOpponents[rnd.Next(inMemoryOpponents.Count)];
                }

                // 5. Fallback to pre-generated PVP bases (e.g. if single player on server)
                return GetRandomPvpBase(attackerId);
            }
            catch (Exception ex)
            {
                Error($"GetRandomOpponent error: {ex.Message}");
                return GetRandomPvpBase(attackerId);
            }
        }

        public static Level GetRandomPvpBase(long attackerId)
        {
            if (m_vRandomBases == null || m_vRandomBases.Count == 0)
            {
                LoadRandomBase();
            }
            if (m_vRandomBases == null || m_vRandomBases.Count == 0)
            {
                return null;
            }

            Random rnd = new Random();
            int idx = rnd.Next(0, m_vRandomBases.Count);
            string baseJson = m_vRandomBases[idx];

            Level defender = new Level();
            defender.Avatar.UserId = 9000000 + idx;
            defender.Avatar.SetName("Goblin Chief #" + (idx + 1));
            defender.Avatar.SetScore(rnd.Next(400, 3200));
            defender.Avatar.SetTownHallLevel(rnd.Next(6, 11));
            defender.Avatar.AllianceId = 0;
            defender.Avatar.LastTickSaved = DateTime.UtcNow;
            defender.LoadFromJSON(baseJson);
            return defender;
        }

        public static void LoadNpcLevels()
        {
            int Count = 0;
            NpcLevels.Add(17000000, new StreamReader(@"Gamefiles/level/NPC/tutorial_npc.json").ReadToEnd());
            NpcLevels.Add(17000001, new StreamReader(@"Gamefiles/level/NPC/tutorial_npc2.json").ReadToEnd());

            for (int i = 2; i < 50; i++)
            {
                using (StreamReader sr = new StreamReader(@"Gamefiles/level/NPC/level" + (i + 1) + ".json"))
                {
                    NpcLevels.Add(i + 17000000, sr.ReadToEnd());
                }
                Count++;
            }

            Say($"NPC Levels  have been succesfully loaded. ({Count})");
        }

        public static void LoadRandomBase()
        {
            try
            {
                string pvpDir = @"Gamefiles/level/PVP";
                if (!Directory.Exists(pvpDir) && Directory.Exists(@"bin/Debug/Gamefiles/level/PVP"))
                {
                    pvpDir = @"bin/Debug/Gamefiles/level/PVP";
                }
                if (Directory.Exists(pvpDir))
                {
                    string[] files = Directory.GetFiles(pvpDir, "Base*.json");
                    m_vRandomBaseAmount = files.Length;
                    m_vRandomBases.Clear();
                    for (int i = 0; i < files.Length; i++)
                    {
                        using (StreamReader sr2 = new StreamReader(files[i]))
                        {
                            m_vRandomBases.Add(i, sr2.ReadToEnd());
                        }
                    }
                    Say($"PVP Levels  have been succesfully loaded. ({m_vRandomBases.Count})");
                }
            }
            catch (Exception ex)
            {
                Error($"Failed to load PVP levels: {ex.Message}");
            }
        }

        public static void RemoveInMemoryAlliance(long id)
        {
            ResourcesManager.RemoveAllianceFromMemory(id);
        }

        public static long GetMaxAllianceID() => MaxAllianceID;

        public static long GetMaxPlayerID() => MaxPlayerID;

        public static int RandomBaseCount() => m_vRandomBaseAmount;

        public void Dispose()
        {
            if (TimerReferenceRedis != null && TimerReferenceMysql != null)
            {
                TimerReferenceRedis.Dispose();
                TimerReferenceMysql.Dispose();
                TimerReferenceRedis = null;
                TimerReferenceMysql = null;
            }
        }

    }
}
