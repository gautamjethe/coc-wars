using System;
using System.Linq;
using CSS.Core;
using CSS.Core.Network;
using CSS.Helpers.Binary;
using CSS.Logic;
using CSS.Logic.StreamEntry;
using CSS.Packets.Messages.Server;

namespace CSS.Packets.Commands.Client
{
    // Command 560
    internal class StartClanWarCommand : Command
    {
        public int Tick;
        public int WarSize;

        public StartClanWarCommand(Reader reader, Device client, int id) : base(reader, client, id)
        {
        }

        internal override void Decode()
        {
            try
            {
                this.Tick = this.Reader.ReadInt32();
                if (this.Reader.BaseStream.Position + 4 <= this.Reader.BaseStream.Length)
                {
                    this.WarSize = this.Reader.ReadInt32();
                }
            }
            catch (Exception) { }
        }

        internal override async void Process()
        {
            try
            {
                long clanId = this.Device?.Player?.Avatar?.AllianceId ?? 0;
                if (clanId <= 0) return;

                Alliance clan = ObjectManager.GetAlliance(clanId);
                if (clan == null) return;

                // Check permissions: Leader (1) or Co-Leader (4)
                if (clan.m_vAllianceMembers.TryGetValue(this.Device.Player.Avatar.UserId, out AllianceMemberEntry member))
                {
                    if (member.Role != 1 && member.Role != 4)
                    {
                        Logger.Write($"User {this.Device.Player.Avatar.UserId} tried to start clan war without Leader/Co-Leader role.");
                        return;
                    }
                }

                // Check if already in war
                var existingWar = ClanWarManager.GetWarByClanId(clanId);
                if (existingWar != null && existingWar.State != ClanWarState.WAR_ENDED && existingWar.State != ClanWarState.NOT_IN_WAR)
                {
                    Logger.Write($"Clan {clanId} is already in war {existingWar.WarId} (State: {existingWar.State})");
                    new AllianceWarMapDataMessage(this.Device, existingWar).Send();
                    return;
                }

                int memberCount = clan.GetAllianceMembers().Count;
                int teamSize = WarSize > 0 ? WarSize : 10;

                // Pick best matching standard size: 5, 10, 15, 20, 25, 30, 40, 50
                if (memberCount >= 50 && teamSize >= 50) teamSize = 50;
                else if (memberCount >= 40 && teamSize >= 40) teamSize = 40;
                else if (memberCount >= 30 && teamSize >= 30) teamSize = 30;
                else if (memberCount >= 25 && teamSize >= 25) teamSize = 25;
                else if (memberCount >= 20 && teamSize >= 20) teamSize = 20;
                else if (memberCount >= 15 && teamSize >= 15) teamSize = 15;
                else if (memberCount >= 10 && teamSize >= 10) teamSize = 10;
                else if (memberCount >= 5) teamSize = 5;
                else teamSize = Math.Max(1, memberCount); // Solo/testing support

                // Start the war!
                var war = await ClanWarManager.StartWar(clan, null, teamSize);

                if (war != null)
                {
                    AllianceEventStreamEntry eventStreamEntry = new AllianceEventStreamEntry();
                    eventStreamEntry.ID = (int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
                    eventStreamEntry.SetSender(this.Device.Player.Avatar);
                    eventStreamEntry.EventType = 7; // Clan War started
                    clan.AddChatMessage(eventStreamEntry);

                    foreach (AllianceMemberEntry a in clan.GetAllianceMembers())
                    {
                        Level l = await ResourcesManager.GetPlayer(a.AvatarId);
                        if (l?.Client != null)
                        {
                            new AllianceFullEntryMessage(l.Client, clan).Send();
                            new AllianceWarDataMessage(l.Client, war).Send();
                            new AllianceStreamEntryMessage(l.Client) { StreamEntry = eventStreamEntry }.Send();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Write($"[Error] StartClanWarCommand process error: {ex.Message}");
            }
        }
    }
}
