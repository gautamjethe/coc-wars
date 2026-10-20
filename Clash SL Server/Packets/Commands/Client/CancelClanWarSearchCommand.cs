using System;
using CSS.Core;
using CSS.Core.Network;
using CSS.Helpers.Binary;
using CSS.Logic;
using CSS.Packets.Messages.Server;

namespace CSS.Packets.Commands.Client
{
    // Command 561
    internal class CancelClanWarSearchCommand : Command
    {
        public int Tick;

        public CancelClanWarSearchCommand(Reader reader, Device client, int id) : base(reader, client, id)
        {
        }

        internal override void Decode()
        {
            try
            {
                this.Tick = this.Reader.ReadInt32();
            }
            catch (Exception) { }
        }

        internal override void Process()
        {
            try
            {
                long clanId = this.Device?.Player?.Avatar?.AllianceId ?? 0;
                if (clanId <= 0) return;

                Alliance clan = ObjectManager.GetAlliance(clanId);
                if (clan == null) return;

                // Check permissions: Leader (1/2) or Co-Leader (4)
                if (clan.m_vAllianceMembers.TryGetValue(this.Device.Player.Avatar.UserId, out AllianceMemberEntry member))
                {
                    if (member.Role != 1 && member.Role != 2 && member.Role != 4)
                    {
                        Logger.Write($"User {this.Device.Player.Avatar.UserId} tried to cancel clan war search without Leader/Co-Leader role.");
                        return;
                    }
                }

                ClanWarManager.CancelSearch(clanId);

                if (this.Device != null)
                {
                    this.Device.PlayerState = CSS.Logic.Enums.State.LOGGED;
                    new AllianceFullEntryMessage(this.Device, clan).Send();
                    new AllianceWarDataMessage(this.Device).Send();
                }
            }
            catch (Exception ex)
            {
                Logger.Write($"[Error] CancelClanWarSearchCommand process error: {ex.Message}");
            }
        }
    }
}
