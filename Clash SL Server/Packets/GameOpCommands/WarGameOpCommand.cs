using System;
using System.Linq;
using CSS.Core;
using CSS.Core.Network;
using CSS.Core.Threading;
using CSS.Logic;
using CSS.Logic.AvatarStreamEntry;
using CSS.Packets.Messages.Server;

namespace CSS.Packets.GameOpCommands
{
    internal class WarGameOpCommand : GameOpCommand
    {
        readonly string[] m_vArgs;

        public WarGameOpCommand(string[] args)
        {
            m_vArgs = args;
            SetRequiredAccountPrivileges(0);
        }

        public override async void Execute(Level level)
        {
            if (level?.Avatar == null || level.Client == null) return;

            long allianceId = level.Avatar.AllianceId;
            Alliance alliance = allianceId > 0 ? ObjectManager.GetAlliance(allianceId) : null;

            if (m_vArgs.Length <= 1 || m_vArgs[1] == "help")
            {
                SendFeedback(level, "--- CLAN WAR COMMANDS ---\n" +
                                    "/war start [size] - Start a war (5, 10, 15, etc.)\n" +
                                    "/war fast - Toggle fast mode (2m prep, 5m battle)\n" +
                                    "/war skip - Skip current phase\n" +
                                    "/war end - Force-end war and distribute rewards\n" +
                                    "/war status - View current war status\n" +
                                    "/war attack <pos> <stars> <pct> - Test attack on enemy base");
                return;
            }

            string sub = m_vArgs[1].ToLower();

            if (sub == "start")
            {
                if (alliance == null)
                {
                    SendFeedback(level, "You must be in a clan to start a war.");
                    return;
                }

                var memberEntry = alliance.m_vAllianceMembers.ContainsKey(level.Avatar.UserId) ? alliance.m_vAllianceMembers[level.Avatar.UserId] : null;
                if (memberEntry != null && memberEntry.Role < 2 && level.Avatar.AccountPrivileges < 1)
                {
                    SendFeedback(level, "Only Leader or Co-Leaders can start a Clan War.");
                    return;
                }

                var existingWar = ClanWarManager.GetWarByClanId(allianceId);
                if (existingWar != null && existingWar.State != ClanWarState.WAR_ENDED)
                {
                    SendFeedback(level, $"Your clan is already in a war! (State: {existingWar.State}, War ID: {existingWar.WarId})");
                    return;
                }

                int size = 10;
                if (m_vArgs.Length >= 3 && int.TryParse(m_vArgs[2], out int parsedSize))
                {
                    size = parsedSize;
                }
                else
                {
                    int eligibleCount = alliance.GetAllianceMembers().Count;
                    if (eligibleCount <= 5) size = 5;
                    else if (eligibleCount <= 10) size = 10;
                    else if (eligibleCount <= 15) size = 15;
                    else size = 20;
                }

                var war = await ClanWarManager.StartWar(alliance, null, size);
                if (war != null)
                {
                    SendFeedback(level, $"Clan War started successfully! War ID: {war.WarId}, Size: {war.WarSize}v{war.WarSize}.\nPreparation Day is now active.");
                }
                else
                {
                    SendFeedback(level, "Failed to start Clan War.");
                }
                return;
            }

            if (sub == "fast")
            {
                ClanWarManager.WarFastMode = !ClanWarManager.WarFastMode;
                string modeStr = ClanWarManager.WarFastMode ? "ENABLED (2m Prep, 5m Battle)" : "DISABLED (24h Prep, 24h Battle)";
                
                if (alliance != null)
                {
                    var war = ClanWarManager.GetWarByClanId(allianceId);
                    if (war != null && ClanWarManager.WarFastMode)
                    {
                        if (war.State == ClanWarState.PREPARATION_DAY)
                        {
                            war.PreparationEndTime = DateTime.UtcNow.AddMinutes(2);
                            war.BattleStartTime = war.PreparationEndTime;
                            war.BattleEndTime = war.BattleStartTime.AddMinutes(5);
                        }
                        else if (war.State == ClanWarState.BATTLE_DAY)
                        {
                            war.BattleEndTime = DateTime.UtcNow.AddMinutes(5);
                        }
                        war.BroadcastWarMapUpdate();
                    }
                }

                SendFeedback(level, $"Clan War Fast Mode is now {modeStr}.");
                return;
            }

            if (sub == "skip")
            {
                if (alliance == null)
                {
                    SendFeedback(level, "You must be in a clan.");
                    return;
                }

                var war = ClanWarManager.GetWarByClanId(allianceId);
                if (war == null || war.State == ClanWarState.WAR_ENDED || war.State == ClanWarState.NOT_IN_WAR)
                {
                    SendFeedback(level, "Your clan has no active war to skip.");
                    return;
                }

                var oldState = war.State;
                ClanWarManager.SkipPhase(war);
                SendFeedback(level, $"Skipped {oldState}! War is now in {war.State}.");
                return;
            }

            if (sub == "end")
            {
                if (alliance == null)
                {
                    SendFeedback(level, "You must be in a clan.");
                    return;
                }

                var war = ClanWarManager.GetWarByClanId(allianceId);
                if (war == null || war.State == ClanWarState.WAR_ENDED || war.State == ClanWarState.NOT_IN_WAR)
                {
                    SendFeedback(level, "Your clan has no active war to end.");
                    return;
                }

                ClanWarManager.ForceEnd(war);
                SendFeedback(level, $"Clan War {war.WarId} force-ended! XP and war loot bonus have been distributed.");
                return;
            }

            if (sub == "status")
            {
                if (alliance == null)
                {
                    SendFeedback(level, "You must be in a clan.");
                    return;
                }

                var war = ClanWarManager.GetWarByClanId(allianceId);
                if (war == null || war.State == ClanWarState.NOT_IN_WAR)
                {
                    SendFeedback(level, $"Clan '{alliance.m_vAllianceName}' is not currently in a war.");
                    return;
                }

                var homeClan = war.GetClan(allianceId);
                var enemyClan = war.GetOpponentClan(allianceId);

                int rem = war.RemainingTimeSeconds;
                int hours = rem / 3600;
                int minutes = (rem % 3600) / 60;
                int seconds = rem % 60;

                string statusMsg = $"--- CLAN WAR STATUS (War #{war.WarId}) ---\n" +
                                   $"State: {war.State} | Time Remaining: {hours:D2}h {minutes:D2}m {seconds:D2}s\n" +
                                   $"Size: {war.WarSize}v{war.WarSize}\n" +
                                   $"{homeClan?.ClanName}: {homeClan?.TotalStars}* ({homeClan?.TotalDestruction:F1}%) | Atks: {homeClan?.AttacksUsed}/{war.WarSize * 2}\n" +
                                   $"{enemyClan?.ClanName}: {enemyClan?.TotalStars}* ({enemyClan?.TotalDestruction:F1}%) | Atks: {enemyClan?.AttacksUsed}/{war.WarSize * 2}\n" +
                                   $"Fast Mode: {(ClanWarManager.WarFastMode ? "ON" : "OFF")}";

                SendFeedback(level, statusMsg);
                return;
            }

            if (sub == "attack")
            {
                if (alliance == null)
                {
                    SendFeedback(level, "You must be in a clan.");
                    return;
                }

                var war = ClanWarManager.GetWarByClanId(allianceId);
                if (war == null || war.State != ClanWarState.BATTLE_DAY)
                {
                    SendFeedback(level, "War is not currently in Battle Day.");
                    return;
                }

                if (m_vArgs.Length < 5)
                {
                    SendFeedback(level, "Usage: /war attack <pos 1..N> <stars 0..3> <pct 0..100>");
                    return;
                }

                if (!int.TryParse(m_vArgs[2], out int pos) || !int.TryParse(m_vArgs[3], out int stars) || !int.TryParse(m_vArgs[4], out int pct))
                {
                    SendFeedback(level, "Invalid parameters. Usage: /war attack <pos> <stars> <pct>");
                    return;
                }

                var enemyClan = war.GetOpponentClan(allianceId);
                int index = pos - 1;
                if (enemyClan == null || index < 0 || index >= enemyClan.Members.Count)
                {
                    SendFeedback(level, $"Target position {pos} is out of bounds (1..{enemyClan?.Members.Count ?? 0}).");
                    return;
                }

                var enemyMember = enemyClan.Members[index];
                bool ok = war.RecordAttack(level.Avatar.UserId, enemyMember.UserId, stars, pct, 120);
                if (ok)
                {
                    SendFeedback(level, $"Recorded attack on #{pos} ({enemyMember.Name})! Result: {stars}* ({pct}%).");
                }
                else
                {
                    SendFeedback(level, "Attack failed. Attacker may have exhausted their 2 war attacks.");
                }
                return;
            }

            SendFeedback(level, "Unknown war command. Type '/war help' for available commands.");
        }

        private static void SendFeedback(Level level, string message)
        {
            try
            {
                var p = new GlobalChatLineMessage(level.Client)
                {
                    Message = message,
                    HomeId = 0,
                    CurrentHomeId = 0,
                    LeagueId = 22,
                    PlayerName = "Clan War Admin"
                };
                Processor.Send(p);

                if (message.Contains("\n"))
                {
                    var mail = new AllianceMailStreamEntry
                    {
                        ID = (int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds,
                        IsNew = 2,
                        AllianceId = 0,
                        AllianceBadgeData = 1526735450,
                        AllianceName = "Clan War Admin",
                        Message = message
                    };
                    mail.SetSender(level.Avatar);
                    var entryMsg = new AvatarStreamEntryMessage(level.Client);
                    entryMsg.SetAvatarStreamEntry(mail);
                    Processor.Send(entryMsg);
                }
            }
            catch (Exception) { }
        }
    }
}
