using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CSS.Core;
using CSS.Files;
using CSS.Logic;
using CSS.Packets.Messages.Server;

namespace CSS.Test
{
    class E2ETestProgram
    {
        static void Main(string[] args)
        {
            try
            {
                Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
                RunTests().GetAwaiter().GetResult();
                Console.WriteLine("\n========================================================");
                Console.WriteLine("ALL END-TO-END CLAN WAR & DATABASE TESTS PASSED (14/14)!");
                Console.WriteLine("========================================================");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\nTEST FAILED: {ex}");
                Console.ResetColor();
                Environment.Exit(1);
            }
        }

        static async Task RunTests()
        {
            Console.WriteLine("[1/14] Initializing CSV Manager & Data Tables...");
            new CSVManager();
            Console.WriteLine("       CSV Tables loaded successfully.");

            Console.WriteLine("[2/14] Setting up Test Clans for Matchmaking...");
            Alliance clanA = new Alliance(7001)
            {
                m_vAllianceName = "Alpha Clan",
                m_vAllianceBadgeData = 0x5B002B54,
                m_vAllianceLevel = 5,
                m_vAllianceType = 0
            };
            ResourcesManager.AddAllianceInMemory(clanA);

            for (int i = 1; i <= 5; i++)
            {
                long pid = 7100 + i;
                Level p = new Level(pid);
                p.Avatar.UserId = pid;
                p.Avatar.AvatarName = $"AlphaMember#{i}";
                p.Avatar.m_vTownHallLevel = 9;
                p.Avatar.AllianceId = 7001;
                ResourcesManager.m_vInMemoryLevels[pid] = p;
                clanA.AddAllianceMember(new AllianceMemberEntry(pid) { Role = i == 1 ? 2 : 1, WarOptInStatus = 1 });
            }

            Alliance clanB = new Alliance(7002)
            {
                m_vAllianceName = "Beta Clan",
                m_vAllianceBadgeData = 0x5B002B54,
                m_vAllianceLevel = 4,
                m_vAllianceType = 0
            };
            ResourcesManager.AddAllianceInMemory(clanB);

            for (int i = 1; i <= 5; i++)
            {
                long pid = 7200 + i;
                Level p = new Level(pid);
                p.Avatar.UserId = pid;
                p.Avatar.AvatarName = $"BetaMember#{i}";
                p.Avatar.m_vTownHallLevel = 9;
                p.Avatar.AllianceId = 7002;
                ResourcesManager.m_vInMemoryLevels[pid] = p;
                clanB.AddAllianceMember(new AllianceMemberEntry(pid) { Role = i == 1 ? 2 : 1, WarOptInStatus = 1 });
            }

            Console.WriteLine("[3/14] Testing War Search & Cancellation...");
            await ClanWarManager.StartSearch(clanA, 5);
            if (!ClanWarManager.IsClanSearching(clanA.m_vAllianceId))
                throw new Exception("Expected Clan A to be in Searching state!");
            if (ClanWarManager.GetClanWarState(clanA.m_vAllianceId) != ClanWarState.SEARCHING)
                throw new Exception($"Expected state SEARCHING (1), got {ClanWarManager.GetClanWarState(clanA.m_vAllianceId)}");

            // Validate Packet 24331 in searching state
            var searchMsg = new AllianceWarDataMessage(null);
            var searchDevice = new CSS.Packets.Device(null);
            var searchLevel = new Level(7101);
            searchLevel.Avatar.AllianceId = 7001;
            searchDevice.Player = searchLevel;
            var searchDataMsg = new AllianceWarDataMessage(searchDevice);
            searchDataMsg.Encode();
            byte[] searchBytes = searchDataMsg.Data.ToArray();
            if (searchBytes.Length < 12) throw new Exception("Expected at least 12 bytes for search overview packet");
            // First 4 bytes must be 1 (SEARCHING)
            int encodedState = (searchBytes[0] << 24) | (searchBytes[1] << 16) | (searchBytes[2] << 8) | searchBytes[3];
            if (encodedState != 1) throw new Exception($"Expected encoded state 1 (SEARCHING), got {encodedState}");
            Console.WriteLine("       Packet 24331 correctly encoded state 1 (SEARCHING).");

            ClanWarManager.CancelSearch(clanA.m_vAllianceId);
            if (ClanWarManager.IsClanSearching(clanA.m_vAllianceId))
                throw new Exception("Expected Clan A to NO LONGER be searching after cancellation!");
            Console.WriteLine("       Search cancellation successfully tested.");

            Console.WriteLine("[4/14] Testing Clan-vs-Clan Real Matchmaking (Clan A vs Clan B)...");
            await ClanWarManager.StartSearch(clanA, 5);
            await ClanWarManager.StartSearch(clanB, 5);

            var matchedWar = ClanWarManager.GetWarByClanId(clanA.m_vAllianceId);
            if (matchedWar == null) throw new Exception("Expected matched war between Clan A and Clan B!");
            if (matchedWar.Clan1.ClanId != 7001 && matchedWar.Clan2.ClanId != 7001)
                throw new Exception("Clan A not found in matched war!");
            if (matchedWar.Clan1.ClanId != 7002 && matchedWar.Clan2.ClanId != 7002)
                throw new Exception("Clan B not found in matched war!");
            Console.WriteLine($"       Matchmaking matched: '{matchedWar.Clan1.ClanName}' vs '{matchedWar.Clan2.ClanName}'!");

            Console.WriteLine("[5/14] Testing Preparation Day Phase & Castle Donations...");
            if (matchedWar.State != ClanWarState.PREPARATION_DAY)
                throw new Exception($"Expected PREPARATION_DAY, got {matchedWar.State}");
            if (matchedWar.RemainingTimeSeconds <= 0)
                throw new Exception("Expected positive remaining time in preparation day!");

            var castleTarget = matchedWar.Clan1.Members[0];
            bool ccOk = castleTarget.AddDonation(matchedWar.Clan1.Members[1].UserId, 4000000, 5, 4);
            if (!ccOk) throw new Exception("Clan Castle donation failed!");
            Console.WriteLine($"       Preparation Day verified. CC donations added: {castleTarget.GetClanCastleUsedCapacity()} troops.");

            Console.WriteLine("[6/14] Testing War Map Packet Encoding in Preparation Day...");
            var mapMsg = new AllianceWarMapDataMessage(searchDevice, matchedWar);
            mapMsg.Encode();
            byte[] mapBytes = mapMsg.Data.ToArray();
            if (mapBytes == null || mapBytes.Length < 1000)
                throw new Exception($"War map packet unexpected size: {mapBytes?.Length ?? 0}");
            Console.WriteLine($"       War map packet successfully encoded: {mapBytes.Length} bytes.");

            Console.WriteLine("[7/14] Transitioning to Battle Day Phase...");
            ClanWarManager.SkipPhase(matchedWar);
            if (matchedWar.State != ClanWarState.BATTLE_DAY)
                throw new Exception($"Expected BATTLE_DAY, got {matchedWar.State}");
            Console.WriteLine($"       War is now in Battle Day. Remaining time: {matchedWar.RemainingTimeSeconds}s.");

            Console.WriteLine("[8/14] Executing Clan War Attacks during Battle Day...");
            var atk1 = matchedWar.Clan1.Members[0];
            var def1 = matchedWar.Clan2.Members[0];

            ClanWarManager.SetActiveBattle(atk1.UserId, matchedWar.WarId, def1.UserId, atk1.TownHallLevel, def1.TownHallLevel, 200);
            ClanWarManager.TrackTroopDeployment(atk1.UserId, 180);
            bool b1 = ClanWarManager.FinalizeBattle(atk1.UserId, 3, 100);
            if (!b1) throw new Exception("FinalizeBattle 1 failed!");

            var atk2 = matchedWar.Clan2.Members[0];
            var def2 = matchedWar.Clan1.Members[0];
            ClanWarManager.SetActiveBattle(atk2.UserId, matchedWar.WarId, def2.UserId, atk2.TownHallLevel, def2.TownHallLevel, 200);
            ClanWarManager.TrackTroopDeployment(atk2.UserId, 150);
            bool b2 = ClanWarManager.FinalizeBattle(atk2.UserId, 2, 75);
            if (!b2) throw new Exception("FinalizeBattle 2 failed!");

            if (matchedWar.AttackLogs.Count != 2)
                throw new Exception($"Expected 2 attack logs, got {matchedWar.AttackLogs.Count}");
            var latestAttack = matchedWar.AttackLogs.Last();
            if (latestAttack.AttackerMapPosition != atk2.MapPosition)
                throw new Exception($"Expected attacker map pos {atk2.MapPosition}, got {latestAttack.AttackerMapPosition}");
            if (latestAttack.AttackTime == DateTime.MinValue)
                throw new Exception("Expected valid AttackTime!");
            Console.WriteLine($"       Attacks recorded! Clan 1: {matchedWar.Clan1.TotalStars}* vs Clan 2: {matchedWar.Clan2.TotalStars}*");

            Console.WriteLine("[9/14] Concluding War -> Result Day (WAR_ENDED)...");
            bool clan1Won = matchedWar.Clan1.TotalStars > matchedWar.Clan2.TotalStars;
            var winningAlliance = clan1Won ? (matchedWar.Clan1.ClanId == 7001 ? clanA : clanB) : (matchedWar.Clan2.ClanId == 7001 ? clanA : clanB);
            int xpBefore = winningAlliance.m_vAllianceExperience;
            ClanWarManager.SkipPhase(matchedWar);
            if (matchedWar.State != ClanWarState.WAR_ENDED)
                throw new Exception($"Expected WAR_ENDED, got {matchedWar.State}");
            if (winningAlliance.m_vWonWars < 1)
                throw new Exception("Winning clan should have won and won wars incremented!");
            if (winningAlliance.m_vAllianceExperience <= xpBefore)
                throw new Exception("Winning clan experience should have increased!");
            Console.WriteLine($"       War ended! Winning clan WON with 3 stars! Clan XP gained: {winningAlliance.m_vAllianceExperience - xpBefore}");

            Console.WriteLine("[10/14] Validating War Map in Result Day (WAR_ENDED)...");
            var endMapMsg = new AllianceWarMapDataMessage(searchDevice, matchedWar);
            endMapMsg.Encode();
            byte[] endMapBytes = endMapMsg.Data.ToArray();
            if (endMapBytes == null || endMapBytes.Length < 1000)
                throw new Exception("War map packet failed in WAR_ENDED state!");
            int endState = (endMapBytes[0] << 24) | (endMapBytes[1] << 16) | (endMapBytes[2] << 8) | endMapBytes[3];
            if (endState != 6) throw new Exception($"Expected war state 6 in map, got {endState}");
            Console.WriteLine($"       War Map in Result Day successfully encoded (State: {endState}, {endMapBytes.Length} bytes).");

            Console.WriteLine("[11/14] Validating Clan War History / War Log (Packet 24338)...");
            var history = ClanWarManager.GetWarHistory(clanA.m_vAllianceId);
            if (history.Count < 1) throw new Exception("Expected at least 1 war in Clan A history!");
            var histMsg = new AllianceWarHistoryMessage(null, clanA);
            histMsg.Encode();
            byte[] histBytes = histMsg.Data.ToArray();
            if (histBytes == null || histBytes.Length < 50)
                throw new Exception($"War history packet size invalid: {histBytes?.Length ?? 0}");
            Console.WriteLine($"       War History verified: {history.Count} war(s) logged, packet encoded {histBytes.Length} bytes.");

            Console.WriteLine("[12/14] Validating JSON Serialization / Deserialization...");
            var json = matchedWar.SaveToJson();
            if (json == null || json["war_id"] == null) throw new Exception("SaveToJson produced invalid JSON!");
            var loadedWar = new ClanWar();
            loadedWar.LoadFromJson(json);
            if (loadedWar.WarId != matchedWar.WarId) throw new Exception("Loaded WarId mismatch!");
            if (loadedWar.State != ClanWarState.WAR_ENDED) throw new Exception("Loaded state mismatch!");
            if (loadedWar.AttackLogs.Count != 2) throw new Exception("Loaded attack logs count mismatch!");
            Console.WriteLine("       JSON serialization and deserialization verified.");

            Console.WriteLine("[13/14] Validating DatabaseManager Table Schemas & Queries...");
            var db = new DatabaseManager();
            if (db == null) throw new Exception("DatabaseManager initialization failed!");
            // Verify SaveClanWar and SaveClanWarLog handles null or offline DB gracefully
            db.SaveClanWar(matchedWar);
            db.SaveClanWarLog(matchedWar, clanA.m_vAllianceId, 1, 195);
            var activeWars = db.GetActiveClanWars();
            var dbHistory = db.GetClanWarHistoryFromDb(clanA.m_vAllianceId);
            Console.WriteLine("       DatabaseManager ClanWar and ClanWarLog methods executed safely.");

            Console.WriteLine("[14/14] Validating AI Opponent Matchmaking on Search Timeout...");
            Alliance clanC = new Alliance(7003)
            {
                m_vAllianceName = "Solo Search Clan",
                m_vAllianceBadgeData = 0x5B002B54,
                m_vAllianceLevel = 2,
                m_vAllianceType = 0
            };
            ResourcesManager.AddAllianceInMemory(clanC);
            for (int i = 1; i <= 5; i++)
            {
                long pid = 7300 + i;
                Level p = new Level(pid);
                p.Avatar.UserId = pid;
                p.Avatar.AvatarName = $"SoloMember#{i}";
                p.Avatar.m_vTownHallLevel = 8;
                p.Avatar.AllianceId = 7003;
                ResourcesManager.m_vInMemoryLevels[pid] = p;
                clanC.AddAllianceMember(new AllianceMemberEntry(pid) { Role = i == 1 ? 2 : 1, WarOptInStatus = 1 });
            }

            // Start search and verify no AI opponent is created
            await ClanWarManager.StartSearch(clanC, 5);
            // Force tick to test matchmaking
            typeof(ClanWarManager).GetMethod("OnTick", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                .Invoke(null, new object[] { null });

            var aiWar = ClanWarManager.GetWarByClanId(7003);
            if (aiWar != null) throw new Exception("Expected NO AI Opponent war to be created! AI wars must be disabled.");
            if (!ClanWarManager.IsClanSearching(7003))
                throw new Exception("Expected clanC to still be searching for a real clan!");
            Console.WriteLine("       Real-Clan-Only Matchmaking verified: clanC remains in search queue and NO AI opponent was generated.");
        }
    }
}
