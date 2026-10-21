using System;
using MySql.Data.MySqlClient;
using Newtonsoft.Json.Linq;
using CSS.Logic;

namespace Test {
    class Program {
        static void Main() {
            var cs = "server=sql12.freesqldatabase.com;port=3306;user=sql12839059;password=UIwzJXPdF8;database=sql12839059;";
            using (var conn = new MySqlConnection(cs)) {
                conn.Open();
                using (var cmd = new MySqlCommand("SELECT Data FROM ClanWar WHERE State != 6", conn))
                using (var reader = cmd.ExecuteReader()) {
                    if (reader.Read()) {
                        var json = JObject.Parse(reader.GetString(0));
                        var war = new ClanWar();
                        war.LoadFromJson(json);
                        Console.WriteLine("war.RemainingTimeSeconds = " + war.RemainingTimeSeconds);
                        Console.WriteLine("PreparationEndTime = " + war.PreparationEndTime + " (Kind: " + war.PreparationEndTime.Kind + ")");
                        Console.WriteLine("DateTime.UtcNow = " + DateTime.UtcNow);
                        Console.WriteLine("DateTime.Now = " + DateTime.Now);
                    }
                }
            }
        }
    }
}
