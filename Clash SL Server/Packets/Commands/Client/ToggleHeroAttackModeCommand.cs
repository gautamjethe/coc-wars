using CSS.Core;
using CSS.Helpers.Binary;
using CSS.Logic;

namespace CSS.Packets.Commands.Client
{
    // Packet 572
    internal class ToggleHeroAttackModeCommand : Command
    {
        public ToggleHeroAttackModeCommand(Reader reader, Device client, int id) : base(reader, client, id)
        {
        }

        public int BuildingId { get; set; }
        public uint Tick { get; set; }

        internal override void Decode()
        {
            this.BuildingId = this.Reader.ReadInt32();
            if (this.Reader.BaseStream.Position + 4 <= this.Reader.BaseStream.Length)
            {
                this.Tick = this.Reader.ReadUInt32();
            }
        }

        internal override void Process()
        {
            if (this.Device?.Player != null)
            {
                var go = this.Device.Player.GameObjectManager.GetGameObjectByID(this.BuildingId);
                if (go != null)
                {
                    var ci = go as ConstructionItem;
                    if (ci != null)
                    {
                        ci.AttackMode = !ci.AttackMode;
                        ci.AttackModeWar = ci.AttackMode;
                    }

                    Logger.Write($"[CMD] ToggleHeroAttackMode executed for Building {this.BuildingId}");
                    CSS.Core.Resources.DatabaseManager.Save(this.Device.Player);
                }
            }
        }
    }
}