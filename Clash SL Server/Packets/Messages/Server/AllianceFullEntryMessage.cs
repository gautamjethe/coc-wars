using System;
using System.Collections.Generic;
using CSS.Helpers.List;
using CSS.Logic;

namespace CSS.Packets.Messages.Server
{
    // Packet 24324
    internal class AllianceFullEntryMessage : Message
    {
        readonly Alliance m_vAlliance;

        public AllianceFullEntryMessage(Device client, Alliance alliance) : base(client)
        {
            this.Identifier = 24324;
            this.m_vAlliance = alliance;
        }

        internal override void Encode()
        {
            try
            {
                this.Data.AddString(m_vAlliance.m_vAllianceDescription ?? "");

                // War packets disabled: clan is always encoded as NOT IN WAR (state=0, no warId)
                this.Data.AddInt(0);             // Field 2: War state (0 = NOT_IN_WAR)
                this.Data.AddInt(0);             // Field 3: Remaining countdown
                this.Data.Add((byte)0);          // Field 4: No War ID

                this.Data.AddInt(0);             // Field 5: Unknown int32 (offset 0x4c)
                this.Data.Add((byte)0);          // Field 6: Optional long flag (offset 0x50)
                this.Data.AddRange(m_vAlliance.EncodeFullEntry());
            }
            catch (Exception ex)
            {
                Core.Logger.Write($"[Error] AllianceFullEntryMessage encode error: {ex.Message}");
            }
        }
    }
}
