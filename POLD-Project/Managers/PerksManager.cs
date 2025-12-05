using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PerksManager : MonoBehaviour, IGameSystem
{
    private TeamPerks teamPerks;
    private PersonalPerks personalPerks;
    public WallManager wall;

    // 동일 정수를 중복 선택하지 않게 막는 용도(성공적으로 장착된 타입만 기록)
    private List<EssenceType> usedEssenceType = new();
    private PlayerController localPlayer;

    // ====== EssenceType -> ItemData 매핑 ======
    [System.Serializable]
    public struct EssenceMap
    {
        public EssenceType type;   // 개인특전에서 넘겨주는 정수 타입
        public ItemData item;
    }
    
    [Header("개인특전: Essence 매핑(인스펙터에서 연결)")]
    public List<EssenceMap> essenceMaps = new();

    
    private Dictionary<EssenceType, ItemData> essenceLookup;

    [Header("팀 스탯 보정(누적) - 0.1 = +10%")]
    public float teamAtk, teamDef, teamHP, teamAS, teamMS, teamRegen;

    private PhotonView pv;
    void Awake()
    {
        pv = GetComponent<PhotonView>();
    }
    public void Init()
    {
        usedEssenceType.Clear();
        teamPerks = new TeamPerks();
        personalPerks = new PersonalPerks();
        

        // 매핑 초기화
        essenceLookup = new Dictionary<EssenceType, ItemData>();

        foreach (var m in essenceMaps)
            if (m.item != null) essenceLookup[m.type] = m.item;

        //  팀 버프 초기화
        teamAtk = teamDef = teamHP = teamAS = teamMS = teamRegen = 0f;
    }

    // ====== 팀 특전 ======
    public void ApplyTeamPerks(TeamPerksType type)
    {
        if (PhotonNetwork.isMasterClient)
            pv.RPC("RPC_ApplyTeamPerks", PhotonTargets.AllBuffered, (int)type);
    }

    [PunRPC]
    void RPC_ApplyTeamPerks(int typeInt)
    {
        TeamPerksType type = (TeamPerksType)typeInt;

        switch (type)
        {
            case TeamPerksType.ReviveAll:
                ReviveAllPlayers();
                break;

            case TeamPerksType.TeamStatBuff:
                AddTeamBuff(0.10f, 0.10f, 0.10f, 0.10f, 0.10f, 0.10f);
                break;

            case TeamPerksType.WallEnforce:
                EnforceWall();
                break;
        }

        foreach(var ps in FindObjectsOfType<PlayerStats>())
        {
            var view = ps.GetComponent<PhotonView>();
            if (view != null)
                view.RPC("RPC_ForceRecalculate", view.owner, null);
        }
        Debug.Log($"[TeamPerk] {type} 적용 완료");
    }
    void EnforceWall()
    {
        Debug.Log("[TeamPerk] 강화 시도");
        if (wall == null)
        {
            Debug.LogWarning("[TeamPerk] WallEnforce 실패 - WallManager 없음");
            return;
        }

        if (PhotonNetwork.isMasterClient)
        {
            Debug.Log("[TeamPerk] WallEnforce 실행");
            float percent=0.10f;
            wall.pv.RPC("RPC_IncreaseWallMaxHP",PhotonTargets.All, percent);
        }
        Debug.Log("[TeamPerk] WallEnforce 적용됨");
    }

    void ReviveAllPlayers()
    {
        
        StageManager stage=StageManager.Instance;
        if (stage == null)
        {
            Debug.LogError("[Perk-Revive] StageManager.Instance 가 null 입니다.");
            return;
        }

        var players = FindObjectsOfType<PlayerController>();
        Debug.Log($"[Perk-Revive] PlayerController 수 : {players.Length}");

        foreach (var pc in players)
        {
            if (pc == null)
            {
                Debug.LogWarning("[Perk-Revive] PlayerController 가 null 입니다.");
                continue;
            }

            var hp = pc.GetComponent<PlayerHealth>();
            if(hp==null)
            {
                Debug.LogWarning($"[Perk-Revive] {pc.name} 에 PlayerHealth 가 없습니다.");
                continue;
            }

            if(!hp.IsDead)continue;

            var pv = pc.GetComponent<PhotonView>();
            if(pv==null)
            {
                Debug.LogWarning($"[Perk-Revive] {pc.name} 에 PhotonView 가 없습니다.");
                continue;
            }

            if (pv.owner == null)
            {
                Debug.LogWarning($"[Perk-Revive] {pc.name} 의 pv.owner 가 null 입니다.");
                continue;
            }

            int actorId=pv.owner.ID;

            Transform spawn = stage.GetSpawnPoint(actorId);
            if (spawn == null)
            {
                Debug.LogWarning($"[Perk-Revive] SpawnPoint 를 찾을 수 없습니다. actorId={actorId}");
                continue;
            }

            Debug.Log($"[Perk-Revive] {pc.name} (actorId={actorId}) 를 {spawn.position} 에서 부활시킵니다.");
            pv.RPC("RPC_ReviveWithFullRestore", PhotonTargets.All, spawn.position, spawn.rotation);
            SFXManager.Instance.PlaySFX("Revive");
        }
        
        Debug.Log("[TeamPerk] ReviveAll 실행완료");
    }

    void AddTeamBuff(float atk, float def, float hp, float aSpd, float mSpd, float regen)
    {
        teamAtk += atk;
        teamDef += def;
        teamHP += hp;
        teamAS += aSpd;
        teamMS += mSpd;
        teamRegen += regen;
        RecalcAllPlayers();
    }

    void RecalcAllPlayers()
    {
        foreach (var ps in FindObjectsOfType<PlayerStats>())
            ps.Recalculate();
    }
    
    
    public void ApplyPersonalPerks(EssenceType type)
    {
        if (!PhotonNetwork.inRoom) return;
        if (!PhotonNetwork.player.IsLocal) return;

        var player = FindLocalPlayer();
        if (player == null)
        {
            Debug.LogWarning("[PersonalPerk] 로컬 플레이어 없음");
            return;
        }

        var stats = player.GetComponent<PlayerStats>();
        if (stats == null)
        {
            Debug.LogWarning("[PersonalPerk] PlayerStats 없음");
            return;
        }

        TryGiveEssenceTo(stats, type);
    }
    PlayerController FindLocalPlayer()
    {
        foreach (var pc in FindObjectsOfType<PlayerController>())
        {
            var view = pc.GetComponent<PhotonView>();
            if (view != null && view.isMine)
                return pc;
        }
        return null;
    }

    void TryGiveEssenceTo(PlayerStats stats,EssenceType type)
    {
        if (usedEssenceType.Contains(type)) return;
        if (stats.EssenceCount >= 4)
        {
            Debug.Log("[Perks] 슬롯 4칸 가득 참");
            return;
        }

        if (!essenceLookup.TryGetValue(type, out var item) || item == null)
        {
            Debug.LogWarning($"[Perks] 매핑 없음 : {type}");
            return;
        }

        bool ok = stats.TryAddEssence(item, allowDuplicate: true);
        if (ok)
        {
            usedEssenceType.Add(type);
            Debug.Log($"[PersonalPerk] {type} 장착 성공");
            var ui = FindObjectOfType<InventoryUI>(true);
            if (ui) ui.ForceUIRefresh();
        }
        else
        {
            Debug.Log($"[PersonalPerk] {type} 장착 실패");
        }
        
    }
    public List<EssenceType> GetRandomPersonalPerks()
    {
        List<EssenceType> available = new();
        foreach (EssenceType p in System.Enum.GetValues(typeof(EssenceType)))
        {
            if (!usedEssenceType.Contains(p))
                available.Add(p);
        }

        List<EssenceType> result = new();
        for (int i = 0; i < 3 && available.Count > 0; i++)
        {
            int rand = Random.Range(0, available.Count);
            result.Add(available[rand]);
            available.RemoveAt(rand);
        }

        return result;
    }
    
    public void ReleaseSystem()
    {
        // 팀/개인 특전 데이터 초기화
        Debug.Log("[PerksManager] 초기화 완료");
    }

    public void UpdateSystem()
    {
    }
}
