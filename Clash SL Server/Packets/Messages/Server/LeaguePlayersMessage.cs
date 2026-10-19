using System;
using System.Collections.Generic;
using System.Linq;
using CSS.Core;
using CSS.Helpers.List;
using CSS.Logic;

namespace CSS.Packets.Messages.Server
{
    // Packet 24503
    internal class LeaguePlayersMessage : Message
    {
        public LeaguePlayersMessage(Device client) : base(client)
        {
            this.Identifier = 24503;
        }

        internal override async void Encode()
        {
            try
            {
                DatabaseManager db = Resources.DatabaseManager ?? new DatabaseManager();
                var season = db.GetOrCreateLeaderboardSeason("PlayerSeason", 2592000);

                List<byte> packet1 = new List<byte>();
                int i = 1;

                int leagueId = this.Device?.Player?.Avatar?.m_vLeagueId ?? 0;
                var candidateLevels = ResourcesManager.m_vOnlinePlayers
                    .Concat(ResourcesManager.m_vInMemoryLevels.Values)
                    .Where(t => t?.Avatar != null && t.Avatar.m_vLeagueId == leagueId)
                    .GroupBy(t => t.Avatar.UserId)
                    .Select(g => g.First())
                    .OrderByDescending(t => t.Avatar.GetScore());

                foreach (Level player in candidateLevels)
                {
                    if (i >= 51)
                        break;

                    ClientAvatar pl = player.Avatar;
                    if (pl != null && pl.AvatarName != null)
                    {
                        packet1.AddLong(pl.UserId);
                        packet1.AddString(pl.AvatarName);
                        packet1.AddInt(i);
                        packet1.AddInt(pl.GetScore());
                        packet1.AddInt(i);
                        packet1.AddInt(pl.m_vAvatarLevel);
                        packet1.AddInt(200);
                        packet1.AddInt(i);
                        packet1.AddInt(100);
                        packet1.AddInt(1);
                        packet1.AddLong(pl.AllianceId);
                        packet1.AddInt(1);
                        packet1.AddInt(1);
                        if (pl.AllianceId > 0)
                        {
                            Alliance clan = ObjectManager.GetAlliance(pl.AllianceId);
                            if (clan != null)
                            {
                                packet1.Add((byte)1);
                                packet1.AddLong(pl.AllianceId);
                                packet1.AddString(clan.m_vAllianceName ?? "Clan");
                                packet1.AddInt(clan.m_vAllianceBadgeData);
                                packet1.AddLong(i);
                            }
                            else
                            {
                                packet1.Add((byte)0);
                            }
                        }
                        else
                        {
                            packet1.Add((byte)0);
                        }
                        i++;
                    }
                }

                this.Data.AddInt(season.RemainingSeconds); // Season End (synced with database)
                this.Data.AddInt(i - 1);
                this.Data.AddRange(packet1);
            }
            catch (Exception ex)
            {
                Logger.Write($"[Error] LeaguePlayersMessage encode error: {ex.Message}");
            }
        }
    }
}