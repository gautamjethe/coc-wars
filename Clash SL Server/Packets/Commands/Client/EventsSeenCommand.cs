using System;
using CSS.Helpers.Binary;

namespace CSS.Packets.Commands.Client
{
    internal class EventsSeenCommand : Command
    {
        public EventsSeenCommand(Reader reader, Device client, int id) : base(reader, client, id)
        {
        }

        public int UnknownID;
        public int Tick;

        internal override void Decode()
        {
            try
            {
                if (this.Reader != null && this.Reader.BaseStream != null)
                {
                    if (this.Reader.BaseStream.Position + 4 <= this.Reader.BaseStream.Length)
                        this.UnknownID = this.Reader.ReadInt32();

                    if (this.Reader.BaseStream.Position + 4 <= this.Reader.BaseStream.Length)
                        this.Tick = this.Reader.ReadInt32();
                }
            }
            catch (Exception)
            {
            }
        }

        internal override void Process()
        {
        }
    }
}
