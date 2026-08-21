using System.Collections;
using System.Text;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using InnerNet;
using TONX.Modules;
using TONX.Roles.AddOns.Common;
using TONX.Roles.Crewmate;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TONX;

[HarmonyPatch]
public static class MeetingHudPatch
{
    public static List<bool> FirstCastVote = Enumerable.Repeat(false, 15).ToList();

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.CheckForEndVoting))]
    class CheckForEndVotingPatch
    {
        public static bool Prefix()
        {
            if (!AmongUsClient.Instance.AmHost) return true;
            MeetingVoteManager.Instance?.CheckAndEndMeeting();
            return false;
        }
    }
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.PopulateResults))]
    class PopulateResultsPatch
    {
        public static void Prefix(MeetingHud __instance)
        {
            __instance.StartCoroutine(CoAnimateSwapVote(__instance).WrapToIl2Cpp()); // 换票动画
        }
    }
    public static IEnumerator CoAnimateSwapVote(MeetingHud __instance)
    {
        var meetingVoteManager = MeetingVoteManager.Instance;
        if (meetingVoteManager == null) yield break;

        var swappedPlayers = meetingVoteManager.SwappedPlayers.ToList();
        foreach (var data in swappedPlayers)
        {
            if (!data.ShouldAnimate) continue;
            if ((Utils.GetPlayerById(data.Target1)?.Data?.IsDead ?? true) || (Utils.GetPlayerById(data.Target2)?.Data?.IsDead ?? true)) continue;

            var pva1 = __instance.playerStates.FirstOrDefault(p => p.PlayerId == data.Target1);
            var pva2 = __instance.playerStates.FirstOrDefault(p => p.PlayerId == data.Target2);
            if (pva1 == null || pva2 == null) continue;

            var time = 1.5f / swappedPlayers.Select(p => p.ShouldAnimate).Count();
            __instance.StartCoroutine(Effects.Slide3D(pva1.transform, pva1.transform.localPosition, pva2.transform.localPosition, time));
            __instance.StartCoroutine(Effects.Slide3D(pva2.transform, pva2.transform.localPosition, pva1.transform.localPosition, time));
            yield return new WaitForSeconds(time);
        }
    }
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.CastVote))]
    public static class CastVotePatch
    {
        public static bool Prefix(MeetingHud __instance, [HarmonyArgument(0)] PlayerId srcPlayerId /* 投票者 */ , [HarmonyArgument(1)] PlayerId suspectPlayerId /* 被票者 */ )
        {
            if (!AmongUsClient.Instance.AmHost) return true;

            byte srcId = srcPlayerId;
            byte suspectId = suspectPlayerId;
            var voter = Utils.GetPlayerById(srcId);
            var voted = Utils.GetPlayerById(suspectId);

            if (voter != null)
            {
                //主动叛变模式
                if (CustomRoles.Madmate.IsEnable() && Options.MadmateSpawnMode.GetInt() == 2 && srcId == suspectId)
                {
                    if (FirstCastVote[srcId])
                    {
                        if (Main.AllPlayerControls.Count(p => p.Is(CustomRoles.Madmate)) < CustomRoles.Madmate.GetCount() && voter.CanBeMadmate())
                        {
                            voter.RpcSetCustomRole(CustomRoles.Madmate);
                            Logger.Info($"注册附加职业：{voter.GetNameWithRole()} => {CustomRoles.Madmate}", "AssignCustomSubRoles");
                            voter.ShowPopUp(GetString("MadmateSelfVoteModeSuccessfulMutiny"));
                            Utils.SendMessage(GetString("MadmateSelfVoteModeSuccessfulMutiny"), voter.PlayerId);
                        }
                        else
                        {
                            voter.ShowPopUp(GetString("MadmateSelfVoteModeMutinyFailed"));
                            Utils.SendMessage(GetString("MadmateSelfVoteModeMutinyFailed"), voter.PlayerId);
                        }
                        __instance.RpcClearVote(voter.PlayerId);
                        Logger.Info($"{voter.GetNameWithRole()} 的投票被清除", nameof(CastVotePatch));
                        FirstCastVote[srcId] = false;
                        return false;
                    }
                }
                
                if (voter.GetRoleClass()?.CheckVoteAsVoter(voted) == false)
                {
                    __instance.RpcClearVote(voter.PlayerId);
                    Logger.Info($"{voter.GetNameWithRole()} 的投票被清除", nameof(CastVotePatch));
                    return false;
                }
                if (CustomRoleManager.CheckVoteOthers(voter, voted) == false)
                {
                    __instance.RpcClearVote(voter.PlayerId);
                    Logger.Info($"{voter.GetNameWithRole()} 的投票被清除", nameof(CastVotePatch));
                    return false;
                }
            }

            MeetingVoteManager.Instance?.SetVote(srcId, suspectId);
            return true;
        }
    }
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
    [HarmonyPriority(Priority.First)]
    class StartPatch
    {
        public static void Prefix()
        {
            Logger.Info("------------会议开始------------", "Phase");
            ChatUpdatePatch.DoBlockChat = true;
            GameStates.AlreadyDied |= !Utils.IsAllAlive;
            Main.AllPlayerControls.Do(x => ReportDeadBodyPatch.WaitReport[x.PlayerId].Clear());
            FirstCastVote = Enumerable.Repeat(true, 15).ToList();
            MeetingStates.MeetingCalled = true;
        }
        public static void Postfix(MeetingHud __instance)
        {
            MeetingVoteManager.Start();

            SoundManager.Instance.ChangeAmbienceVolume(0f);
            if (!GameStates.IsModHost) return;
            // 会议开始重新给色盲客户端发送偏移外观（抵消 GameData 同步造成的覆盖）
            TONX.Roles.AddOns.Common.Colorblind.RpcSetSkinAll();
            var myRole = PlayerControl.LocalPlayer.GetRoleClass();
            foreach (var pva in __instance.playerStates)
            {
                var pc = Utils.GetPlayerById(pva.PlayerId);
                if (pc == null) continue;
                var roleTextMeeting = Object.Instantiate(pva.NameText);
                roleTextMeeting.transform.SetParent(pva.NameText.transform);
                roleTextMeeting.transform.localPosition = new Vector3(0f, -0.18f, 0f);
                roleTextMeeting.fontSize = 1.5f;
                (roleTextMeeting.enabled, roleTextMeeting.text)
                    = Utils.GetRoleNameAndProgressTextData(PlayerControl.LocalPlayer, pc);
                roleTextMeeting.gameObject.name = "RoleTextMeeting";
                roleTextMeeting.enableWordWrapping = false;

                // 役職とサフィックスを同時に表示する必要が出たら要改修
                var suffixBuilder = new StringBuilder(32);
                if (myRole != null)
                {
                    suffixBuilder.Append(myRole.GetSuffix(PlayerControl.LocalPlayer, pc, isForMeeting: true));
                }
                suffixBuilder.Append(CustomRoleManager.GetSuffixOthers(PlayerControl.LocalPlayer, pc, isForMeeting: true));
                if (suffixBuilder.Length > 0)
                {
                    roleTextMeeting.text = suffixBuilder.ToString();
                    roleTextMeeting.enabled = true;
                }
            }

            if (Options.SyncButtonMode.GetBool())
            {
                Utils.SendMessage(string.Format(GetString("Message.SyncButtonLeft"), Options.SyncedButtonCount.GetFloat() - Options.UsedButtonCount));
                Logger.Info("紧急会议剩余 " + (Options.SyncedButtonCount.GetFloat() - Options.UsedButtonCount) + " 次使用次数", "SyncButtonMode");
            }
            if (AntiBlackout.OverrideExiledPlayer && !Options.NoGameEnd.GetBool())
            {
                _ = new LateTask(() =>
                {
                    Utils.SendMessage(GetString("Warning.OverrideExiledPlayer"), 255, Utils.ColorString(Color.red, GetString("DefaultSystemMessageTitle")));
                }, 5f, "Warning OverrideExiledPlayer");
            }
            if (MeetingStates.FirstMeeting) TemplateManager.SendTemplate("OnFirstMeeting", noErr: true);
            TemplateManager.SendTemplate("OnMeeting", noErr: true);

            if (AmongUsClient.Instance.AmHost)
            {
                _ = new LateTask(() =>
                {
                    foreach (var seer in Main.AllPlayerControls)
                    {
                        if (seer.IsModClient()) continue;
                        var sender = CustomRpcSender.Create("SetNameToChat", Hazel.SendOption.Reliable);
                        sender.StartMessage(seer.GetClientId());

                        foreach (var seen in Main.AllPlayerControls)
                        {
                            var seenName = seen.GetRealName(isMeeting: true);
                            var coloredName = Utils.ColorString(seen.GetRoleColor(), seenName);
                            sender.RpcSetName(seen, seer == seen ? coloredName : seenName, seer);
                        }
                        sender.SendMessage();
                    }
                    ChatUpdatePatch.DoBlockChat = false;
                }, 3f, "SetName To Chat");
            }

            if (AmongUsClient.Instance.AmHost)
            {
                CustomRoleManager.AllActiveRoles.Values.ToList().Do(role => role.OnStartMeeting());
                Options.CurrentGameMode.GetModeClass()?.OnStartMeeting();
                MeetingStartNotify.OnMeetingStart();
                Tiebreaker.OnMeetingStart();
            }

            foreach (var pva in __instance.playerStates)
            {
                if (pva == null) continue;
                var seer = PlayerControl.LocalPlayer;
                var seerRole = seer.GetRoleClass();

                var target = Utils.GetPlayerById(pva.PlayerId);
                if (target == null) continue;

                var sb = new StringBuilder();

                //会議画面での名前変更
                //自分自身の名前の色を変更
                //NameColorManager準拠の処理
                pva.NameText.text = pva.NameText.text.ApplyNameColorData(seer, target, true);

                var overrideName = pva.NameText.text;
                //调用职业类通过 seer 重写 name
                seer.GetRoleClass()?.OverrideNameAsSeer(target, ref overrideName, true);
                //调用职业类通过 seen 重写 name
                target.GetRoleClass()?.OverrideNameAsSeen(seer, ref overrideName, true);
                pva.NameText.text = overrideName;

                //とりあえずSnitchは会議中にもインポスターを確認することができる仕様にしていますが、変更する可能性があります。

                if (seer.KnowDeathReason(target))
                    sb.Append($"({Utils.ColorString(Utils.GetRoleColor(CustomRoles.Doctor), Utils.GetVitalText(target.PlayerId))})");

                sb.Append(seerRole?.GetMark(seer, target, true));
                sb.Append(CustomRoleManager.GetMarkOthers(seer, target, true));

                bool isLover = false;
                foreach (var subRole in target.GetCustomSubRoles())
                {
                    switch (subRole)
                    {
                        case CustomRoles.Lovers:
                            if (seer.Is(CustomRoles.Lovers) || seer.Data.IsDead)
                            {
                                sb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Lovers), "♡"));
                                isLover = true;
                            }
                            break;
                    }
                }

                //海王相关显示
                if ((seer.Is(CustomRoles.Neptune) || target.Is(CustomRoles.Neptune)) && !seer.Data.IsDead && !isLover)
                    sb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Lovers), "♡"));
                else if (seer == target && CustomRoles.Neptune.IsExist() && !isLover)
                    sb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Lovers), "♡"));

                //会議画面ではインポスター自身の名前にSnitchマークはつけません。

                pva.NameText.text += sb.ToString();
                pva.ColorBlindName.transform.localPosition -= new Vector3(1.35f, 0f, 0f);
            }
        }
    }
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Update))]
    class UpdatePatch
    {
        public static bool Prefix() { return GameManager.Instance is not null; }
        public static void Postfix(MeetingHud __instance)
        {
            if (__instance == null || __instance.IsDestroyedOrNull()) return;
            // 设置自定义会议标题
            if (__instance.CurrentState == MeetingHud.MeetingStates.Discussion)
            {
                var customTitle = Options.CurrentGameMode.GetModeClass()?.GetMeetingTitleText();
                if (!string.IsNullOrEmpty(customTitle) && __instance.TitleText.text != customTitle)
                    __instance.TitleText.text = customTitle;
            }
            if (!AmongUsClient.Instance.AmHost || !GameStates.IsInGame) return;
            if (Input.GetMouseButtonUp(1) && Input.GetKey(KeyCode.LeftControl))
            {
                __instance.playerStates.DoIf(x => x.HighlightedFX.enabled, x =>
                {
                    var player = Utils.GetPlayerById(x.PlayerId);
                    player.RpcExile();
                    var state = PlayerState.GetByPlayerId(player.PlayerId);
                    state.DeathReason = CustomDeathReason.Execution;
                    state.SetDead();
                    Utils.SendMessage(string.Format(GetString("Message.Executed"), player.Data.PlayerName));
                    Logger.Info($"{player.GetNameWithRole()}を処刑しました", "Execution");
                    __instance.CheckForEndVoting();
                });
            }
        }
    }
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.OnDestroy))]
    class OnDestroyPatch
    {
        public static void Postfix()
        {
            if (CustomRoleSelector.RoleAssigned) MeetingStates.FirstMeeting = false;
            Logger.Info("------------会议结束------------", "Phase");
            if (AmongUsClient.Instance.AmHost)
            {
                AntiBlackout.SetIsDead();
                EAC.MeetingTimes = 0;
            }
            // MeetingVoteManagerを通さずに会議が終了した場合の後処理
            MeetingVoteManager.Instance?.Destroy();
        }
    }

    public static void TryAddAfterMeetingDeathPlayers(CustomDeathReason deathReason, params byte[] playerIds)
    {
        var AddedIdList = new List<byte>();
        foreach (var playerId in playerIds)
            if (Main.AfterMeetingDeathPlayers.TryAdd(playerId, deathReason))
                AddedIdList.Add(playerId);
        CheckForDeathOnExile(deathReason, AddedIdList.ToArray());
    }
    public static void CheckForDeathOnExile(CustomDeathReason deathReason, params byte[] playerIds)
    {
        foreach (var playerId in playerIds)
        {
            //Loversの後追い
            if (CustomRoles.Lovers.IsExist(true) && !Main.isLoversDead && Main.LoversPlayers.Find(lp => lp.PlayerId == playerId) != null)
                FixedUpdatePatch.LoversSuicide(playerId, true);
        }
    }
}

[HarmonyPatch(typeof(PlayerVoteArea), nameof(PlayerVoteArea.SetHighlighted))]
class SetHighlightedPatch
{
    public static bool Prefix(PlayerVoteArea __instance, bool value)
    {
        if (!AmongUsClient.Instance.AmHost) return true;
        if (!__instance.HighlightedFX) return false;
        __instance.HighlightedFX.enabled = value;
        return false;
    }
}

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.SetJudgeOverrule))]
class JudgeSetJudgeOverrulePatch
{
    public static bool Prefix(MeetingHud __instance, [HarmonyArgument(0)] PlayerId judgePlayerId, [HarmonyArgument(1)] PlayerId targetPlayerId)
    {
        if (!AmongUsClient.Instance.AmHost) return true;

        var judge = Utils.GetPlayerById(judgePlayerId);
        var target = Utils.GetPlayerById(targetPlayerId);
        if (judge == null || target == null) return true;

        if (judge.GetRoleClass()?.OnCheckOverrule(target) == false)
        {
            Logger.Info($"{judge.GetNameWithRole()} 的否决被 {judge.GetRoleClass()?.GetType().Name} 阻止 => {target.GetNameWithRole()}", "JudgeOverrule");
            var pva = __instance.playerStates.FirstOrDefault(x => (byte)x.PlayerId == (byte)judgePlayerId);
            if (pva != null)
            {
                pva.UnsetVote();
                __instance.RpcClearVote(pva.PlayerId);
            }
            __instance.UpdateForeground();
            return false;
        }
        return true;
    }
}