using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CSS.Core;
using CSS.Files;
using CSS.Files.Logic;
using CSS.Logic;
using CSS.Packets;
using CSS.Packets.Messages.Server;

namespace CSS.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
                RunTests().GetAwaiter().GetResult();
                Console.WriteLine("\n==========================================");
                Console.WriteLine("ALL CLAN WAR TESTS PASSED SUCCESSFULLY! (10/10)");
                Console.WriteLine("==========================================");
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
            Console.WriteLine("[1/10] Initializing CSV Manager & Data Tables...");
            new CSVManager();
            Console.WriteLine("       CSV Tables loaded successfully.");

            Console.WriteLine("[2/10] Creating Test Alliance & Members...");
            Alliance alliance = new Alliance(1001)
            {
                m_vAllianceName = "Test Warriors",
                m_vAllianceBadgeData = 0x5B002B54,
                m_vAllianceLevel = 3,
                m_vAllianceType = 0
            };
            ResourcesManager.AddAllianceInMemory(alliance);

            for (int i = 1; i <= 5; i++)
            {
                long playerId = 2000 + i;
                Level p = new Level(playerId);
                p.Avatar.UserId = playerId;
                p.Avatar.AvatarName = $"Member #{i}";
                p.Avatar.m_vTownHallLevel = 8 + (i % 3);
                p.Avatar.AllianceId = 1001;
                ResourcesManager.m_vInMemoryLevels[playerId] = p;

                var memberEntry = new AllianceMemberEntry(playerId)
                {
                    Role = i == 1 ? 2 : 1, // Leader
                    WarOptInStatus = 1
                };
                alliance.AddAllianceMember(memberEntry);
            }
            Console.WriteLine($"       Alliance '{alliance.m_vAllianceName}' created with {alliance.GetAllianceMembers().Count} members.");

            Console.WriteLine("[3/10] Starting 5v5 Clan War with AI Opponent...");
            ClanWar war = await ClanWarManager.StartWar(alliance, null, 5);
            if (war == null) throw new Exception("ClanWarManager.StartWar returned null!");
            if (war.State != ClanWarState.PREPARATION_DAY) throw new Exception($"Expected PREPARATION_DAY (4), got {war.State}");
            if (war.WarSize != 5) throw new Exception($"Expected WarSize 5, got {war.WarSize}");
            if (war.Clan1.Members.Count != 5) throw new Exception($"Expected 5 Clan1 members, got {war.Clan1.Members.Count}");
            if (war.Clan2.Members.Count != 5) throw new Exception($"Expected 5 Clan2 members, got {war.Clan2.Members.Count}");
            Console.WriteLine($"       Clan War #{war.WarId} started: '{war.Clan1.ClanName}' vs '{war.Clan2.ClanName}'");

            Console.WriteLine("[4/10] Validating Packet 24331 (AllianceWarDataMessage Overview)...");
            var overviewMsg = new AllianceWarDataMessage(null, war);
            overviewMsg.Encode();
            byte[] overviewBytes = overviewMsg.Data.ToArray();
            if (overviewBytes == null || overviewBytes.Length == 0) throw new Exception("Overview packet produced empty bytes!");
            Console.WriteLine($"       Overview packet encoded: {overviewBytes.Length} bytes.");

            Console.WriteLine("[5/10] Validating Packet 24335 (AllianceWarMapDataMessage Full Map)...");
            var mapMsg = new AllianceWarMapDataMessage(null, war);
            mapMsg.Encode();
            byte[] mapBytes = mapMsg.Data.ToArray();
            if (mapBytes == null || mapBytes.Length < 1000) throw new Exception($"Map packet length unexpected: {mapBytes?.Length ?? 0} bytes!");
            Console.WriteLine($"       Map packet encoded: {mapBytes.Length} bytes.");

            Console.WriteLine("[6/10] Testing War Clan Castle Donation in Preparation Day...");
            var targetMember = war.Clan1.Members[0];
            int initialCcSpace = targetMember.GetClanCastleUsedCapacity();
            // Data ID 4000000 = Barbarian (housing space 1)
            bool donated = targetMember.AddDonation(war.Clan1.Members[1].UserId, 4000000, 5, 4);
            if (!donated) throw new Exception("AddDonation failed!");
            int newCcSpace = targetMember.GetClanCastleUsedCapacity();
            if (newCcSpace != initialCcSpace + 5) throw new Exception($"Expected CC space {initialCcSpace + 5}, got {newCcSpace}");
            Console.WriteLine($"       Donated 5 Barbarians to CC: Used {newCcSpace}/{targetMember.ClanCastleMaxCapacity} capacity.");

            Console.WriteLine("[7/10] Transitioning Phase (Skipping to Battle Day)...");
            ClanWarManager.SkipPhase(war);
            if (war.State != ClanWarState.BATTLE_DAY) throw new Exception($"Expected BATTLE_DAY (5), got {war.State}");
            Console.WriteLine($"       War State is now {war.State}. Battle Day is active!");

            Console.WriteLine("[8/10] Testing Attack Execution & Scoring...");
            var attacker = war.Clan1.Members[0];
            var defender = war.Clan2.Members[0];

            ClanWarManager.SetActiveBattle(attacker.UserId, war.WarId, defender.UserId, attacker.TownHallLevel, defender.TownHallLevel, 200);
            ClanWarManager.TrackTroopDeployment(attacker.UserId, 150);
            bool attackResult = ClanWarManager.FinalizeBattle(attacker.UserId, 3, 100);
            if (!attackResult) throw new Exception("FinalizeBattle failed!");
            if (attacker.AttacksUsed != 1) throw new Exception($"Expected AttacksUsed 1, got {attacker.AttacksUsed}");
            if (defender.StarsConceded != 3) throw new Exception($"Expected defender StarsConceded 3, got {defender.StarsConceded}");
            if (defender.DamageConceded != 100) throw new Exception($"Expected defender DamageConceded 100, got {defender.DamageConceded}");
            if (war.Clan1.TotalStars < 3) throw new Exception($"Expected Clan1 stars >= 3, got {war.Clan1.TotalStars}");
            if (war.AttackLogs.Count != 1) throw new Exception($"Expected 1 attack log, got {war.AttackLogs.Count}");
            Console.WriteLine($"       Attack successful! Recorded: 3 Stars, 100% Destruction. Clan Stars: {war.Clan1.TotalStars}*");

            Console.WriteLine("[9/10] Ending War & Distributing Rewards...");
            int initialClanXp = alliance.m_vAllianceExperience;
            ClanWarManager.SkipPhase(war);
            if (war.State != ClanWarState.WAR_ENDED) throw new Exception($"Expected WAR_ENDED (6), got {war.State}");
            if (alliance.m_vWonWars < 1) throw new Exception("Expected WonWars to be incremented!");
            if (alliance.m_vAllianceExperience <= initialClanXp) throw new Exception("Expected AllianceExperience to increase!");
            Console.WriteLine($"       War concluded: '{war.Clan1.ClanName}' WON! Clan XP: {alliance.m_vAllianceExperience} (+{alliance.m_vAllianceExperience - initialClanXp})");

            Console.WriteLine("[10/12] Validating Packet 24338 (AllianceWarHistoryMessage)...");
            var historyMsg = new AllianceWarHistoryMessage(null, alliance);
            historyMsg.Encode();
            byte[] historyBytes = historyMsg.Data.ToArray();
            if (historyBytes == null || historyBytes.Length == 0) throw new Exception("History packet produced empty bytes!");
            var historyList = ClanWarManager.GetWarHistory(alliance.m_vAllianceId);
            if (historyList.Count < 1) throw new Exception($"Expected at least 1 war in history, got {historyList.Count}");
            Console.WriteLine($"       History packet encoded: {historyBytes.Length} bytes with {historyList.Count} completed war(s).");

            Console.WriteLine("[11/12] Testing Solo Player Dummy Generation in 5v5 War...");
            Alliance soloAlliance = new Alliance(5555)
            {
                m_vAllianceName = "Solo Champion Clan",
                m_vAllianceBadgeData = 0x5B002B54,
                m_vAllianceLevel = 6,
                m_vAllianceType = 0
            };
            ResourcesManager.AddAllianceInMemory(soloAlliance);
            long soloPlayerId = 8888001;
            Level soloPlayer = new Level(soloPlayerId);
            soloPlayer.Avatar.UserId = soloPlayerId;
            soloPlayer.Avatar.AvatarName = "SoloLeader";
            soloPlayer.Avatar.m_vTownHallLevel = 10;
            soloPlayer.Avatar.AllianceId = 5555;
            ResourcesManager.m_vInMemoryLevels[soloPlayerId] = soloPlayer;
            soloAlliance.AddAllianceMember(new AllianceMemberEntry(soloPlayerId) { Role = 2, WarOptInStatus = 1 });

            if (soloAlliance.GetAllianceMembers().Count != 1)
                throw new Exception("Expected exactly 1 member in solo alliance before war!");

            ClanWar soloWar = await ClanWarManager.StartWar(soloAlliance, null, 5);
            if (soloWar == null) throw new Exception("StartWar failed for solo clan!");
            if (soloWar.Clan1.Members.Count != 5) throw new Exception($"Expected 5 Clan1 members, got {soloWar.Clan1.Members.Count}");
            if (soloWar.Clan2.Members.Count != 5) throw new Exception($"Expected 5 Clan2 members, got {soloWar.Clan2.Members.Count}");
            if (soloAlliance.GetAllianceMembers().Count != 5)
                throw new Exception($"Expected soloAlliance to now have 5 members, got {soloAlliance.GetAllianceMembers().Count}");

            Alliance aiAlliance = ObjectManager.GetAlliance(soloWar.Clan2.ClanId);
            if (aiAlliance == null) throw new Exception("AI Alliance not found in ObjectManager!");
            if (aiAlliance.GetAllianceMembers().Count != 5)
                throw new Exception($"Expected AI Alliance to have 5 members, got {aiAlliance.GetAllianceMembers().Count}");

            // Verify all 5 Clan 1 members resolve via ResourcesManager.GetPlayer
            foreach (var m in soloWar.Clan1.Members)
            {
                Level p = await ResourcesManager.GetPlayer(m.UserId);
                if (p == null) throw new Exception($"Failed to resolve Clan1 member {m.UserId} ({m.Name}) from ResourcesManager!");
                if (string.IsNullOrEmpty(p.Avatar.AvatarName)) throw new Exception($"Clan1 member {m.UserId} has empty AvatarName!");
            }

            // Verify all 5 Clan 2 members resolve via ResourcesManager.GetPlayer
            foreach (var m in soloWar.Clan2.Members)
            {
                Level p = await ResourcesManager.GetPlayer(m.UserId);
                if (p == null) throw new Exception($"Failed to resolve Clan2 member {m.UserId} ({m.Name}) from ResourcesManager!");
                if (string.IsNullOrEmpty(p.Avatar.AvatarName)) throw new Exception($"Clan2 member {m.UserId} has empty AvatarName!");
            }
            Console.WriteLine($"       Solo clan successfully populated with 4 dummies (Total: 5). Opponent '{soloWar.Clan2.ClanName}' created with 5 dummies.");

            Console.WriteLine("[12/12] Validating War Map Packet 24335 for Solo Clan War with Dummy Bases...");
            var soloMapMsg = new AllianceWarMapDataMessage(null, soloWar);
            soloMapMsg.Encode();
            byte[] soloMapBytes = soloMapMsg.Data.ToArray();
            if (soloMapBytes == null || soloMapBytes.Length < 1000)
                throw new Exception($"Solo war map packet length unexpected: {soloMapBytes?.Length ?? 0} bytes!");
            Console.WriteLine($"       Solo war map encoded successfully: {soloMapBytes.Length} bytes for 10 total bases (5v5).");
        }
    }
}
