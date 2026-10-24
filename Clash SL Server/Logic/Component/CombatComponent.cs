using Newtonsoft.Json.Linq;
using CSS.Core;
using CSS.Files.Logic;

namespace CSS.Logic
{
    internal class CombatComponent : Component
    {
        public CombatComponent(ConstructionItem ci, Level level) : base(ci)
        {
            var bd = (BuildingData) ci.GetData();
            if (bd.AmmoCount != 0)
            {
                m_vAmmo = bd.AmmoCount;
            }
        }

        public override int Type => 1;

        const int m_vType = 0x01AB3F00;

        int m_vAmmo;
        public bool AttackMode { get; set; }
        public bool AttackModeWar { get; set; }

        public void FillAmmo()
        {
            var ca = GetParent().Avatar.Avatar;
            var bd = (BuildingData) GetParent().GetData();
            var rd = CSVManager.DataTables.GetResourceByName(bd.AmmoResource);

            if (ca.HasEnoughResources(rd, bd.AmmoCost))
            {
                ca.CommodityCountChangeHelper(0, rd, bd.AmmoCost);
                m_vAmmo = bd.AmmoCount;
            }
        }

        public void ToggleAttackMode()
        {
            AttackMode = !AttackMode;
            AttackModeWar = AttackMode;
        }

        public void SetAttackMode(bool mode)
        {
            AttackMode = mode;
            AttackModeWar = mode;
        }

        public override void Load(JObject jsonObject)
        {
            if (jsonObject["ammo"] != null)
            {
                m_vAmmo = jsonObject["ammo"].ToObject<int>();
            }
            if (jsonObject["attack_mode"] != null)
            {
                AttackMode = jsonObject["attack_mode"].ToObject<bool>();
            }
            if (jsonObject["attack_mode_war"] != null)
            {
                AttackModeWar = jsonObject["attack_mode_war"].ToObject<bool>();
            }
            else
            {
                AttackModeWar = AttackMode;
            }
        }

        public override JObject Save(JObject jsonObject)
        {
            jsonObject["ammo"] = m_vAmmo;
            var bd = GetParent()?.GetData() as BuildingData;
            if (bd != null && (!string.IsNullOrEmpty(bd.ToggleAttackModeEffect) || bd.AltAttackMode))
            {
                jsonObject["attack_mode"] = AttackMode;
                jsonObject["attack_mode_war"] = AttackModeWar;
                jsonObject["attack_mode_draft"] = AttackMode;
                jsonObject["attack_mode_draft_war"] = AttackModeWar;
            }
            return jsonObject;
        }
    }
}

