using System;
using CSS.Helpers.List;
using CSS.Logic;

namespace CSS.Packets.Messages.Server
{
    // Packet 24336
    internal class AllianceWarHistoryMessage : Message
    {
        readonly Alliance m_vHomeAlliance;
        public AllianceWarHistoryMessage(Device client, Alliance home) : base(client)
        {
            this.Identifier = 24336;
            m_vHomeAlliance = home;
        }

        internal override void Encode()
        {
            if (m_vHomeAlliance == null)
            {
                this.Data.AddInt(0);
                return;
            }

            var history = ClanWarManager.GetWarHistory(m_vHomeAlliance.m_vAllianceId);
            this.Data.AddInt(history.Count);

            foreach (var war in history)
            {
                var homeClan = war.GetClan(m_vHomeAlliance.m_vAllianceId) ?? war.Clan1;
                var enemyClan = war.GetOpponentClan(m_vHomeAlliance.m_vAllianceId) ?? war.Clan2;
                if (homeClan == null || enemyClan == null) continue;

                this.Data.AddLong(homeClan.ClanId);
                this.Data.AddString(homeClan.ClanName);
                this.Data.AddInt(homeClan.ClanBadge);
                this.Data.AddInt(homeClan.ClanLevel);

                this.Data.AddLong(enemyClan.ClanId);
                this.Data.AddString(enemyClan.ClanName);
                this.Data.AddInt(enemyClan.ClanBadge);
                this.Data.AddInt(enemyClan.ClanLevel);

                this.Data.AddInt(homeClan.TotalStars);
                this.Data.AddInt(enemyClan.TotalStars);

                this.Data.AddInt((int)homeClan.TotalDestruction);
                this.Data.AddInt((int)enemyClan.TotalDestruction);

                this.Data.AddInt(0);
                this.Data.AddInt(0);

                this.Data.AddInt(homeClan.AttacksUsed);
                this.Data.AddInt(50 + homeClan.TotalStars * 5); // XP Earned

                this.Data.AddLong(war.WarId);
                this.Data.AddLong(war.WarSize);

                bool won = homeClan.TotalStars > enemyClan.TotalStars || (homeClan.TotalStars == enemyClan.TotalStars && homeClan.TotalDestruction > enemyClan.TotalDestruction);
                this.Data.AddInt(won ? 1 : 0);

                this.Data.Add(99);
                int timeAgo = (int)Math.Max(60, (DateTime.UtcNow - war.WarEndedTime).TotalSeconds);
                this.Data.AddInt(timeAgo);
                this.Data.AddLong(timeAgo);
            }
        }
    }
}
