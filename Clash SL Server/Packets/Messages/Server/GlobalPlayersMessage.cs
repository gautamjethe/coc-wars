using System;
using System.Collections.Generic;
using System.Linq;
using CSS.Core;
using CSS.Helpers.List;
using CSS.Logic;

namespace CSS.Packets.Messages.Server
{
    // Packet 24403
    internal class GlobalPlayersMessage : Message
    {
        public GlobalPlayersMessage(Device client) : base(client)
        {
            this.Identifier = 24403;
        }

        internal override async void Encode()
        {
            try
            {
                var topPlayers = DatabaseManager.GetTopPlayers(100);

                List<byte> list1Data = new List<byte>();
                int rank = 1;

                foreach (var pl in topPlayers)
                {
                    if (pl == null) continue;

                    list1Data.AddLong(pl.UserId);
                    list1Data.AddString(pl.AvatarName ?? "Player");
                    list1Data.AddInt(rank);
                    list1Data.AddInt(pl.GetScore());
                    list1Data.AddInt(rank);
                    list1Data.AddInt(pl.m_vAvatarLevel);
                    list1Data.AddInt(100);
                    list1Data.AddInt(rank - 1);
                    list1Data.AddInt(100);
                    list1Data.AddInt(1);
                    list1Data.AddInt(pl.m_vLeagueId);
                    list1Data.AddString(string.IsNullOrEmpty(pl.Region) ? "EN" : pl.Region.ToUpper());
                    list1Data.AddLong(pl.UserId);
                    list1Data.AddInt(1);
                    list1Data.AddInt(1);

                    if (pl.AllianceId > 0)
                    {
                        Alliance clan = ObjectManager.GetAlliance(pl.AllianceId);
                        if (clan != null)
                        {
                            list1Data.Add((byte)1);
                            list1Data.AddLong(pl.AllianceId);
                            list1Data.AddString(clan.m_vAllianceName ?? "Clan");
                            list1Data.AddInt(clan.m_vAllianceBadgeData);
                        }
                        else
                        {
                            list1Data.Add((byte)0);
                        }
                    }
                    else
                    {
                        list1Data.Add((byte)0);
                    }

                    rank++;
                }

                // List 1: Top Players
                this.Data.AddInt(rank - 1);
                this.Data.AddRange(list1Data);

                // List 2: Previous Season (0 items to prevent binary format mismatch crash)
                this.Data.AddInt(0);

                // Season timer & dates (from database)
                DatabaseManager db = Resources.DatabaseManager ?? new DatabaseManager();
                var season = db.GetOrCreateLeaderboardSeason("PlayerSeason", 2592000); // 30-day season

                this.Data.AddInt(season.RemainingSeconds);
                this.Data.AddInt(season.EndTime.Year);
                this.Data.AddInt(season.EndTime.Month);
                this.Data.AddInt(season.EndTime.AddMonths(-1).Year);
                this.Data.AddInt(season.EndTime.AddMonths(-1).Month);
            }
            catch (Exception ex)
            {
                Logger.Write($"[Error] GlobalPlayersMessage encode error: {ex.Message}");
            }
        }
    }
}