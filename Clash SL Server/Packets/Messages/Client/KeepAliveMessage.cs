using System.IO;
using CSS.Core.Network;
using CSS.Helpers.Binary;
using CSS.Logic;
using CSS.Packets.Messages.Server;

namespace CSS.Packets.Messages.Client
{
    // Packet 10108
    internal class KeepAliveMessage : Message
    {
        public KeepAliveMessage(Device device, Reader reader) : base(device, reader)
        {
        }

        internal override void Process()
        {
           new KeepAliveOkMessage(Device, this).Send();

           if (Device != null && !Device.WarMapSent && Device.Player?.Avatar?.AllianceId > 0)
           {
               ClanWar war = ClanWarManager.GetWarByClanId(Device.Player.Avatar.AllianceId);
               if (war != null && war.State != ClanWarState.NOT_IN_WAR && war.State != ClanWarState.SEARCHING)
               {
                   Device.WarMapSent = true;
                   new AllianceWarDataMessage(Device, war).Send();
                   new AllianceWarMapDataMessage(Device, war).Send();
                   Core.Logger.Say($"[ClanWars] Sent KeepAlive war map to client for clan {Device.Player.Avatar.AllianceId}");
               }
           }
        }
    }
}