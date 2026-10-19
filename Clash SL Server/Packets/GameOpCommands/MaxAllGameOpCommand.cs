using System;
using System.IO;
using CSS.Core;
using CSS.Core.Network;
using CSS.Files.Logic;
using CSS.Logic;
using CSS.Logic.Manager;
using CSS.Packets.Messages.Server;

namespace CSS.Packets.GameOpCommands
{
    internal class MaxAllGameOpCommand : GameOpCommand
    {
        public MaxAllGameOpCommand(string[] args)
        {
            SetRequiredAccountPrivileges(0);
        }

        public override void Execute(Level level)
        {
            if (level.Avatar.AccountPrivileges >= GetRequiredAccountPrivileges())
            {
                try
                {
                    // 1. Load Town Hall 11 full max base layout (all buildings, max defenses, max walls, traps)
                    string jsonPath = "Gamefiles/level/NPC/townhall11.json";
                    if (!File.Exists(jsonPath))
                    {
                        jsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Gamefiles/level/NPC/townhall11.json");
                    }

                    if (File.Exists(jsonPath))
                    {
                        string homeJson = File.ReadAllText(jsonPath);
                        level.GameObjectManager = new GameObjectManager(level);
                        level.WorkerManager = new WorkerManager();
                        level.LoadFromJSON(homeJson);
                    }
                    else
                    {
                        Logger.Write($"[Error] MaxAllGameOpCommand: {jsonPath} not found!");
                    }

                    // 2. Set Town Hall level to 11
                    level.Avatar.m_vTownHallLevel = 11;

                    // 3. Max all characters / troops to highest level
                    var charTable = CSVManager.DataTables.GetTable(3);
                    if (charTable != null)
                    {
                        for (int i = 0; i < charTable.GetItemCount(); i++)
                        {
                            var cd = (CharacterData)charTable.GetItemAt(i);
                            if (cd != null)
                            {
                                int maxLvl = cd.GetUpgradeLevelCount();
                                if (maxLvl > 0)
                                    level.Avatar.SetUnitUpgradeLevel(cd, maxLvl - 1);
                            }
                        }
                    }

                    // 4. Max all spells to highest level
                    var spellTable = CSVManager.DataTables.GetTable(25);
                    if (spellTable != null)
                    {
                        for (int i = 0; i < spellTable.GetItemCount(); i++)
                        {
                            var sd = (SpellData)spellTable.GetItemAt(i);
                            if (sd != null)
                            {
                                int maxLvl = sd.GetUpgradeLevelCount();
                                if (maxLvl > 0)
                                    level.Avatar.SetUnitUpgradeLevel(sd, maxLvl - 1);
                            }
                        }
                    }

                    // 5. Max all heroes (Barbarian King, Archer Queen, Grand Warden)
                    var heroTable = CSVManager.DataTables.GetTable(27);
                    if (heroTable != null)
                    {
                        for (int i = 0; i < heroTable.GetItemCount(); i++)
                        {
                            var hd = (HeroData)heroTable.GetItemAt(i);
                            if (hd != null)
                            {
                                int maxLvl = hd.GetUpgradeLevelCount();
                                if (maxLvl > 0)
                                {
                                    level.Avatar.SetUnitUpgradeLevel(hd, maxLvl - 1);
                                    level.Avatar.SetHeroHealth(hd, 0); // 0 = fully healthy
                                    level.Avatar.SetHeroState(hd, 3); // 3 = active / defending
                                }
                            }
                        }
                    }

                    // 6. Max out resources and gems
                    var gold = CSVManager.DataTables.GetResourceByName("Gold");
                    if (gold != null) level.Avatar.SetResourceCount(gold, 999999999);

                    var elixir = CSVManager.DataTables.GetResourceByName("Elixir");
                    if (elixir != null) level.Avatar.SetResourceCount(elixir, 999999999);

                    var darkElixir = CSVManager.DataTables.GetResourceByName("DarkElixir");
                    if (darkElixir != null) level.Avatar.SetResourceCount(darkElixir, 999999999);

                    level.Avatar.m_vCurrentGems = 999999999;

                    // 7. Set Avatar level (XP) to 150+
                    if (level.Avatar.m_vAvatarLevel < 150)
                    {
                        level.Avatar.m_vAvatarLevel = 150;
                        level.Avatar.m_vExperience = 0;
                    }

                    // 8. Persist fully maxed village to MySQL database
                    Resources.DatabaseManager?.Save(level);

                    Logger.Say($"[Command] /maxall successfully executed for player {level.Avatar.UserId} ({level.Avatar.AvatarName})");

                    // 9. Send reload to client so new village layout & maxed stats take effect
                    Processor.Send(new OutOfSyncMessage(level.Client));
                }
                catch (Exception ex)
                {
                    Logger.Write($"[Error] MaxAllGameOpCommand failed: {ex.Message}");
                }
            }
            else
            {
                SendCommandFailedMessage(level.Client);
            }
        }
    }
}
