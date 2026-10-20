using System;
using CSS.Helpers.List;
using CSS.Logic;

namespace CSS.Packets.Messages.Server
{
    // Packet 24113
    internal class VisitedHomeDataMessage : Message
    {
        public VisitedHomeDataMessage(Device client, Level ownerLevel, Level visitorLevel) : base(client)
        {
            this.Identifier = 24113;
            m_vOwnerLevel = ownerLevel;
            m_vVisitorLevel = visitorLevel;
            this.Device.PlayerState = Logic.Enums.State.VISIT;
        }

        internal override void Encode()
        {
            try
            {
                string villageJson = m_vOwnerLevel.SaveToJSON();
                var war = ClanWarManager.GetWarByClanId(m_vVisitorLevel.Avatar.AllianceId);
                if (war != null && war.GetMember(m_vOwnerLevel.Avatar.UserId) != null)
                {
                    if (villageJson.Contains("\"war_base\":false"))
                    {
                        villageJson = villageJson.Replace("\"war_base\":false", "\"war_base\":true");
                    }
                    else if (!villageJson.Contains("\"war_base\":true") && !villageJson.Contains("\"war_base\": true"))
                    {
                        int lastBrace = villageJson.LastIndexOf('}');
                        if (lastBrace > 0)
                        {
                            villageJson = villageJson.Substring(0, lastBrace) + ",\"war_base\":true}";
                        }
                    }
                }

                ClientHome ownerHome = new ClientHome
                {
                    Id = m_vOwnerLevel.Avatar.UserId,
                    ShieldTime = m_vOwnerLevel.Avatar.m_vShieldTime,
                    ProtectionTime = m_vOwnerLevel.Avatar.m_vProtectionTime,
                    Village = villageJson
                };

                this.Data.AddInt(-1);
                this.Data.AddInt((int)TimeSpan.FromSeconds(100).TotalSeconds);
                this.Data.AddRange(ownerHome.Encode);
                this.Data.AddRange(this.m_vOwnerLevel.Avatar.EncodeSync());
                this.Data.AddInt(0);
                this.Data.Add(1);
                this.Data.AddRange(this.m_vVisitorLevel.Avatar.EncodeSync());
            }
            catch (Exception ex)
            {
                CSS.Core.Logger.Write($"[Error] VisitedHomeDataMessage.Encode failed: {ex.Message}");
            }
        }

        readonly Level m_vOwnerLevel;
        readonly Level m_vVisitorLevel;
    }
}
