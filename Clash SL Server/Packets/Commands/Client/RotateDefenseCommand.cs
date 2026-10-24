using CSS.Core;
using CSS.Helpers.Binary;
using CSS.Logic;

namespace CSS.Packets.Commands.Client
{
    // Packet 554
    internal class RotateDefenseCommand : Command
    {
        public RotateDefenseCommand(Reader reader, Device client, int id) : base(reader, client, id)
        {
        }

        public int BuildingID { get; set; }
        public uint Tick { get; set; }

        internal override void Decode()
        {
            this.BuildingID = this.Reader.ReadInt32();
            if (this.Reader.BaseStream.Position + 4 <= this.Reader.BaseStream.Length)
            {
                this.Tick = this.Reader.ReadUInt32();
            }
        }

        internal override void Process()
        {
            if (this.Device?.Player != null)
            {
                var go = this.Device.Player.GameObjectManager.GetGameObjectByID(this.BuildingID);
                if (go != null)
                {
                    var ci = go as ConstructionItem;
                    if (ci != null)
                    {
                        ci.AttackMode = !ci.AttackMode;
                        ci.AttackModeWar = ci.AttackMode;
                    }

                    var combat = go.GetComponent(1, true) as CombatComponent;
                    if (combat != null)
                    {
                        if (ci != null)
                            combat.SetAttackMode(ci.AttackMode);
                        else
                            combat.ToggleAttackMode();
                    }

                    Logger.Write($"[CMD] RotateDefense executed for Building {this.BuildingID}");
                    CSS.Core.Resources.DatabaseManager.Save(this.Device.Player);
                }
            }
        }
    }
}

