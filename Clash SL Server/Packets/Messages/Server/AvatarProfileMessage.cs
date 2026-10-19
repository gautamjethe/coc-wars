using System;
using CSS.Core;
using CSS.Helpers.List;
using CSS.Logic;

namespace CSS.Packets.Messages.Server
{
    // Packet 24334
    internal class AvatarProfileMessage : Message
    {
        internal Level Level;

        public AvatarProfileMessage(Device client) : base(client)
        {
            this.Identifier = 24334;
        }

        internal override void Encode()
        {
            try
            {
                if (this.Level?.Avatar == null)
                {
                    Logger.Write("[Error] AvatarProfileMessage: Level or Avatar is null!");
                    return;
                }

                byte[] avatarData = this.Level.Avatar.EncodeSync();
                if (avatarData == null || avatarData.Length == 0)
                {
                    Logger.Write("[Error] AvatarProfileMessage: Avatar.EncodeSync() returned empty/null!");
                    return;
                }

                this.Data.AddRange(avatarData);
                this.Data.AddCompressed(this.Level.SaveToJSON(), false);

                this.Data.AddInt(this.Level.Avatar.m_vDonated); // Donated
                this.Data.AddInt(this.Level.Avatar.m_vReceived); // Received
                this.Data.AddInt(0); // War Cooldown

                this.Data.Add(0); // Boolean (libg: readBoolean at 0x1fb680)
                this.Data.AddInt(0); // Int (libg: readInt at 0x1fb68a)
            }
            catch (Exception ex)
            {
                Logger.Write($"[Error] AvatarProfileMessage encode error: {ex.Message}");
            }
        }
    }
}