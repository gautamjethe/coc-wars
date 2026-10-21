using System;
using MySql.Data.MySqlClient;
using Newtonsoft.Json.Linq;

namespace Test {
    class Program {
        static void Main() {
            var cs = "server=sql12.freesqldatabase.com;port=3306;user=sql12839059;password=UIwzJXPdF8;database=sql12839059;";
            using (var conn = new MySqlConnection(cs)) {
                conn.Open();
                using (var cmd = new MySqlCommand("SELECT Data FROM ClanWar WHERE State != 6", conn))
                using (var reader = cmd.ExecuteReader()) {
                    while (reader.Read()) {
                        var json = JObject.Parse(reader.GetString(0));
                        foreach (var p in json.Properties()) {
                            if (p.Name != "clan1" && p.Name != "clan2" && p.Name != "attack_logs") {
                                Console.WriteLine(p.Name + ": " + p.Value);
                            } else if (p.Name == "clan1" || p.Name == "clan2") {
                                var c = (JObject)p.Value;
                                Console.WriteLine(p.Name + " id=" + c["clan_id"] + ", name=" + c["clan_name"] + ", members=" + (c["members"] as JArray)?.Count);
                            }
                        }
                    }
                }
            }
        }
    }
}
