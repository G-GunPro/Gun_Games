using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TeamResourceManager : MonoBehaviour,IGameSystem
{
    private int teamResource;
    private Dictionary<string, int> personalResource = new();
    PhotonView pv;
    public void Init()
    {
        teamResource = 0;
        personalResource.Clear();
        pv=GetComponent<PhotonView>();
    }

    public void ReleaseSystem()
    {
        teamResource = 0;
        personalResource.Clear();
        Debug.Log("[TeamResourceManager] 초기화 완료");
    }

    public void UpdateSystem()
    {
    }

    // 웨이브 클리어시 호출
    public void GiveWaveReward()
    {
        teamResource += 1;

        foreach (var player in PhotonNetwork.playerList)
        {
            string nick = player.NickName;
            if (!personalResource.ContainsKey(nick))
                personalResource[nick] = 0;

            personalResource[nick] += 1;
        }

        Debug.Log($"[TeamResourceManager] 팀재화 : {teamResource}, 개인재화 지급완료");

        string localNick = PhotonNetwork.player.NickName;

        UIManager ui = GameManager.Instance.uiManager;
        if (ui != null)
        {
            ui.UpdateTeamResourceUI(teamResource);
            if (personalResource.ContainsKey(localNick))
                ui.UpdatePersonalResourceUI(localNick, personalResource[localNick]);
        }
        else
        {
            Debug.LogWarning("[TeamResource] UIManager가 null입니다. 나중에 UI 갱신 필요");
        }
        pv.RPC("RPC_SyncPersonalResource", PhotonTargets.All, localNick, personalResource[localNick]);

        Debug.Log($"[DEBUG] {PhotonNetwork.player.NickName} 개인재화 = {personalResource[localNick]}");

    }
    [PunRPC]
void RPC_SyncPersonalResource(string nick, int value)
{
    personalResource[nick] = value;
    if (nick == PhotonNetwork.player.NickName)
    {
        GameManager.Instance.uiManager.UpdatePersonalResourceUI(nick, value);
    }
}



    public bool TryUseTeamResource(int cost)
    {
        if (teamResource < cost) return false;
        teamResource -= cost;
        
        UIManager ui = GameManager.Instance.uiManager;
        ui.UpdateTeamResourceUI(teamResource);
        return true;
    }

    public bool TryUsePersonalResource(string nickname, int cost)
    {
        if (!personalResource.ContainsKey(nickname) || personalResource[nickname] < cost)
            return false;

        personalResource[nickname] -= cost;
        UIManager ui = GameManager.Instance.uiManager;
        ui.UpdatePersonalResourceUI(nickname, personalResource[nickname]);
        return true;
    }

    public int GetTeamResource() => teamResource;
    public int GetPersonalResource(string nickname)
    => personalResource.ContainsKey(nickname) ? personalResource[nickname] : 0;
}
