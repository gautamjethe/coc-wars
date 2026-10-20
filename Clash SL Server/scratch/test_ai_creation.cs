using System;
using System.IO;
using CSS.Core;
using CSS.Logic;
using CSS.Files;

namespace Scratch
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                new CSVManager();
                Console.WriteLine("CSVManager initialized.");

                var db = new DatabaseManager();
                long maxP = db.GetMaxPlayerId();
                long maxC = db.GetMaxAllianceId();
                Console.WriteLine($"DB Max PlayerId: {maxP}, Max ClanId: {maxC}");

                new ObjectManager();

                long nextP1 = ObjectManager.GeneratePlayerId();
                long nextP2 = ObjectManager.GeneratePlayerId();
                long nextC1 = ObjectManager.GenerateAllianceId();
                Console.WriteLine($"Generated PlayerId 1: {nextP1}");
                Console.WriteLine($"Generated PlayerId 2: {nextP2}");
                Console.WriteLine($"Generated ClanId 1: {nextC1}");

                if (nextP1 <= maxP || nextP2 != nextP1 + 1)
                {
                    throw new Exception("Player ID generation sequence invalid!");
                }
                if (nextP1 >= 99990000 || nextP2 >= 99990000)
                {
                    throw new Exception("Player ID still using 999999x range!");
                }

                // Test creating a fresh account
                Level fresh = ObjectManager.CreateAvatar(0, "TEST_TOKEN");
                Console.WriteLine($"Created fresh avatar: UserId={fresh.Avatar.UserId}, Name={fresh.Avatar.AvatarName}");

                byte[] encoded = fresh.Avatar.EncodeSync();
                Console.WriteLine($"Fresh avatar EncodeSync byte length: {encoded.Length}");
                if (encoded.Length == 0)
                {
                    throw new Exception("EncodeSync failed or returned 0 bytes!");
                }

                // Verify ClanWar AI clan generation
                var aiWarClan = ClanWarManager.CreatePresetWarMember(nextC1, 0, 9, "TestBot");
                Console.WriteLine($"Created ClanWar AI Member: UserId={aiWarClan.UserId}, MapPos={aiWarClan.MapPosition}");
                if (aiWarClan.UserId >= 99990000)
                {
                    throw new Exception($"AI war member UserId is {aiWarClan.UserId}, which is in 999999x range!");
                }

                Console.WriteLine("\n========================================================");
                Console.WriteLine(">>> ALL AI & SEQUENTIAL PLAYER ID TESTS PASSED! <<<");
                Console.WriteLine("========================================================");
            }
            catch (Exception ex)
            {
                Console.WriteLine("FAIL: " + ex);
            }
        }
    }
}
