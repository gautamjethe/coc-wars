using System;
using CSS.Helpers.List;
using CSS.Logic;

namespace CSS.Packets.Messages.Server
{
    // Packet 24107
    internal class EnemyHomeDataMessage : Message
    {
        public int AttackType = 3;

        public EnemyHomeDataMessage(Device client, Level ownerLevel, Level visitorLevel, int attackType = 3) : base(client)
        {
            this.Identifier = 24107;
            this.OwnerLevel = ownerLevel;
            this.VisitorLevel = visitorLevel;
            this.AttackType = attackType;
        }

        internal override async void Encode()
        {
            try
            {
                string villageJson = this.OwnerLevel.SaveToJSON();
                if (this.AttackType == 2)
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

                ClientHome ch = new ClientHome
                {
                    Id = this.OwnerLevel.Avatar.UserId,
                    ShieldTime = this.OwnerLevel.Avatar.m_vShieldTime,
                    Village = villageJson,
                    ProtectionTime = this.OwnerLevel.Avatar.m_vProtectionTime
                };

                this.Data.AddInt((int)TimeSpan.FromSeconds(100).TotalSeconds);
                this.Data.AddInt(-1);
                this.Data.AddInt((int)this.OwnerLevel.Avatar.LastTickSaved.Subtract(new DateTime(1970, 1, 1)).TotalSeconds);
                this.Data.AddRange(ch.Encode);
                this.Data.AddRange(await this.OwnerLevel.Avatar.Encode());
                this.Data.AddRange(await this.VisitorLevel.Avatar.Encode());
                this.Data.AddInt(this.AttackType); // 1 : Friendly ?       2 : Clan War (next disabled)       3 : PvP         5 : Friendly
                this.Data.AddInt(0);
                this.Data.Add(0);
            }
            catch (Exception)
            {
            }
        }

        internal readonly Level OwnerLevel;
        internal readonly Level VisitorLevel;
    }
}
