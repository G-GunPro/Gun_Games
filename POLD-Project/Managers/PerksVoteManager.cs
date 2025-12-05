using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PerksVoteManager : MonoBehaviour
{
    public static PerksVoteManager Instance;
    public float voteDuration = 20f;

    private Dictionary<TeamPerksType, int> voteCounts;
    private Dictionary<string, TeamPerksType> playerVotes = new Dictionary<string, TeamPerksType>();
    private bool votingActive = false;
    private float timer = 0f;
    private PhotonView pv;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        pv = GetComponent<PhotonView>();
        if (pv == null)
            Debug.LogError("[PerksVoteManager] PhotonView가 없습니다");

        DontDestroyOnLoad(gameObject);
    }
    // =====투표 시작======
    public void StartVote()
    {
        if (!PhotonNetwork.isMasterClient) return;

        voteCounts = new Dictionary<TeamPerksType, int>
        {
            {TeamPerksType.ReviveAll,0},
            {TeamPerksType.TeamStatBuff,0},
            {TeamPerksType.WallEnforce,0}
        };

        playerVotes.Clear();
        votingActive = true;
        timer = voteDuration;

        pv.RPC("RPC_StartVoteUI", PhotonTargets.All);

        StartCoroutine(VoteTimer());
    }

    [PunRPC]
    void RPC_StartVoteUI()
    {
        votingActive = true;
        GameManager.Instance.uiManager.ShowTeamPerksChoices();
    }

    //=====투표 전송=====
    public void SubmitVote(TeamPerksType type)
    {
        if (!votingActive)
        {
            Debug.LogWarning("[Vote] 현재 투표가 활성화되어 있지 않습니다");
            return;
        }

        string playerName = PhotonNetwork.player.NickName;
        if (pv == null)
        {
            Debug.LogError("[Vote] PhotonView null! RPC 전송 불가");
            return;
        }

        Debug.Log($"[Vote] {playerName}가 {type} 전송 시도");
        pv.RPC("RPC_SubmitVote", PhotonTargets.MasterClient, playerName, (int)type);
    }

    [PunRPC]
    void RPC_SubmitVote(string playerName, int type)
    {
        if (!PhotonNetwork.isMasterClient) return;

        TeamPerksType t = (TeamPerksType)type;

        if (playerVotes.ContainsKey(playerName))
        {
            TeamPerksType previous = playerVotes[playerName];
            if (previous == t)
            {
                return;
            }
            voteCounts[previous] = Mathf.Max(0, voteCounts[previous] - 1);
        }
        playerVotes[playerName] = t;
        voteCounts[t]++;

        Debug.Log($"[Vote] {playerName}->{t} 한 표 (현재 {voteCounts[t]})");

        foreach (var kv in voteCounts)
            Debug.Log($"   {kv.Key}: {kv.Value}");
        
        pv.RPC("RPC_UpdateVoteCounts", PhotonTargets.AllBuffered, voteCounts[TeamPerksType.ReviveAll],
                                                            voteCounts[TeamPerksType.TeamStatBuff],
                                                            voteCounts[TeamPerksType.WallEnforce]);
    }

    [PunRPC]
    void RPC_UpdateVoteCounts(int revive,int buff, int wall)
    {
        Debug.Log($"[RPC_UpdateVoteCounts] 받은 값 - revive:{revive}, buff:{buff}, wall:{wall}");
        if (GameManager.Instance.uiManager == null)
        {
            Debug.LogError("[Vote] UIManager가 null입니다.");
            return;
        }
    
        GameManager.Instance.uiManager.UpdateVoteUI(revive, buff, wall);
    }
    IEnumerator VoteTimer()
    {
        while (timer > 0)
        {
            yield return new WaitForSeconds(1f);
            timer--;
            pv.RPC("RPC_UpdateTimerUI", PhotonTargets.All, (int)timer);
        }

        if (PhotonNetwork.isMasterClient)
            EndVote();
    }

    void EndVote()
    {
        votingActive = false;

        TeamPerksType result = TeamPerksType.ReviveAll;
        int maxVote = -1;
        foreach (var kv in voteCounts)
        {
            if (kv.Value > maxVote)
            {
                maxVote = kv.Value;
                result = kv.Key;
            }
        }

        Debug.Log($"[Vote] 결과 : {result} ({maxVote}표)");
        pv.RPC("RPC_AnnounceResult", PhotonTargets.All, (int)result);
    }

    [PunRPC]
    void RPC_AnnounceResult(int result)
    {
        TeamPerksType chosen = (TeamPerksType)result;
        GameManager.Instance.perksManager.ApplyTeamPerks(chosen);
        Debug.Log($"[Vote] 최종 특전 적용 : {chosen}");

        GameManager.Instance.uiManager.CloseUI(GameManager.Instance.uiManager.teamPerksPnl);
        GameManager.Instance.uiManager.ShowPersonalPerksChoices();

        if (PhotonNetwork.isMasterClient)
        {
            var stage = FindObjectOfType<StageManager>();
            stage.GetComponent<PhotonView>().RPC("RPC_NotifyNextWave", PhotonTargets.All);
        }
    
    }

    [PunRPC]
    void RPC_UpdateTimerUI(int remaining)
    {
        GameManager.Instance.uiManager.UpdateVoteTimer(remaining);
    }
}
