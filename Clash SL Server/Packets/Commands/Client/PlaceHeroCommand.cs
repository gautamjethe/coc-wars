using CSS.Helpers.Binary;
using CSS.Logic;

namespace CSS.Packets.Commands.Client
{
    // Packet 605
    internal class PlaceHeroCommand : Command
    {
        public PlaceHeroCommand(Reader reader, Device client, int id) : base(reader, client, id)
        {
        }


        internal override void Decode()
        {
            this.X = this.Reader.ReadInt32();
            this.Y = this.Reader.ReadInt32();
            this.HeroID = this.Reader.ReadInt32();
            this.Tick = this.Reader.ReadInt32();
        }

        internal override void Process()
        {
            if (this.Device.PlayerState == Logic.Enums.State.WAR_EMODE && this.Device.Player != null)
            {
                ClanWarManager.TrackTroopDeployment(this.Device.Player.Avatar.UserId, 25);
            }
        }

        public int X;

        public int Y;

        public int Tick;

        public int HeroID;
    }
}