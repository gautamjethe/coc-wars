using System;
using CSS.Core;
using CSS.Core.Network;
using CSS.Helpers.Binary;
using CSS.Logic;
using CSS.Packets.Messages.Server;

namespace CSS.Packets.Messages.Client
{
    // Packet 14331 & 14332
    internal class AskForAllianceWarDataMessage : Message
    {
        public long WarId;

        public AskForAllianceWarDataMessage(Device client, Reader reader) : base(client, reader)
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
                long clanId = this.Device?.Player?.Avatar?.AllianceId ?? 0;
                ClanWar war = null;
                Alliance alliance = null;
                if (clanId > 0)
                {
                    war = ClanWarManager.GetWarByClanId(clanId);
                    alliance = ObjectManager.GetAlliance(clanId);
                }
                if (war == null && WarId > 0)
                {
                    war = ClanWarManager.GetWar(WarId);
                }

                Logger.Say($"[ClanWars] Packet 14331/14332 handled for clan {clanId}, warId={war?.WarId ?? WarId}");

                if (alliance != null)
                {
                    new AllianceFullEntryMessage(this.Device, alliance).Send();
                }

                if (war != null && war.State != ClanWarState.NOT_IN_WAR && war.State != ClanWarState.SEARCHING)
                {
                    new AllianceWarMapDataMessage(this.Device, war).Send();
                }
            }
            catch (Exception ex)
            {
                Logger.Write($"[Error] AskForAllianceWarDataMessage process error: {ex.Message}");
            }
        }
    }
}