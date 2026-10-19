using System;
using System.IO;
using CSS.Core;
using CSS.Core.Network;
using CSS.Helpers;
using CSS.Helpers.Binary;
using CSS.Logic;
using CSS.Packets.Messages.Server;

namespace CSS.Packets.Messages.Client
{
    // Packet 14325
    internal class AskForAvatarProfileMessage : Message
    {
        public AskForAvatarProfileMessage(Device Device, Reader Reader) : base(Device, Reader)
        {
        }

        long m_vAvatarId;
        long m_vCurrentHomeId;

        internal override void Decode()
        {
            this.m_vAvatarId = this.Reader.ReadInt64();
            if (this.Reader.ReadBoolean())
                this.m_vCurrentHomeId = this.Reader.ReadInt64();
        }

        internal override async void Process()
        {
            try
            {
                long targetId = this.m_vAvatarId;
                if (targetId == 0 && this.Device?.Player?.Avatar != null)
                {
                    targetId = this.Device.Player.Avatar.UserId;
                }

                Logger.Say($"[AskForAvatarProfile] Handling profile request for targetId={targetId} (requester={this.Device?.Player?.Avatar?.UserId})");

                Level targetLevel = null;
                if (this.Device?.Player?.Avatar != null && this.Device.Player.Avatar.UserId == targetId)
                {
                    targetLevel = this.Device.Player;
                }
                else
                {
                    targetLevel = await ResourcesManager.GetPlayer(targetId);
                }

                if (targetLevel != null)
                {
                    targetLevel.Tick();
                    new AvatarProfileMessage(this.Device) { Level = targetLevel }.Send();
                    Logger.Say($"[AskForAvatarProfile] AvatarProfileMessage (24334) sent for targetId={targetId}");
                }
                else
                {
                    Logger.Write($"[Error] AskForAvatarProfile: Target player {targetId} could not be found!");
                }
            }
            catch (Exception ex)
            {
                Logger.Write($"[Error] AskForAvatarProfileMessage process error: {ex.Message}");
            }
        }
    }
}
