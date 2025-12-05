using ExitGames.Client.Photon;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using UnityEngine;
using System;
using System.Collections;

//🌟 11.04 추가 
public class PlayerHealth : MonoBehaviour
{

    public event Action<PlayerHealth> OnDied;
    //죽음 
    private bool died = false;
    private PhotonView pv;

    HUDController hud;
    

    [Header("엑셀 DB 연결")]
    public string playerTitanName = "Female Titan"; // 현재 캐릭터 이름 (엑셀 이름과 동일하게)
    public float baseHP = 1000f; //기준 체력 
    public float attackPower;
    public float baseAttack = 100f;
    public float defensePower;
    public float baseDefense = 1f;


    [Header("현재 상태")]
    public float maxHP;
    public float currentHP;

    [HideInInspector]
    public Animator anim;
    public bool IsDead => currentHP <= 0f;

    bool canPlayHitAnim = true;
    float hitAnimCoolDown = 0.3f;

    // private bool canBeHit = true;
    // private bool isHitTriggered = false;
    // private float hitCooldown = 3f;
    void Awake()
    {
        pv = GetComponent<PhotonView>();
        hud = FindObjectOfType<HUDController>();

    }
    public void Heal(int amount)
    {
        currentHP += amount;
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);
        Debug.Log($"체력 회복 아이템 사용!. 현재 HP: {currentHP}");
        UpdateHUD();
        UploadHpToPhoton();
    }

    public void UpdateHUD()
    {
        if (!pv.isMine)
        if (hud == null) hud = FindObjectOfType<HUDController>();
        if (hud == null) return;

        hud.SetPlayerHp(currentHP, maxHP);
    }
    void UploadHpToPhoton()
    {
        if (!pv.isMine) return;
        Hashtable ht = new Hashtable();
        ht["HP"] = currentHP;
        ht["HPMax"] = maxHP;
        PhotonNetwork.player.SetCustomProperties(ht);
    }

    void Start()
    {
        anim = GetComponent<Animator>();

        pv = GetComponent<PhotonView>();

        // PlayerStats와 연결
        var stats = GetComponent<PlayerStats>();
        if (stats != null)
        {
            maxHP = stats.MaxHP;
            currentHP = maxHP;
        }
        // if (TitanStatsDB.Instance != null)
        // {
        //     //===========DB에서 체력 불러오기
        //     if (TitanStatsDB.Instance.TryGet(playerTitanName, out var row))
        //     {
        //         maxHP = baseHP * row.maxHpMul;
        //         Debug.Log($"[PlayerHealth] '{playerTitanName}' HP 배율 {row.maxHpMul} → MaxHP = {maxHP}");

        //         attackPower = baseAttack * row.attackMul;
        //         defensePower = baseDefense * row.defenseMul;
        //     }
        //     else
        //     {
        //         Debug.LogWarning($"[PlayerHealth] '{playerTitanName}' 데이터를 DB에서 찾을 수 없음. 기본값 사용.");
        //         maxHP = baseHP;
        //     }
            
        // }
        // else
        // {
        //     Debug.LogWarning("[PlayerHealth] TitanStatsDB Instance가 존재하지 않습니다.");
        //     maxHP = baseHP;
        // }

        // currentHP = maxHP;

        UpdateHUD();
        UploadHpToPhoton();
    }

    public void TakeDamage(float damage)
    {
        if (IsDead) return;

        var stats = GetComponent<PlayerStats>();
        if (stats != null)
        {
            // 방어 값 퍼센트로 해설 (normal = 0%, 10 = 10% )
            float defFactor = Mathf.Clamp(stats.Defense, 0.8f, 1.8f);

            // defFactor가 클수록 데미지가 줄어들도록 역수 
            float dmgMul = 1f/defFactor;

            //딜 감소 최대 80% 
            dmgMul = Mathf.Clamp(dmgMul, 1f / 1.8f, 1f / 0.8f);
            damage *= dmgMul;
            
            Debug.Log($"[PlayerHealth] raw={damage / dmgMul}, defFactor={defFactor}, mul={dmgMul}, final={damage}");
        }

        currentHP -= damage;

        if (currentHP <= 0f)
        {
            currentHP = 0f;
            OnDeath();
        }
        else
        {
            Debug.Log($"{playerTitanName} 피격! 남은 HP: {currentHP} / {maxHP}");
        }

        UpdateHUD();
        UploadHpToPhoton();

        //데미지 깍고 모션만 조절 
        if(canPlayHitAnim)
        {
            canPlayHitAnim = false;
            StartCoroutine(HitAnimCooldownRoutine());

            var controller = GetComponent<PlayerController>();
            if (controller != null)
            {
                controller.SendHitRPC(); //네트워크 재생 지시
            }
        }
    }

    IEnumerator HitAnimCooldownRoutine()
    {
        yield return new WaitForSeconds(hitAnimCoolDown);
        canPlayHitAnim = true;
    }

    void OnDeath()
    {
        if (died) return;
        died = true;

        if (anim != null)
        {
            anim.SetBool("Death", true);
            anim.CrossFadeInFixedTime("Death", 0.1f);
        }

        Debug.Log($"{playerTitanName} 사망!");
        //  1) 데스 카운트 증가 (내가 조종중인 플레이어일 때만)
        if (PlayerStatsTracker.Instance != null)
        {
            var view = GetComponent<PhotonView>();

            bool isMine = true; // 오프라인/뷰 없을 때는 그냥 내 것 취급

            if (PhotonNetwork.connected && PhotonNetwork.inRoom && view != null)
            {
                isMine = view.isMine;
            }

            if (isMine)
            {
                PlayerStatsTracker.Instance.AddDeath();
            }
        }
#if PHOTON_UNITY_NETWORKING
        if(!PhotonNetwork.connected || PhotonNetwork.isMasterClient)
#endif        
        //로컬에서 이벤트 발생 
        OnDied?.Invoke(this);

#if PHOTON_UNITY_NETWORKING
        //2) 마스터가 전체에 사망 사실만 브로드캐스트
        if (PhotonNetwork.connected && PhotonNetwork.inRoom && PhotonNetwork.isMasterClient)
        {
            if (!pv) pv = GetComponent<PhotonView>();
            if(pv) pv.RPC(nameof(RPC_PlayerDied), PhotonTargets.All, pv.viewID);
        }
#endif
        if (pv.isMine)
        {
            SpectatorManager.BeginSpectate();
        }
        CheckTeamDeath();
    }

    void CheckTeamDeath()
    {
        int alive = 0;
        foreach (var p in FindObjectsOfType<PlayerHealth>())
            if (!p.IsDead) alive++;

            if(alive <= 0)
        {
            Debug.Log("[GaemOver] 플레이어 전멸");
            GameManager.Instance.OnGameOver();
        }
    }



#if PHOTON_UNITY_NETWORKING
[PunRPC]
    void RPC_PlayerDied(int viewId)
    {
        //시각, 애니메이션
        if(anim != null)
        {
            anim.SetBool("Death", true);
            anim.CrossFadeInFixedTime("Death", 0.1f);
        }
    } 
#endif       




    public void TakeDamageByAttackId(int attackId, int attackerViewId = 0)
    {
#if PHOTON_UNITY_NETWORKING
//클라면 마스터에게
        if (PhotonNetwork.connected && PhotonNetwork.inRoom && !PhotonNetwork.isMasterClient)
        {
            var pv = GetComponent<PhotonView>();
            if(pv) pv.RPC(nameof(RPC_RequestApplyDamage_ByAttackId), 
                                 PhotonTargets.MasterClient, attackId,attackerViewId);
            return;
        }      
#endif
        //오프라인 /로컬 싱글 / 마스테어서 즉시 적요
        ApplyAttack_ById_Server(attackId, attackerViewId);
    }




#if PHOTON_UNITY_NETWORKING
    [PunRPC]
    void RPC_RequestApplyDamage_ByAttackId(int attackId, int attackerViewId, PhotonMessageInfo info)
    {
        if(!PhotonNetwork.isMasterClient) return;

        //공격자 검증, viewId 소유자와 요청자 일치
        var attackerPv = PhotonView.Find(attackerViewId);
        if(attackerPv != null && attackerPv.ownerId != info.sender.ID) return;

        ApplyAttack_ById_Server(attackId, attackerViewId);
    }   
#endif


    //마스터 전용 실제 계산, 적용
    void ApplyAttack_ById_Server(int attackId, int attackerViewId)
    {
        if (IsDead) return;

        //임시 
        float dmg;

#if PHOTON_UNITY_NETWORKING
        //1) 공격자 PhotonView 찾기
        EnemyCore attackerCore = null; 

        if(PhotonNetwork.connected && PhotonNetwork.inRoom && attackerViewId != 0)
        {
            var attackerPv = PhotonView.Find(attackerViewId);
                if(attackerPv != null)
                {
                    attackerCore = attackerPv.GetComponent<EnemyCore>()
                                    ?? attackerPv.GetComponentInParent<EnemyCore>();     
                }
        }

        //2) EnemyCore가 있으면 CalcDamager 사용 
        if(attackerCore != null)
        {
            dmg = attackerCore.CalcDamage(attackId);
        }
        else
#endif
        {
            dmg = GetFallbackDamage(attackId);
        }

        Debug.Log($"[PlayerHealth] ApplyAttack_ById_Server id={attackId}, from view={attackerViewId}, dmg={dmg}");
        TakeDamage(dmg);

    }
    

    float GetFallbackDamage(int attackId)
    {
        switch (attackId)
        {
            case 1001:
                return 50f;
            case 2001:
                return 80f;
            default:
                return 50f;        
        }
    }


    public void ResetForRespawn()
    {
        died = false; // 사망 해제 
        currentHP = maxHP;
        // canBeHit = true;
        if (anim != null)
        {
            anim.ResetTrigger("Hit");
            anim.SetBool("Death", false); // 죽음 해제 
            anim.Play("Idle"); // 기본 아이들 복귀 
        }

        Debug.Log($"{playerTitanName} 리스폰 완료. HP: {currentHP} /{maxHP}");
    }
    
#if PHOTON_UNITY_NETWORKING
    [PunRPC]
    public void RPC_ResetForRespawn(Vector3 pos, Quaternion rot)
    {
        //위치 각도 동기화
        transform.SetPositionAndRotation(pos, rot);

        //상태 초기화
        ResetForRespawn();
    }    
#endif

    public float GetCurrentHealth()
    {
        return currentHP;
    }

    [PunRPC]
    public void RPC_ReviveWithFullRestore(Vector3 pos,Quaternion rot)
    {
        transform.SetPositionAndRotation(pos,rot);

        died = false; 
        currentHP=maxHP;
        
        var stats=GetComponent<PlayerStats>();
        if(stats != null)
        stats.RPC_ForceRecalculate();

        var motor=GetComponent<PlayerMotor>();
        if(motor!=null)
        motor.enabled=true;

        var controller=GetComponent<PlayerController>();
        if(controller !=null)
        controller.enabled=true;

        if(anim!=null)
        {
            anim.ResetTrigger("Hit");
            anim.SetBool("Death", false);
            anim.Rebind();
            anim.Update(0f);
        }

        if (pv.isMine)
        {
            var cam=FindObjectOfType<CameraFollow>();
            if(cam !=null)
            cam. AttachTarget(transform);

        }

    }
}
