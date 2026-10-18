using System;
using MySql.Data.MySqlClient;

class TestDirect {
    static void Main() {
        string connStr = "server=sql12.freesqldatabase.com;port=3306;user id=sql12839059;password=UIwzJXPdF8;database=sql12839059;CharSet=utf8mb4;";
        using (var conn = new MySqlConnection(connStr)) {
            conn.Open();
            Console.WriteLine("Direct MySQL connection opened!");
            
            using (var cmd = new MySqlCommand("INSERT INTO Player (PlayerId, Avatar, GameObjects, LastUpdateTime) VALUES (@id, @avatar, @go, NOW()) ON DUPLICATE KEY UPDATE Avatar=@avatar, GameObjects=@go, LastUpdateTime=NOW()", conn)) {
                cmd.Parameters.AddWithValue("@id", 12345);
                cmd.Parameters.AddWithValue("@avatar", "{\"test\":1}");
                cmd.Parameters.AddWithValue("@go", "{\"go\":2}");
                int affected = cmd.ExecuteNonQuery();
                Console.WriteLine("Rows affected: " + affected);
            }

            using (var cmd = new MySqlCommand("SELECT PlayerId, Avatar, GameObjects FROM Player WHERE PlayerId = 12345", conn)) {
                using (var reader = cmd.ExecuteReader()) {
                    if (reader.Read()) {
                        Console.WriteLine("Read player back: ID=" + reader.GetInt64(0) + ", Avatar=" + reader.GetString(1));
                    }
                }
            }

            // Cleanup test
            using (var cmd = new MySqlCommand("DELETE FROM Player WHERE PlayerId = 12345", conn)) {
                cmd.ExecuteNonQuery();
                Console.WriteLine("Cleaned up test player!");
            }
        }
    }
}
