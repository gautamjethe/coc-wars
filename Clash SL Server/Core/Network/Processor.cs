using System;
using CSS.Helpers;
using CSS.Packets;

namespace CSS.Core.Network
{
    internal static class Processor
    {
        internal static void Recept(this Message Message)
        {
            Message.Decrypt();
            Message.Decode();
            Message.Process();
        }

        internal static void Send(this Message Message)
        {
            try
            {
                if (Message is Packets.Messages.Server.AllianceWarDataMessage)
                {
                    // Packet 24331 does not exist in CoC client protocol. Do not send to prevent client crash.
                    return;
                }

                Message.Encode();
                Message.Encrypt();
                Resources.Gateway.Send(Message);

                Message.Process();
            }
            catch (Exception)
            {
            }
        }

        internal static Command Handle(this Command Command)
        {
            Command.Encode();

            return Command;
        }
    }
}