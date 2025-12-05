using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*HP, Die, Network EnemyManager에 보고*/
public class EnemyController : MonoBehaviour, IPunObservable
{
    [Header("HP")]
    public float maxHp = 300f;
    public float currentHp;
    public bool IsDead { get; private set; }

    public bool diedByBomb = false;
    EnemyCore core;
    Animator anim;
    PhotonView pv;

    Vector3 netPos;
    Quaternion netRot;
    float lerp = 10f;

    bool halfHpHitPlayed = false; //보스 반피 히트(포효) 여부 
    // 🔹 마지막으로 이 적을 공격한 플레이어의 ViewID (Kill 주기 위함)
    int lastAttackerViewId = 0;

#if PHOTON_UNITY_NETWORKING
    bool IsMaster() => !PhotonNetwork.connected || PhotonNetwork.isMasterClient;  //if(!IsMaster()) return;
#else
    bool IsMaster() => true;
#endif    

    void Awake()
    {
        core = GetComponent<EnemyCore>();
        anim = GetComponent<Animator>();
        pv = GetComponent<PhotonView>();

        currentHp = maxHp;

#if PHOTON_UNITY_NETWORKING
        if(pv && !pv.ObservedComponents.Contains(this))
            pv.ObservedComponents.Add(this);

        if(!PhotonNetwork.isMasterClient)
        {
            if(core && core.fsm) core.fsm.enabled = false;
            foreach (var hb in GetComponentsInChildren<EnemyHitbox>(true))
                hb.enabled = false;
        }
#endif

        netPos = transform.position;
        netRot = transform.rotation;
    }

    void Update()
    {
#if PHOTON_UNITY_NETWORKING
    if(!PhotonNetwork.isMasterClient)
    {
        float smooth = Mathf.Clamp01(Time.deltaTime * lerp); //도착 지연 보정 
        transform.position = Vector3.Lerp(transform.position, netPos, smooth);
        transform.rotation = Quaternion.Slerp(transform.rotation, netRot, smooth);
    }        
#endif
    }

    //======================================================
    //  ★ 애니메이션 동기화 (Idle/Move/Run/Attack/Hit/Die)
    //======================================================

    public void PlayAnim(string state)
    {
#if PHOTON_UNITY_NETWORKING
        if (!PhotonNetwork.isMasterClient) return;
        pv.RPC(nameof(RPC_SetAnim), PhotonTargets.All, state);
#else
        RPC_SetAnim(state);
#endif
    }

    [PunRPC]
    void RPC_SetAnim(string state)
    {
        if (!anim) return;

        switch (state)
        {
            case "Idle":
                anim.SetBool("Move", false);
                anim.SetFloat("Speed", 0f);
                anim.CrossFadeInFixedTime("Idle", 0.1f);
                break;

            case "Move":
                anim.SetBool("Move", true);
                anim.CrossFadeInFixedTime("Move", 0.1f);
                break;

            case "Run":
                anim.SetBool("Move", true);
                anim.CrossFadeInFixedTime("Run", 0.12f);
                break;

            case "Attack":
                anim.ResetTrigger("Hit");
                anim.SetTrigger("Attack");
                break;

            case "Hit":
                anim.ResetTrigger("Attack");
                anim.SetTrigger("Hit");
                break;

            case "Die":
                anim.SetBool("Die", true);
                anim.SetTrigger("Death");
                break;
        }
    }
    
    //================Damage API=================
    public void TakeDamage(float damage, int attackerViewId = 0)
    {
        if (IsDead) return;

#if PHOTON_UNITY_NETWORKING
        if(!IsMaster())
        {
            //[로컬] 공격 성공 판단 -> 요청 보내기 (공격  ID, 타깃 ID)
            pv.RPC(nameof(RPC_RequestApplyDamage_ByAttackId), PhotonTargets.MasterClient, damage, attackerViewId);
            return;
        }        
#endif

        //  여기서 마스터/오프라인에서 마지막 공격자 ViewID 저장 + 로그 찍기
        if (attackerViewId != 0)
        {
            lastAttackerViewId = attackerViewId;
            Debug.Log($"[DMG] TakeDamage()에서 lastAttackerViewId SET = {lastAttackerViewId}");
        }
        else
        {
            Debug.LogWarning("[DMG] TakeDamage() attackerViewId=0 → 공격자 정보 없음!!");
        }

        ApplyDamage_Authoritative(Mathf.Max(0f, damage));
    }

#if PHOTON_UNITY_NETWORKING
    [PunRPC] //마스터에서 최종 계산 
    void RPC_RequestApplyDamage_ByAttackId(float damage, int attackerViewId, PhotonMessageInfo info)
    {
        if(!PhotonNetwork.isMasterClient) return; 

        float finalDmg = Mathf.Max(0f, damage);

        ApplyDamage_Authoritative(finalDmg);
    }    
#endif

    public void ApplyDamage_Authoritative(float dmg)
    {
        if (IsDead) return;

        //1) 이번 데미지에서 hit/포효를 재생할 지 결정 
        bool  playHit = ShouldPlayHitForThisDamage(dmg);

        //2) 실제 처리 로직 
        ApplyDamage_Authoritative(dmg, playHit);

    }

//추가
    public void ApplyDamage_Authoritative(float dmg, bool playHitReaction)
    {
        if (IsDead) return;

        currentHp -= Mathf.Max(0f, dmg);
        currentHp = Mathf.Max(0f, currentHp);

        if(playHitReaction)
        {
            if (currentHp <= 0f)
            {
                Kill_Authoritative();
                return;
            }
            PlayAnim("Hit");
            core.isAttacking = false;
            core.EndAttackLock();
            core.PauseAgent();
            core.hitLockUntil = Time.time + 0.25f;
            core.anim.ResetTrigger("Attack");
            core.anim.SetTrigger("Hit");
            
        }
        else
        {
            //Hp만 감소, 애니/락은 건드리지 않음 
            if (currentHp <= 0f)
            {
                Kill_Authoritative();
            }
        }
    }

    //추가
    public void TakeReflectDamage(float damage)
    {
#if PHOTON_UNITY_NETWORKING
       if(!IsMaster()) return; 
#endif 
       ApplyDamage_Authoritative(damage, false); //리액션 없이 데미지만    
       
    }

    int GetActorNumberFromView(int viewId)
    {
#if PHOTON_UNITY_NETWORKING
        var v = PhotonView.Find(viewId);
        return v != null ? v.ownerId : 0;
#else
        return 0;
#endif        
    }
    

    //추가 hit 재생 여부 결정 헬퍼
    bool ShouldPlayHitForThisDamage(float dmg)
    {
        //core가 없거나, 보스가 아니면 => 항상 hit 재생(minion)
        if(core == null || !core.IsBoss)
        return true;

        //===보스 전용 로직===
        float prevHp = currentHp;
        float newHp = Mathf.Max(0f, prevHp - Mathf.Max(0f, dmg));

        float half = maxHp * 0.5f; 

        //아직 반피 히트 안씀 
        //이번 데미지로 hp가 처음으로 반피 이하로 떨어지는 순간
        if(!halfHpHitPlayed && prevHp > half && newHp <= half)
        {
            halfHpHitPlayed = true;
            return true; //이때만 hit 재생 
        }

        //그 외에는 보스는 hit 모션 안씀 
        return false; 
    }


    void Kill_Authoritative()
    {
        if (IsDead) return;
        Debug.Log($"[KILL] Kill_Authoritative() 시작 - lastAttackerViewId={lastAttackerViewId}");
        IsDead = true;

        // 🔹 저장해둔 마지막 공격자에게 Kill 1회 반영 (마스터 or 오프라인)
        if (lastAttackerViewId != 0)
        {
            // Photon 연결된 상태면 마스터만 처리, 오프라인이면 그냥 처리
            if (!PhotonNetwork.connected || PhotonNetwork.isMasterClient)
            {
                Debug.Log($"[KILL] ReportKillToPlayer({lastAttackerViewId}) 호출");
                ReportKillToPlayer(lastAttackerViewId);
            }
            else
            {
                Debug.Log("[KILL] 마스터가 아니라서 킬 처리는 스킵");
            }
        }
        else
        {
            Debug.LogWarning("[KILL] lastAttackerViewId == 0, 킬 안 줌");
        }


        // 모든 히트박스 즉시 종료
        var relay = GetComponent<EnemyAttackEventRelay>();
        if (relay) relay.CloseAllHitboxes();
        foreach (var hb in GetComponentsInChildren<EnemyHitbox>(true))
        {
            hb.ForceClose();
            hb.gameObject.SetActive(false);
        }

        if (diedByBomb)
        {   
            anim.enabled = false;
            var rag = GetComponent<EnemyRagdollController>();
            if (rag != null)
            {
                rag.SetRagdoll(true);
                rag.AddExplosionForce(50f, transform.position, 5f);
            }

            // EnemyManager 제거 처리 유지
            if (IsMaster())
            {
                EnemyManager em = FindObjectOfType<EnemyManager>();
                if (em) em.RemoveEnemy(gameObject);
            }

            // 몇 초 뒤 삭제
            KillWithDelay(5f);
            return;
        }
        //뒤늦게 들어온 클라도 사망상태 유지 
        RPC_PlayDie_AllBuffered();

        //FSM 
        if (core)
        {
            core.OnDeath();
        }

        if (IsMaster())
        {
            //EnemyManager 보고
            EnemyManager em = FindObjectOfType<EnemyManager>();
            if (em) em.RemoveEnemy(gameObject);
        }
        var hud=FindObjectOfType<HUDController>(true);
        //var core=GetComponent<EnemyCore>();
        if(hud&&core&&core.IsBoss)
        hud.ShowBossHP(false);
        //파괴
        if(PhotonNetwork.isMasterClient)
        {
            KillWithDelay(5f);
        }     

    }
    public void KillWithDelay(float delay)
    {
        if (PhotonNetwork.isMasterClient)
            StartCoroutine(DestroyAfter(delay));
    }
    IEnumerator DestroyAfter(float t)
    {
        yield return new WaitForSeconds(t);
        PhotonNetwork.Destroy(gameObject);
    }
    // 🔹 마지막 공격자에게 Kill 전달
    void ReportKillToPlayer(int attackerViewId)
    {
        if (!PhotonNetwork.isMasterClient)
        {
            Debug.LogWarning("[KILL] ReportKillToPlayer가 마스터가 아닌 클라에서 호출됨");
            return;
        }

        var attackerPv = PhotonView.Find(attackerViewId);
        if (attackerPv == null)
        {
            Debug.LogWarning($"[KILL] attackerViewId={attackerViewId} PhotonView 찾기 실패");
            return;
        }

        var killer = attackerPv.owner;   // PhotonPlayer

        Debug.Log($"[KILL] Enemy killed by actorID={killer.ID}, Nick={killer.NickName}");

        if (PlayerStatsTracker.Instance != null)
        {
            PlayerStatsTracker.Instance.AddKill(killer);
        }
        else
        {
            Debug.LogWarning("[KILL] PlayerStatsTracker.Instance가 없음");
        }
    }
    //==============Animation RPCs==============
    void RPC_PlayHit_All()
    {
#if PHOTON_UNITY_NETWORKING
        if(PhotonNetwork.connected)
        {
            pv.RPC(nameof(RPC_PlayHit), PhotonTargets.All);
            return;
        }
#endif
        RPC_PlayHit(); // 오프라인 
    }

    void RPC_PlayDie_AllBuffered()
    {
#if PHOTON_UNITY_NETWORKING
        if(PhotonNetwork.connected)
        {
            pv.RPC(nameof(RPC_PlayDie), PhotonTargets.AllBuffered); 
            return;
        }      
#endif
        RPC_PlayDie(); //오프라인
    }

    [PunRPC]
    void RPC_PlayHit()
    {
        if (!IsDead && anim)
            anim.SetTrigger("Hit");
    }

    [PunRPC]
    void RPC_PlayDie()
    {
        IsDead = true; // 모든 클라이언트의 동일 프레임에서 IsDead 동시에 true
        if (anim)
        {
            anim.ResetTrigger("Hit");
            anim.SetBool("Die", true);
        }
    }



    //========================경량 동기화=======================
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
#if PHOTON_UNITY_NETWORKING
        if(!PhotonNetwork.connected) return;

        if(stream.isWriting) // owner가 송신 
        {
            stream.SendNext(transform.position); // 위치
            stream.SendNext(transform.rotation); //회전
            stream.SendNext(currentHp); //HP
            stream.SendNext(IsDead); // Die
        }  
        else
        {
            netPos = (Vector3)stream.ReceiveNext();
            netRot = (Quaternion)stream.ReceiveNext();
            currentHp = (float)stream.ReceiveNext(); 
            IsDead = (bool)stream.ReceiveNext();
        }      
#endif        
    }
    

    public void SFX_EnemyHit()
    {
        SFXManager.Instance.PlaySFX("Hit2");
    }

    public void SFX_BossHit()
    {
        SFXManager.Instance.PlaySFX("AttackSkill");
    }

    public void SFX_MinionDie()
    {
        SFXManager.Instance.PlaySFX("MinionDie");
    }
    public void SFX_BossDie()
    {
        SFXManager.Instance.PlaySFX("BossDie");
    }
    public void SFX_YmirDie()
    {
        SFXManager.Instance.PlaySFX("AcYmirDie");
    }
    public void SFX_ColossusWalk()
    {
        SFXManager.Instance.PlaySFX("ColossusWark");
    }
    public void SFX_YmirHit()
    {
        SFXManager.Instance.PlaySFX("AcYmirHit");
    }
}
