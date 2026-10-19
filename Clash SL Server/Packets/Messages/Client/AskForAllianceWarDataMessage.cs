using System;
using CSS.Core;
using CSS.Core.Network;
using CSS.Helpers.Binary;
using CSS.Logic;
using CSS.Packets.Messages.Server;

namespace CSS.Packets.Messages.Client
{
    // Packet 14331
    internal class AskForAllianceWarDataMessage : Message
    {
        public AskForAllianceWarDataMessage(Device client, Reader reader) : base(client, reader)
        {
        }

        internal override void Process()
        {
            try
            {
                long clanId = this.Device?.Player?.Avatar?.AllianceId ?? 0;
                ClanWar war = null;
                Alliance alliance = null;
                if (clanId > 0)
                {
                    war = ClanWarManager.GetWarByClanId(clanId);
                    alliance = ObjectManager.GetAlliance(clanId);
                }

                if (alliance != null)
                {
                    new AllianceFullEntryMessage(this.Device, alliance).Send();
                }

                new AllianceWarDataMessage(this.Device, war).Send();

                if (war != null)
                {
                    new AllianceWarMapDataMessage(this.Device, war).Send();
                }
                else
                {
                    new AllianceWarMapDataMessage(this.Device).Send();
                }
            }
            catch (Exception ex)
            {
                Logger.Write($"[Error] AskForAllianceWarDataMessage process error: {ex.Message}");
            }
        }
    }
}