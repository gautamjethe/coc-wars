using System;
using System.IO;
using CSS.Core;
using CSS.Core.Network;
using CSS.Helpers;
using CSS.Logic;
using CSS.Packets.Messages.Server;
using CSS.Helpers.Binary;
namespace CSS.Packets.Messages.Client
{
    // Packet 14113
    internal class VisitHomeMessage : Message
    {
        public VisitHomeMessage(Device device, Reader reader) : base(device, reader)
        {
        }

        internal long AvatarId;

        internal override void Decode()
        {
            this.AvatarId = this.Reader.ReadInt64();
        }

        internal override async void Process()
        {
            try
            {
                Level targetLevel = await ResourcesManager.GetPlayer(AvatarId);
                if (targetLevel == null && this.Device?.Player?.Avatar?.AllianceId > 0)
                {
                    // Check if AvatarId is an AI member in the active clan war
                    var war = ClanWarManager.GetWarByClanId(this.Device.Player.Avatar.AllianceId);
                    var warMember = war?.GetMember(AvatarId);
                    if (warMember != null)
                    {
                        var clan = war.GetClan(warMember.ClanId);
                        targetLevel = new Level(AvatarId);
                        if (!string.IsNullOrEmpty(warMember.VillageJson))
                        {
                            targetLevel.LoadFromJSON(warMember.VillageJson);
                        }
                        else
                        {
                            targetLevel.LoadFromJSON(ClanWarManager.GetPresetBase(warMember.TownHallLevel));
                        }
                        targetLevel.Avatar.UserId = AvatarId;
                        targetLevel.Avatar.CurrentHomeId = AvatarId;
                        targetLevel.Avatar.AvatarName = warMember.Name;
                        targetLevel.Avatar.m_vTownHallLevel = warMember.TownHallLevel;
                        targetLevel.Avatar.AllianceId = warMember.ClanId;
                    }
                }

                if (targetLevel != null)
                {
                    targetLevel.Tick();
                    new VisitedHomeDataMessage(Device, targetLevel, this.Device.Player).Send();
                }
                else
                {
                    new OutOfSyncMessage(Device).Send();
                }

                if (this.Device.Player.Avatar.AllianceId > 0)
                {
                    Alliance alliance = ObjectManager.GetAlliance(this.Device.Player.Avatar.AllianceId);
                    if (alliance != null)
                    {
                        new AllianceStreamMessage(Device, alliance).Send();
                    }
                }
            }
            catch (Exception)
            {
            }
        }
    }
}
