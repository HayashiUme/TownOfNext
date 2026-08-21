using TONX.Modules;
using UnityEngine;

namespace TONX.Roles.Core.Interfaces;

/// <summary>
/// 特殊会议接口。实现该接口的职业或游戏模式可以开启一场逻辑与普通会议不同的特殊会议。
/// </summary>
public interface ISpecialMeeting
{
    /// <summary>
    /// 是否正在主持一场特殊会议
    /// </summary>
    bool IsSpecialMeetingActive { get; }
    
    /// <summary>
    /// 是否允许跳票
    /// </summary>
    bool AllowSkip { get; }

    /// <summary>
    /// 特殊会议参与者 PlayerId 列表。为空列表时表示隐藏所有玩家。
    /// </summary>
    List<byte> SpecialMeetingPlayers { get; }

    /// <summary>
    /// 隐藏非参与者，并将参与者居中排列；空参与者列表时隐藏所有玩家并隐藏跳过按钮。
    /// </summary>
    void HandleSpecialMeeting(MeetingHud __instance)
    {
        var targets = SpecialMeetingPlayers;
        var allowSkip = AllowSkip;
        if (targets == null || targets.Count == 0)
        {
            foreach (var pva in __instance.playerStates) pva.gameObject.SetActive(false);
            if(!allowSkip) __instance.SkipVoteButton.gameObject.SetActive(false);
            return;
        }

        var num = -1;
        foreach (var pva in __instance.playerStates)
        {
            if (!targets.Contains(pva.PlayerId))
            {
                pva.gameObject.SetActive(false);
                continue;
            }
            pva.transform.localPosition = new Vector3(2f * num, 0f, pva.transform.localPosition.z);
            num *= -1;
        }
        __instance.SkipVoteButton.gameObject.SetActive(false);
    }

    /// <summary>
    /// 检查特殊会议中的投票是否合法，返回 false 则取消该投票
    /// </summary>
    /// <param name="voter">投票者</param>
    /// <param name="voted">被投票者</param>
    bool CheckSpecialMeetingVote(PlayerControl voter, PlayerControl voted) => true;

    /// <summary>
    /// 特殊会议投票结束时的自定义处理。<br/>
    /// 返回 true 表示已接管放逐逻辑，跳过正常的投票放逐流程。
    /// </summary>
    /// <param name="voteResult">投票统计结果</param>
    bool OnSpecialMeetingVotingComplete(MeetingVoteManager.VoteResult voteResult) => false;

    /// <summary>
    /// 特殊会议结束后调用来清理
    /// </summary>
    void OnSpecialMeetingEnd() { }
}
