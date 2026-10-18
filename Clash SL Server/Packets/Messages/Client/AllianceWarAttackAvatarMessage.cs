using System;
using System.Linq;
using System.Threading.Tasks;
using CSS.Core;
using CSS.Core.Network;
using CSS.Files.Logic;
using CSS.Helpers.Binary;
using CSS.Logic;
using CSS.Logic.Enums;
using CSS.Packets.Messages.Server;

namespace CSS.Packets.Messages.Client
{
    // Packet 15001
    internal class AllianceWarAttackAvatarMessage : Message
    {
        public long DefenderId;

        public AllianceWarAttackAvatarMessage(Device device, Reader reader) : base(device, reader)
        {
        }

        internal override void Decode()
        {
            try
            {
                if (this.Reader.BaseStream.Length - this.Reader.BaseStream.Position >= 16)
                {
                    long first = this.Reader.ReadInt64();
                    long second = this.Reader.ReadInt64();

                    var war = ClanWarManager.GetWarByClanId(this.Device.Player.Avatar.AllianceId);
                    if (war != null)
                    {
                        var enemyClan = war.GetOpponentClan(this.Device.Player.Avatar.AllianceId);
                        if (enemyClan != null && enemyClan.Members.Any(m => m.UserId == second))
                            this.DefenderId = second;
                        else if (enemyClan != null && enemyClan.Members.Any(m => m.UserId == first))
                            this.DefenderId = first;
                        else
                            this.DefenderId = second;
                    }
                    else
                    {
                        this.DefenderId = second;
                    }
                }
                else if (this.Reader.BaseStream.Length - this.Reader.BaseStream.Position >= 8)
                {
                    this.DefenderId = this.Reader.ReadInt64();
                }
            }
            catch (Exception)
            {
            }
        }

        internal override async void Process()
        {
            try
            {
                if (this.Device.Player == null) return;

                long allianceId = this.Device.Player.Avatar.AllianceId;
                if (allianceId <= 0)
                {
                    new OutOfSyncMessage(this.Device).Send();
                    return;
                }

                ClanWar war = ClanWarManager.GetWarByClanId(allianceId);
                if (war == null || war.State != ClanWarState.BATTLE_DAY)
                {
                    new OutOfSyncMessage(this.Device).Send();
                    return;
                }

                long attackerId = this.Device.Player.Avatar.UserId;
                var attackerMember = war.GetMember(attackerId);
                if (attackerMember == null || attackerMember.AttacksUsed >= 2)
                {
                    new OutOfSyncMessage(this.Device).Send();
                    return;
                }

                var enemyClan = war.GetOpponentClan(allianceId);
                var defenderMember = enemyClan?.GetMember(this.DefenderId);
                if (defenderMember == null)
                {
                    new OutOfSyncMessage(this.Device).Send();
                    return;
                }

                // Calculate attacker total housing space for troops
                int totalHousing = 0;
                foreach (var unit in this.Device.Player.Avatar.GetUnits())
                {
                    if (unit.Data is CombatItemData cid)
                    {
                        totalHousing += cid.GetHousingSpace() * unit.Value;
                    }
                }

                // Register active battle
                ClanWarManager.SetActiveBattle(
                    attackerId, 
                    war.WarId, 
                    this.DefenderId, 
                    attackerMember.TownHallLevel, 
                    defenderMember.TownHallLevel, 
                    totalHousing
                );

                this.Device.PlayerState = State.WAR_EMODE;

                // Load or create defender Level
                Level defenderLevel = await ResourcesManager.GetPlayer(this.DefenderId);
                if (defenderLevel == null)
                {
                    // AI / preset opponent
                    defenderLevel = new Level(this.DefenderId);
                    if (!string.IsNullOrEmpty(defenderMember.VillageJson))
                    {
                        defenderLevel.LoadFromJSON(defenderMember.VillageJson);
                    }
                    else
                    {
                        defenderLevel.LoadFromJSON(ClanWarManager.GetPresetBase(defenderMember.TownHallLevel));
                    }
                    defenderLevel.Avatar.UserId = this.DefenderId;
                    defenderLevel.Avatar.CurrentHomeId = this.DefenderId;
                    defenderLevel.Avatar.AvatarName = defenderMember.Name;
                    defenderLevel.Avatar.m_vTownHallLevel = defenderMember.TownHallLevel;
                    defenderLevel.Avatar.AllianceId = enemyClan.ClanId;
                }

                defenderLevel.Tick();

                // Send EnemyHomeDataMessage with AttackType = 2 (Clan War battle)
                new EnemyHomeDataMessage(this.Device, defenderLevel, this.Device.Player, 2).Send();
            }
            catch (Exception ex)
            {
                Logger.Write("Error in AllianceWarAttackAvatarMessage: " + ex.Message);
            }
        }
    }
}
