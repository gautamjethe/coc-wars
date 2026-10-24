using CSS.Helpers.Binary;

namespace CSS.Packets.Commands.Client
{
    // Packet 552
    internal class SaveVillageLayoutCommand : Command
    {
        public SaveVillageLayoutCommand(Reader reader, Device client, int id) : base(reader, client, id)
        {
        }

        public uint Tick { get; set; }
        public int LayoutId { get; set; }
        public bool War { get; set; }

        internal override void Decode()
        {
            this.Tick = this.Reader.ReadUInt32();
            this.LayoutId = this.Reader.ReadInt32();
            this.War = this.Reader.ReadBoolean();
        }

        internal override void Process()
        {
            if (this.Device?.Player != null)
            {
                CSS.Core.Resources.DatabaseManager.Save(this.Device.Player);
            }
        }
    }
}
