using System;
using MySql.Data.MySqlClient;

class TestBuilder {
    static void Main() {
        var builder = new MySqlConnectionStringBuilder();
        builder.Server = "localhost";
        builder.CharacterSet = "utf8mb4";
        builder.ConvertZeroDateTime = true;
        Console.WriteLine(builder.ToString());
    }
}
