using CSS.Helpers.Binary;
using CSS.Logic;

namespace CSS.Packets.Commands.Client
{
    internal class PlaceAllianceTroopsCommand : Command
    {
        public PlaceAllianceTroopsCommand(Reader reader, Device client, int id) : base(reader, client, id)
        {
        }

        internal override void Decode()
        {
            this.Reader.ReadInt32();
            this.Reader.ReadInt32();
            this.Reader.ReadInt32();
            this.Reader.ReadInt32();
        }

        internal override async void Process()
        {
            ClientAvatar _Player = this.Device.Player.Avatar;

            if (_Player != null)
            {
                int ccUsed = _Player.AllianceUnits.Count > 0 ? 25 : 0;
                _Player.AllianceUnits.Clear();
                _Player.SetAllianceCastleUsedCapacity(0);

                if (this.Device.PlayerState == Logic.Enums.State.WAR_EMODE && ccUsed > 0)
                {
                    ClanWarManager.TrackTroopDeployment(_Player.UserId, ccUsed);
                }
            }
        }
    }
}
