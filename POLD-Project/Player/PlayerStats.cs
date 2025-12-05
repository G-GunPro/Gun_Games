using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using System.Collections;

public class PlayerStats : MonoBehaviour
{
    [Space(10f)]
    [Header("CSV의 Titan Name")]
    public string selectedTitan = "Female Titan";
    
    [Header("Base Stats")]
    public int baseMaxHP = 1000;
    public float baseHPRegen = 5f;
    public float baseAttack = 100f;
    public float baseDefense = 10f;
    public float baseAttackSpd = 1f;
    public float baseMoveSpd = 8f;

    [Header("Essence Slots (정수 4칸)")]
    // 개인특전에서 앞칸부터 채워지는 정수 슬롯
    public ItemData[] essence = new ItemData[4];

    public static PlayerStats Local;                    // 로컬 내 것
    public static event System.Action<PlayerStats> OnLocalReady; // 로컬 준비 이벤트

    // 현재 조종 중인 타이탄 (프로필/초상화 표시용)
    public TitanData currentTitan;

    // 최종 계산 결과
    public int MaxHP { get; private set; }
    public float HPRegen { get; private set; }
    public float Attack { get; private set; }
    public float Defense { get; private set; }
    public float AttackSpeed { get; private set; }
    public float MoveSpeed { get; private set; }
    public float AggroChaseSec { get; private set; } = 4f;

    public Action OnStatsChanged;

    private PhotonView pv;

    void OnEnable()
    {
        // Photon 오프라인이거나, PhotonView가 있고 isMine이면 → 내 것
        bool isMine = PhotonNetwork.offlineMode || (pv && pv.isMine);
        if (isMine)
        {
            Local = this;
            OnLocalReady?.Invoke(this);  // UI들이 이 신호를 듣고 자동 바인딩
        }

    }
    void Awake()
    {
        pv = GetComponent<PhotonView>();
    }
    IEnumerator Start()
    {
        while (TitanStatsDB.Instance == null)
            yield return null;

        while (TitanStatsDB.Instance != null && TitanStatsDB.Instance.TableCount == 0)
            yield return null;

        Recalculate();
    }

    public void Recalculate()
    {
        Debug.Log($"[PlayerStats] selectedTitan='{selectedTitan}' / DBAlive={TitanStatsDB.Instance != null}");
        
        float hpMul = 1f, regenMul = 1f, atkMul = 1f, defMul = 1f, atkSpdMul = 1f, moveSpdMul = 1f;

        if (TitanStatsDB.Instance != null &&
        TitanStatsDB.Instance.TryGet(selectedTitan, out var row))
        {
            hpMul = row.maxHpMul;
            regenMul = row.hpRegenMul;
            atkMul = row.attackMul;
            defMul = row.defenseMul;
            atkSpdMul = row.attackSpdMul;
            moveSpdMul = row.moveSpdMul;
            AggroChaseSec = Mathf.Max(0f, row.aggroChaseSec); //음수 방지 

            // CSV의 값으로 Base Stats 갱신
            baseMaxHP = Mathf.RoundToInt(1000 * row.maxHpMul);
            baseHPRegen = 5f * row.hpRegenMul;
            baseAttack = 100f * row.attackMul;
            baseDefense = 1f * row.defenseMul;
            baseAttackSpd = 1f * row.attackSpdMul;
            baseMoveSpd = 8f * row.moveSpdMul;

        }
        else
        {
            Debug.LogWarning($"[PlayerStatsRuntime] CSV 데이터 '{selectedTitan}'을 찾을 수 없음.");
            AggroChaseSec = 4f;
        }

        float pAtk = 0, pDef = 0, pHP = 0, pAS = 0, pMS = 0, pRegen = 0;

        if (essence != null)
        {
            foreach (var e in essence)
            {
                if (!e) continue;
                pAtk += NormalizeStats(e.addAttack);
                pDef += NormalizeStats(e.addDefense);
                pHP += NormalizeStats(e.addHP);
                pAS += NormalizeStats(e.addAttackSpeed);
                pMS += NormalizeStats(e.addMoveSpeed);
                pRegen += NormalizeStats(e.addRegen);
            }
        }

        var pm = (GameManager.Instance ? GameManager.Instance.perksManager : null);
        if (pm != null)
        {
            pAtk += pm.teamAtk;
            pDef += pm.teamDef;
            pHP += pm.teamHP;
            pAS += pm.teamAS;
            pMS += pm.teamMS;
            pRegen += pm.teamRegen;
        }

        MaxHP = Mathf.RoundToInt(baseMaxHP * hpMul * (1f + pHP));
        HPRegen = baseHPRegen * regenMul * (1f + pRegen);
        Attack = baseAttack * atkMul * (1f + pAtk);

        float defFactorFromCsv = defMul; 
        float defFactorFromBonus = 1f + pDef; 
        float defFactor = defFactorFromCsv * defFactorFromBonus;

        defFactor = Mathf.Clamp(defFactor, 0.8f, 1.8f);

        Defense = defFactor; 
        Debug.Log($"[PlayerStats] Defense = {Defense} (mul={defMul}, bonus = {pDef})" );
        
        AttackSpeed = baseAttackSpd * atkSpdMul * (1f + pAS);
        MoveSpeed = baseMoveSpd * moveSpdMul * (1f + pMS);

        OnStatsChanged?.Invoke();

        Debug.Log($"[STATS] 최종 무브스피드 계산값 = {MoveSpeed}, base={baseMoveSpd}, mul={moveSpdMul}");

    }

    float NormalizeStats(float v) => (v > 1f) ? v * 0.01f : v;



    // --------------------------------------------------------------
    // [정수 관련 기능]
    // --------------------------------------------------------------

    // 현재 장착된 정수 개수
    public int EssenceCount
    {
        get
        {
            int c = 0;
            for (int i = 0; i < essence.Length; i++)
                if (essence[i] != null) c++;
            return c;
        }
    }

    /// <summary>
    /// 개인특전에서 선택된 정수를 앞칸부터 장착 (최대 4개)
    /// </summary>
    public bool TryAddEssence(ItemData data, bool allowDuplicate = true)
    {
        if (!pv.isMine) return false;

        if (data == null || data.kind != ItemData.ItemKind.Essence)
            return false;

        if (!allowDuplicate)
        {
            for (int i = 0; i < essence.Length; i++)
                if (essence[i] == data)
                    return false; // 중복 방지
        }

        for (int i = 0; i < essence.Length; i++)
        {
            if (essence[i] == null)
            {
                essence[i] = data;
                Recalculate();                 // 스탯 재계산
                OnStatsChanged?.Invoke();      // UI 갱신
                return true;
            }
        }

        return false; // 4칸 다 찼음
    }

    /// <summary>
    /// 새 라운드나 게임 시작 시 정수 초기화용
    /// </summary>
    public void ClearEssences()
    {
        if (!pv.isMine) return;

        for (int i = 0; i < essence.Length; i++)
            essence[i] = null;

        Recalculate();
        OnStatsChanged?.Invoke();
    }

    // --------------------------------------------------------------
    // [소모 아이템 사용]
    // --------------------------------------------------------------
    public void ApplyUse(ItemData item)
    {
        if (!pv.isMine) return;

        if (!item) return;
        MaxHP += Mathf.RoundToInt(item.healHPOnUse); // 예: 회복 포션
        OnStatsChanged?.Invoke();
    }

    [PunRPC]
    public void RPC_ForceRecalculate()
    {
        Recalculate();
    }
}
