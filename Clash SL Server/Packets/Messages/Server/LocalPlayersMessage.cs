using System;
using System.Collections.Generic;
using System.Linq;
using CSS.Core;
using CSS.Helpers.List;
using CSS.Logic;

namespace CSS.Packets.Messages.Server
{
    // Packet 24404
    internal class LocalPlayersMessage : Message
    {
        public LocalPlayersMessage(Device client) : base(client)
        {
            this.Identifier = 24404;
        }

        internal override async void Encode()
        {
            try
            {
                string playerRegion = string.IsNullOrEmpty(this.Device?.Player?.Avatar?.Region) ? "EN" : this.Device.Player.Avatar.Region.ToUpper();
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
                    list1Data.AddString(string.IsNullOrEmpty(pl.Region) ? playerRegion : pl.Region.ToUpper());
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

                this.Data.AddInt(rank - 1);
                this.Data.AddRange(list1Data);
            }
            catch (Exception ex)
            {
                Logger.Write($"[Error] LocalPlayersMessage encode error: {ex.Message}");
            }
        }
    }
}