using System;
using System.IO;
using System.Threading.Tasks;
using CSS.Core;
using CSS.Logic;
using CSS.Packets;
using CSS.Packets.Messages.Server;

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
                    Console.WriteLine("=== Testing Player Encode ===");
                    new CSVManager();
                    new ObjectManager();
                    var db = new DatabaseManager();

                    Console.WriteLine("\n--- Testing Existing Player 1 ---");
                    var level1 = await db.GetAccount(1);
                    if (level1 != null)
                    {
                        Console.WriteLine($"Player 1 loaded: Name={level1.Avatar.AvatarName}, TH={level1.Avatar.m_vTownHallLevel}, Score={level1.Avatar.GetScore()}, TutorialStep={level1.Avatar.TutorialStepsCount}, LastTickSaved={level1.Avatar.LastTickSaved}");
                        File.WriteAllText("player1_avatar.json", level1.Avatar.SaveToJSON());
                        File.WriteAllText("player1_village.json", level1.SaveToJSON());
                        var dev1 = new Device(null);
                        dev1.Player = level1;
                        var ok1 = new LoginOkMessage(dev1) { ServerMajorVersion = 8, ServerBuild = 709, ContentVersion = 16 };
                        ok1.Encode();
                        Console.WriteLine($"LoginOkMessage 1: {ok1.Data.Count} bytes");
                        var home1 = new OwnHomeDataMessage(dev1, level1);
                        home1.Encode();
                        Console.WriteLine($"OwnHomeDataMessage 1: {home1.Data.Count} bytes");
                        File.WriteAllBytes("home1.bin", home1.Data.ToArray());
                    }

                    Console.WriteLine("\n--- Testing Newly Created Player via ObjectManager.CreateAvatar ---");
                    var newLvl = ObjectManager.CreateAvatar(0, null);
                    newLvl.Avatar.InitializeAccountCreationDate();
                    newLvl.Avatar.m_vAndroid = true;
                    Console.WriteLine($"New player: ID={newLvl.Avatar.UserId}, Token={newLvl.Avatar.UserToken}, Name={newLvl.Avatar.AvatarName}, TH={newLvl.Avatar.m_vTownHallLevel}, Score={newLvl.Avatar.GetScore()}, TutorialStep={newLvl.Avatar.TutorialStepsCount}");
                    var devNew = new Device(null);
                    devNew.Player = newLvl;
                    var okNew = new LoginOkMessage(devNew) { ServerMajorVersion = 8, ServerBuild = 709, ContentVersion = 16 };
                    okNew.Encode();
                    Console.WriteLine($"LoginOkMessage New: {okNew.Data.Count} bytes");
                    var homeNew = new OwnHomeDataMessage(devNew, newLvl);
                    homeNew.Encode();
                    Console.WriteLine($"OwnHomeDataMessage New: {homeNew.Data.Count} bytes");
                    File.WriteAllBytes("homeNew.bin", homeNew.Data.ToArray());

                    string vNew = newLvl.SaveToJSON();
                    Console.WriteLine($"New village json length: {vNew.Length}");
                    Console.WriteLine($"New village json preview:\n{vNew.Substring(0, Math.Min(500, vNew.Length))}");
                    File.WriteAllText("new_village_sample.json", vNew);
                    File.WriteAllText("new_avatar_sample.json", newLvl.Avatar.SaveToJSON());
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception: {ex}");
                }
            }).Wait();
        }
    }
}
