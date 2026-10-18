namespace CSS.Database
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    using CSS.Helpers;

    public class Mysql : DbContext
    {
        public Mysql() : base(GetConnectionString())
        {
        }

        public static string GetConnectionString()
        {
            try
            {
                var host = Utils.ParseConfigString("MysqlIPAddress");
                var user = Utils.ParseConfigString("MysqlUsername");
                var port = Utils.ParseConfigInt("MysqlPort");
                var db = Utils.ParseConfigString("MysqlDatabase");
                var pwd = Utils.ParseConfigString("MysqlPassword");

                if (!string.IsNullOrEmpty(host))
                {
                    string providerConn = $"server={host};port={port};user id={user};password={pwd};persistsecurityinfo=True;database={db};CharSet=utf8mb4;";
                    return $"metadata=res://*/Database.CSSdb.csdl|res://*/Database.CSSdb.ssdl|res://*/Database.CSSdb.msl;provider=MySql.Data.MySqlClient;provider connection string=\"{providerConn}\"";
                }
            }
            catch (Exception) { }
            return "name=mysql";
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }

        public virtual DbSet<Clan> Clan { get; set; }
        public virtual DbSet<Player> Player { get; set; }
    }
}
