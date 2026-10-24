using CSS.Helpers.Binary;

namespace CSS.Packets.Commands.Client
{
    // Packet 546
    internal class EditVillageLayoutCommand : Command
    {
        internal int LayoutId;
        internal bool EditMode;
        internal uint Tick;

        public EditVillageLayoutCommand(Reader reader, Device client, int id) : base(reader, client, id)
        {
        }

        internal override void Decode()
        {
            this.LayoutId = this.Reader.ReadInt32();
            this.EditMode = this.Reader.ReadBoolean();
            if (this.Reader.BaseStream.Position + 4 <= this.Reader.BaseStream.Length)
            {
                this.Tick = this.Reader.ReadUInt32();
            }
        }

        internal override void Process()
        {
        }
    }
}
