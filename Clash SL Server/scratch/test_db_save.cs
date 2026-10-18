using System;
using System.Threading.Tasks;
using CSS.Core;
using CSS.Logic;
using CSS.Files;

namespace Scratch
{
    class Program
    {
        static void Main(string[] args)
        {
            Task.Run(async () =>
            {
                try
                {
                    Console.WriteLine("=== Testing DatabaseManager ADO.NET Integration ===");
                    new CSVManager();
                    Console.WriteLine("CSVManager initialized successfully.");

                    var db = new DatabaseManager();

                    long maxPlayer = db.GetMaxPlayerId();
                    long maxClan = db.GetMaxAllianceId();
                    Console.WriteLine($"Current Max PlayerId in DB: {maxPlayer}");
                    Console.WriteLine($"Current Max ClanId in DB: {maxClan}");

                    long testPlayerId = 777001;
                    Console.WriteLine($"\n[1] Creating test player {testPlayerId}...");
                    var level = new Level(testPlayerId, "TEST_TOKEN_12345");
                    level.Avatar.SetName("Commander777");
                    level.Avatar.SetScore(1500);

                    db.CreateAccount(level);
                    Console.WriteLine("[1] CreateAccount called successfully!");

                    Console.WriteLine("\n[2] Updating/Saving test player...");
                    level.Avatar.SetScore(1600);
                    await db.Save(level);
                    Console.WriteLine("[2] Save called successfully!");

                    Console.WriteLine("\n[3] Reading test player back from DB via GetAccount...");
                    var loadedLevel = await db.GetAccount(testPlayerId);
                    if (loadedLevel != null)
                    {
                        Console.WriteLine($"[3] SUCCESS! Loaded player {loadedLevel.Avatar.UserId}: Name='{loadedLevel.Avatar.AvatarName}', Score={loadedLevel.Avatar.GetScore()}, Token='{loadedLevel.Avatar.UserToken}'");
                        if (loadedLevel.Avatar.UserToken == "TEST_TOKEN_12345" && loadedLevel.Avatar.AvatarName == "Commander777")
                        {
                            Console.WriteLine(">>> ALL DATA VERIFIED MATCHING! <<<");
                        }
                        else
                        {
                            Console.WriteLine(">>> WARNING: Data mismatch! <<<");
                        }
                    }
                    else
                    {
                        Console.WriteLine(">>> FAILED: GetAccount returned null! <<<");
                    }

                    Console.WriteLine("\n[4] Testing GetAllPlayerIds...");
                    var allIds = db.GetAllPlayerIds();
                    Console.WriteLine($"GetAllPlayerIds count: {allIds.Count}, contains {testPlayerId}: {allIds.Contains(testPlayerId)}");

                    // Cleanup test player
                    Console.WriteLine("\n[5] Cleaning up test player...");
                    using (var conn = new MySql.Data.MySqlClient.MySqlConnection(db.Mysql))
                    {
                        conn.Open();
                        using (var cmd = new MySql.Data.MySqlClient.MySqlCommand("DELETE FROM Player WHERE PlayerId = " + testPlayerId, conn))
                        {
                            cmd.ExecuteNonQuery();
                        }
                    }
                    Console.WriteLine("[5] Cleanup complete!");

                    Console.WriteLine("\n=== ALL DATABASE TESTS PASSED! ===");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("FATAL EXCEPTION: " + ex);
                }
            }).GetAwaiter().GetResult();
        }
    }
}
