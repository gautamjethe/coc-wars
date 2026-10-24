using CSS.Helpers.Binary;

namespace CSS.Packets.Commands.Client
{
    // Packet 567
    internal class SetActiveVillageLayoutCommand : Command
    {
        private int Layout;
        public SetActiveVillageLayoutCommand(Reader reader, Device client, int id) : base(reader, client, id)
        {
            //Layout = br.ReadInt32();
            //Console.WriteLine(br.ReadInt32());
            //Console.WriteLine(br.ReadInt32());
        }
        internal override void Decode()
        {
            this.Layout = this.Reader.ReadInt32();
            if (this.Reader.BaseStream.Position + 4 <= this.Reader.BaseStream.Length)
            {
                this.Reader.ReadInt32();
            }
            if (this.Reader.BaseStream.Position + 1 <= this.Reader.BaseStream.Length)
            {
                this.Reader.ReadBoolean();
            }
            if (this.Reader.BaseStream.Position + 4 <= this.Reader.BaseStream.Length)
            {
                this.Reader.ReadUInt32();
            }
        }

        internal override void Process()
        {
            if (this.Device?.Player != null)
            {
                this.Device.Player.Avatar.m_vActiveLayout = this.Layout;
                CSS.Core.Resources.DatabaseManager.Save(this.Device.Player);
            }
        }
    }
}
