using System;
using CSS.Helpers.Binary;
using CSS.Logic;

namespace CSS.Packets.Commands.Client
{
    // Packet 603
    internal class EndOfBattleCommand : Command
    {
        public EndOfBattleCommand(Reader reader, Device client, int id) : base(reader, client, id)
        {
        }

        internal override void Decode()
        {
        }

        internal override void Process()
        {
            try
            {
                if (this.Device?.Player != null)
                {
                    long attackerId = this.Device.Player.Avatar.UserId;
                    if (ClanWarManager.ActiveBattles.ContainsKey(attackerId))
                    {
                        ClanWarManager.FinalizeBattle(attackerId);
                    }
                }
            }
            catch (Exception)
            {
            }
        }
    }
}