using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using CSS.Core;
using CSS.Files.Logic;

namespace CSS.Logic
{
    internal class ResourceProductionComponent : Component
    {
        public ResourceProductionComponent(ConstructionItem ci, Level level) : base(ci)
        {
            m_vTimeSinceLastClick = (level?.Avatar != null && level.Avatar.LastTickSaved > new DateTime(2010, 1, 1))
                ? level.Avatar.LastTickSaved
                : DateTime.UtcNow;
            m_vProductionResourceData =
           CSVManager.DataTables.GetResourceByName(((BuildingData) ci.GetData()).ProducesResource);
            m_vResourcesPerHour = ((BuildingData) ci.GetData()).ResourcePerHour;
            m_vMaxResources = ((BuildingData) ci.GetData()).ResourceMax;
        }

        public override int Type => 5;

        readonly List<int> m_vMaxResources;
        readonly ResourceData m_vProductionResourceData;
        readonly List<int> m_vResourcesPerHour;
        DateTime m_vTimeSinceLastClick;

        public void CollectResources()
        {
            var ci = (ConstructionItem) GetParent();
            var now = ci.Avatar?.Avatar?.LastTickSaved ?? DateTime.UtcNow;
            if (m_vTimeSinceLastClick < new DateTime(2010, 1, 1))
            {
                m_vTimeSinceLastClick = now;
            }
            var span = now - m_vTimeSinceLastClick;
            float currentResources = 0;
            if (!ci.IsBoosted)
            {
                currentResources = m_vResourcesPerHour[ci.UpgradeLevel] / (60f * 60f) * (float) Math.Max(0, span.TotalSeconds);
            }
            else
            {
                if (ci.GetBoostEndTime() >= now)
                {
                    currentResources = m_vResourcesPerHour[ci.UpgradeLevel] / (60f * 60f) * (float) Math.Max(0, span.TotalSeconds);
                    currentResources *= ci.GetBoostMultipier();
                }
                else
                {
                    var boostedTime = (float) Math.Max(0, span.TotalSeconds) - (float) (now - ci.GetBoostEndTime()).TotalSeconds;
                    var notBoostedTime = (float) Math.Max(0, span.TotalSeconds) - boostedTime;
                    currentResources = m_vResourcesPerHour[ci.UpgradeLevel] / (60f * 60f) * notBoostedTime;
                    currentResources += m_vResourcesPerHour[ci.UpgradeLevel] / (60f * 60f) * boostedTime * ci.GetBoostMultipier();
                    ci.IsBoosted = false;
                }
            }

            currentResources = Math.Min(Math.Max(currentResources, 0), m_vMaxResources[ci.UpgradeLevel]);

            if (currentResources >= 1)
            {
                var ca = ci.Avatar.Avatar;
                if (ca.GetResourceCap(m_vProductionResourceData) >= ca.GetResourceCount(m_vProductionResourceData))
                {
                    if (ca.GetResourceCap(m_vProductionResourceData) - ca.GetResourceCount(m_vProductionResourceData) <
                        currentResources)
                    {
                        var newCurrentResources = ca.GetResourceCap(m_vProductionResourceData) - ca.GetResourceCount(m_vProductionResourceData);
                        m_vTimeSinceLastClick = ci.Avatar
                              .Avatar.LastTickSaved
                              .AddSeconds(-((currentResources - newCurrentResources) / (m_vResourcesPerHour[ci.UpgradeLevel] / (60f * 60f))));
                        currentResources = newCurrentResources;
                    }
                    else
                    {
                        m_vTimeSinceLastClick = ci.Avatar.Avatar.LastTickSaved;
                    }

                    ca.CommodityCountChangeHelper(0, m_vProductionResourceData, (int) currentResources);
                }
            }
        }

        public override void Load(JObject jsonObject)
        {
            var productionObject = (JObject) jsonObject["production"];
            if (productionObject != null && productionObject["t_lastClick"] != null)
            {
                try
                {
                    m_vTimeSinceLastClick = productionObject["t_lastClick"].ToObject<DateTime>();
                }
                catch
                {
                    m_vTimeSinceLastClick = DateTime.UtcNow;
                }
            }
            if (m_vTimeSinceLastClick <= DateTime.MinValue || m_vTimeSinceLastClick < new DateTime(2010, 1, 1))
            {
                m_vTimeSinceLastClick = DateTime.UtcNow;
            }
        }

        public void Reset()
        {
            m_vTimeSinceLastClick = GetParent().Avatar?.Avatar?.LastTickSaved ?? DateTime.UtcNow;
        }

        public override JObject Save(JObject jsonObject)
        {
            var ci = (ConstructionItem) GetParent();
            if (ci.GetUpgradeLevel() != -1)
            {
                if (m_vTimeSinceLastClick <= DateTime.MinValue || m_vTimeSinceLastClick < new DateTime(2010, 1, 1))
                {
                    m_vTimeSinceLastClick = DateTime.UtcNow;
                }
                var now = ci.Avatar?.Avatar?.LastTickSaved ?? DateTime.UtcNow;
                if (now <= DateTime.MinValue || now < new DateTime(2010, 1, 1))
                {
                    now = DateTime.UtcNow;
                }

                var productionObject = new JObject();
                productionObject.Add("t_lastClick", m_vTimeSinceLastClick);
                jsonObject.Add("production", productionObject);

                var seconds = Math.Max(0f, (float) (now - m_vTimeSinceLastClick).TotalSeconds);
                if (ci.IsBoosted)
                {
                    if (ci.GetBoostEndTime() >= now)
                    {
                        seconds *= ci.GetBoostMultipier();
                    }
                    else
                    {
                        var boostedTime = Math.Max(0f, seconds - (float) (now - ci.GetBoostEndTime()).TotalSeconds);
                        var notBoostedTime = seconds - boostedTime;
                        seconds = boostedTime * ci.GetBoostMultipier() + notBoostedTime;
                    }
                }

                int lvl = Math.Max(0, Math.Min(ci.GetUpgradeLevel(), m_vMaxResources.Count - 1));
                float maxRes = m_vMaxResources[lvl];
                float resPerHour = Math.Max(1f, (float) m_vResourcesPerHour[lvl]);
                float totalProdTime = (maxRes / resPerHour) * 3600f;
                int resTime = Math.Max(0, (int) (totalProdTime - seconds));

                jsonObject.Add("res_time", resTime);
            }
            return jsonObject;
        }
    }
}
