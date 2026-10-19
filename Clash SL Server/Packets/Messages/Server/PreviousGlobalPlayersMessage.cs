using System;
using System.Collections.Generic;
using CSS.Core;
using CSS.Helpers.List;
using CSS.Logic;

namespace CSS.Packets.Messages.Server
{
    internal class PreviousGlobalPlayersMessage : Message
    {
        public PreviousGlobalPlayersMessage(Device client) : base(client)
        {
            this.Identifier = 24405;
        }

        internal override async void Encode()
        {
            try
            {
                DatabaseManager db = Resources.DatabaseManager ?? new DatabaseManager();
                var season = db.GetOrCreateLeaderboardSeason("PlayerSeason", 2592000);
                DateTime prevSeason = season.EndTime.AddMonths(-1);

                // Previous season leaderboard:
                // Currently 0 archived players (prevents malformed packet crash when no snapshot exists)
                this.Data.AddInt(0);
                this.Data.AddInt(prevSeason.Month);
                this.Data.AddInt(prevSeason.Year);
            }
            catch (Exception ex)
            {
                Logger.Write($"[Error] PreviousGlobalPlayersMessage encode error: {ex.Message}");
            }
        }
    }
}

