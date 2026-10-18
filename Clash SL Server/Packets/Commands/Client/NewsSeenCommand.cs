using CSS.Helpers.Binary;

namespace CSS.Packets.Commands.Client
{
    // Packet 539
    internal class NewsSeenCommand : Command
    {
        public byte[] packet;

        public NewsSeenCommand(Reader reader, Device client, int id) : base(reader, client, id)
        {
            //packet = br.ReadAllBytes();
            //Unknown1 = br.ReadUInt32WithEndian();
            //Unknown2 = br.ReadUInt32WithEndian();
        }

        internal override void Decode()
        {
            try
            {
                if (this.Reader != null && this.Reader.BaseStream != null)
                {
                    if (this.Reader.BaseStream.Position + 4 <= this.Reader.BaseStream.Length)
                        this.Unknown1 = this.Reader.ReadInt32();

                    if (this.Reader.BaseStream.Position + 4 <= this.Reader.BaseStream.Length)
                        this.Unknown2 = this.Reader.ReadInt32();
                }
            }
            catch (System.Exception)
            {
            }
        }

        public int Unknown1 { get; set; }
        public int Unknown2 { get; set; }
    }
}