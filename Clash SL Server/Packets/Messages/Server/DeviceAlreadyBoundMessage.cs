using System;
using CSS.Helpers.List;
using CSS.Logic;

namespace CSS.Packets.Messages.Server
{
    internal class DeviceAlreadyBoundMessage : Message
    {
        public DeviceAlreadyBoundMessage(Device client, Level level) : base(client)
        {
            this.Identifier = 24262;
            this.Player = level;
        }

        public Level Player;

        internal override void Encode()
        {
            try
            {
                ClientAvatar clientAvatar = this.Player.Avatar;

                this.Data.AddString(null);
                this.Data.Add(1);

                this.Data.AddLong(clientAvatar.UserId);

                this.Data.AddString(clientAvatar.UserToken);
                this.Data.AddRange(clientAvatar.EncodeSync());
            }
            catch (Exception ex)
            {
                CSS.Core.Logger.Write($"[Error] DeviceAlreadyBoundMessage.Encode failed: {ex.Message}");
            }
        }
    }
}