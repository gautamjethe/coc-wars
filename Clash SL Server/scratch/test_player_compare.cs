using System;
using System.IO;
using CSS.Core;
using CSS.Logic;
using CSS.Files;
using CSS.Packets.Messages.Server;

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
                var p6 = db.GetAccount(6).Result;
                var p7 = db.GetAccount(7).Result;

                Console.WriteLine($"P6: AvatarId={p6.Avatar.UserId}, Name={p6.Avatar.AvatarName}, UnitsCount={p6.Avatar.GetUnits().Count}, Upgrades={p6.Avatar.m_vUnitUpgradeLevel.Count}");
                Console.WriteLine($"P7: AvatarId={p7.Avatar.UserId}, Name={p7.Avatar.AvatarName}, UnitsCount={p7.Avatar.GetUnits().Count}, Upgrades={p7.Avatar.m_vUnitUpgradeLevel.Count}");

                byte[] enc6 = p6.Avatar.EncodeSync();
                byte[] enc7 = p7.Avatar.EncodeSync();

                Console.WriteLine($"Avatar.EncodeSync byte length: P6={enc6.Length}, P7={enc7.Length}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex);
            }
        }
    }
}
