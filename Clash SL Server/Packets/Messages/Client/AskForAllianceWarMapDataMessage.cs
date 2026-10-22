using System;
using CSS.Core;
using CSS.Core.Network;
using CSS.Helpers.Binary;
using CSS.Logic;
using CSS.Logic.Enums;
using CSS.Packets.Messages.Server;

namespace CSS.Packets.Messages.Client
{
    // Packet 14334 - Client requests War Map data
    internal class AskForAllianceWarMapDataMessage : Message
    {
        public long WarId;

        public AskForAllianceWarMapDataMessage(Device client, Reader reader) : base(client, reader)
        {
        }

        internal override void Decode()
        {
            try
            {
                if (this.Reader.BaseStream.Length - this.Reader.BaseStream.Position >= 8)
                {
                    this.WarId = this.Reader.ReadInt64();
                }
            }
            catch (Exception)
            {
            }
        }

        internal override void Process()
        {
            try
            {
                if (this.Device?.Player?.Avatar == null) return;

                this.Device.PlayerState = State.WAR_EMODE;

                ClanWar war = null;
                if (WarId > 0)
                {
                    war = ClanWarManager.GetWar(WarId);
                }
                if (war == null && this.Device.Player.Avatar.AllianceId > 0)
                {
                    war = ClanWarManager.GetWarByClanId(this.Device.Player.Avatar.AllianceId);
                }

                Logger.Say($"[ClanWars] Packet 14334 ignored: clan war packets disabled.");
            }
            catch (Exception ex)
            {
                Logger.Write($"[Error] AskForAllianceWarMapDataMessage process error: {ex.Message}");
            }
        }
    }
}
