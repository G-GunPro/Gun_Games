using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/*
스턴 여부 
플레이어 / 벽 타겟
NavMeshAgent 이용해서 이동
타겟 결정 로직 (벽 vs 플레이어)

*/
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class EnemyCore : MonoBehaviour
{
    private EnemyController life; // 피관리는 EnemyController 에서.. 

    PhotonView pv;
    float nextAttackAt;


//=============엑셀=======================
    public float DamageToWallMul { get; set; }
    public float TrapClearance { get; set; }
    public TargetPriority TargetPriority { get; set; }
    public float DamageMitigation { get; set; }
    public bool StatsApplied { get; set; } // 스탯 적용 완료 여부 
    public bool IsBoss{ get; private set; }
    public EnemyBaseStats baseStats; // 프리팹에 연결
    EnemyIdentity identity;
    //=========================================    

    //====================스킬 쿨타임===================

    [Header("스킬 쿨타임(초)")]
    [SerializeField]
    float skillCooldown = 12f;
    float nextSkillTime; //시간 체크해서 스킬 사용 가능 여부 확인 

    //======================================================



    //=========== 벽 보는 5라운드 초대형 거인용 ================
    public bool IsWallOnly => TargetPriority == TargetPriority.WallOnly;



    //몬스터가 벽을 공격할 포인트들
    [HideInInspector]
    public WallAttackPoints wallPoints;

    //KillCount를 위한 막타 플레이어
    private PlayerController lastAttacker;


    [Header("Refs")]
    public Animator anim;
    public NavMeshAgent agent;
    public Transform wallTarget;     //성벽 포인트 
    public Transform playerTarget;   //플레이어
    public Transform colossalTarget; // 초대형 거인 
    public Transform currentTarget; //지금 실제로 쫓는 목표
    public EnemyStateMachine fsm;  //테스트 후 HideInInspector

    Collider col;
    Rigidbody rb;

    [Space (10f)]
    [Header("어그로 타임 설정")]
    public float playerAggroUntil = 0f;
    [HideInInspector]
    public bool IsPlayerAggroActive => Time.time < playerAggroUntil;


    [Space(10f)]
    [Header("스탯")]
    public int EMaxDefense;
    public int ECurrentDefense;
    public float AttackPower = 50f; 

    [Space(10f)]
    [Header("거리 / 속도")]
    public float EMoveSpeed = 3.5f;
    public float aggroRange = 13f; // 플레이어 인식 거리
    public float ColossalRange = 23f;  //초대형 거인 인식 거리 
    public float stunDuration = 2.0f; //스턴 유지시간 
    public float maxChaseTime = 4.0f; // 추격 시간

    [Space(10f)]
    [Header("거리 임계값")]
    public float attackEnterRange = 2.3f; // 공격 진입 
    public float attackExitRange = 2.8f; // 공격 이탈 
    public float aggroEnterRange = 13f; //플레이어 인식 진입
    public float aggroExitRange = 20f; //플레이어 인식 이탈


    public bool IsInAttackEnter(float dist) => dist <= attackEnterRange;
    public bool IsInAttackExit(float dist) => dist >= attackExitRange;
    public bool IsAggroEnter(float dist) => dist <= aggroEnterRange;
    public bool IsAggroExit(float dist) => dist >= aggroExitRange;

 
    [Space(10f)]
    [Header("상태 플래그")]
    [SerializeField]
    private bool isStunned = false;
    [SerializeField]
    public bool IsStunned => isStunned;
    private float stunTimer = 0f;

    //🌟11.03 추가
    [HideInInspector]
    public bool isAttacking = false;

    [HideInInspector]
    public float hitLockUntil = 0f;
    [HideInInspector]
    public bool IsHitLocked => Time.time < hitLockUntil;

    Vector3 _prevPos;

    // [HideInInspector]
    public bool lockPosActive;
    public Vector3 lockPos;

    bool _pendingReturnToWall;

    //====================
    //몬스터 행동 가능 여부.
    public bool CanAct
    {
        get
        {
            if (life != null && life.IsDead) return false;
            if (IsStunned) return false;
            return true;
        }   
    }
    //====================

    [Header("행동 성향/ 4페이즈 보스 스턴")]
    public bool canBeStunned = false;  //잡몸 = false, 4웨이브 보스 = true

    [Header("공격 애니들")]
    public int AttackCount = 3;
    [Header("플레이어에게서 벽 공격 뺏기는 거리")]
    public float playerStealRange = 4f;

    //이동 제어 
    float smoothedSpeed;
    bool moveFlag;
    const float MOVE_ON = 0.15f;
    const float MOVE_OFF = 0.08f;


    bool attackExitPending;
    public bool AgentReady => agent && agent.enabled && agent.isOnNavMesh;
    private PlayerHealth targetHealth; //현재 추적 중인 플레이어의 체력 스크립트  

    [Header("Trap 관련 설정")]
    public float detectTrapRange = 10f;
    public Transform trapTarget;


    //=====================================================================

    //래퍼런스 연결
    void Awake()
    {

#if PHOTON_UNITY_NETWORKING
        pv = GetComponent<PhotonView>();
        if(!pv) Debug.LogWarning("[EnemyCore] 에너미 코어 포톤뷰가 없어", this);

#endif


        if (anim == null)
        {
            anim = GetComponent<Animator>();
        }
        if (anim) anim.applyRootMotion = false;

        if (agent == null)
        {
            agent = GetComponent<NavMeshAgent>();
        }

        if (agent)
        {
            agent.updatePosition = true;
            agent.updateRotation = true;
            agent.stoppingDistance = attackEnterRange * 0.9f;
            agent.angularSpeed = Mathf.Max(agent.angularSpeed, 360);
            agent.acceleration = Mathf.Max(agent.acceleration, 12f);
            agent.avoidancePriority = Random.Range(20, 80);
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.GoodQualityObstacleAvoidance;
        }

        life = GetComponent<EnemyController>();
        col = GetComponent<Collider>();
        rb = GetComponent<Rigidbody>();
        if (rb) rb.isKinematic = true; // 물리 방지


        identity = GetComponent<EnemyIdentity>(); 
        if (!EnemyStatsDB.Instance)
        {
            Debug.LogError("[EnemyCore] EnemyStatsDB 찾지 못함");
            return;
        }

        if (!identity)
        {
            Debug.LogWarning("[EnemyCore] EnemyIdentity 누락");
            return;
        }

        EnemyStatsRuntime stats;
        if (EnemyStatsDB.Instance.TryGet(identity.id, identity.role, out stats))
        {
            EnemyStatsDB.Instance.ApplyStats(this, stats);
            Debug.Log($"[EnemyCore] {gameObject.name} 스탯 적용 완료: {identity.id} | {identity.role}");
        }
        else
        {
            Debug.LogWarning($"[EnemyCore] {gameObject.name} t스탯못찾음!!!!: {identity.id} | {identity.role}");
        }

        IsBoss = identity.role == "Boss";
        Debug.Log($"[EnemyCore] {identity.id} role = {identity.role}, IsBoss = {IsBoss}");

        Debug.Log($"[EnemyCore] {identity.id} 최종 스탯 HP={life.maxHp}, ATK={AttackPower}, SPD={EMoveSpeed}");
        //🌟

        if (wallPoints == null)
        {
            wallPoints = FindObjectOfType<WallAttackPoints>(true);
        }



        _prevPos = transform.position;
    }



    // Start is called before the first frame update
    void Start()
    {
        ECurrentDefense = EMaxDefense;

        //스폰 직후 NavMesh 안착 보장 
        EnsureAgentOnNavMesh(2f);

        if (agent != null)
        {
            agent.speed = EMoveSpeed;
            agent.isStopped = false;
        }

        StartCoroutine(DelayedInit());
        
        Debug.Log($"[Animator RC] {anim?.runtimeAnimatorController?.name}");
    }



    // Update is called once per frame
    void Update()
    {
        if (life && life.IsDead) return;
#if PHOTON_UNITY_NETWORKING
        bool isRemote = PhotonNetwork.connected && !PhotonNetwork.isMasterClient;
#else
        bool isRemote = false;
#endif

        UpdateAnimSpeed(isRemote);
        Debug.Log($"[EnemyCore] ranges atkEnter = {attackEnterRange}, trapDetect={detectTrapRange}, trapClerance={TrapClearance}");

        //스턴 관리
        if (isStunned && !life.IsDead)
        {
            stunTimer -= Time.deltaTime;
            if (stunTimer <= 0f)
            {
                isStunned = false;
                ECurrentDefense = EMaxDefense;
            }
        }

        if (hitLockUntil > 0f && Time.time > hitLockUntil + 0.05f)
        {
            hitLockUntil = 0f;
            ResumeAgent();
        }

        if (_pendingReturnToWall)
        {
            _pendingReturnToWall = false;
            if (fsm != null) fsm.SwitchState(new MoveToWallState(fsm, this));
        }

        #if PHOTON_UNITY_NETWORKING
        if (PhotonNetwork.isMasterClient)
        #endif
        {
            UpdatePlayerTarget();
        }

        #if PHOTON_UNITY_NETWORKING
        if (!PhotonNetwork.isMasterClient) return;
#endif
    }
    //======================================
    void UpdatePlayerTarget()
    {
        var em = GameManager.Instance?.enemyManager;
        if (em == null) return;

        // 현재 가장 가까운 플레이어를 다시 얻기
        Transform newTarget = em.GetClosestPlayer(transform.position);
        if (newTarget == null) return;

        if (playerTarget != newTarget)
        {
            SetPlayerTarget(newTarget);
            playerTarget = newTarget;
        }
    }

    //🌟
    public bool TryFindTrapNearby()
    {
        //1) 씬에 존재하는 모든 Trap 컴포넌트 찾기 
        Trap[] traps = FindObjectsOfType<Trap>();

        Transform best = null;
        float bestSqr = Mathf.Infinity;
        Vector3 p = transform.position;

        float rangeSqr = detectTrapRange * detectTrapRange;

        foreach(var t in traps)
        {
            if(!t) continue;

            Vector3 diff = t.transform.position - p;
            float sqr = diff.sqrMagnitude;

            //너무 멀면 무시
            if(sqr > rangeSqr)
               continue;

            // 가장 가까운 트랩 하나만 선택 
            if(sqr < bestSqr)
            {
                bestSqr = sqr;
                best = t.transform;
            }   
            Debug.Log($"[TrapSearch] candidate = {t.name}");
        }

        trapTarget = best;
        if(trapTarget)
           Debug.Log($"[TrapSearch] final best ={trapTarget.name}");
        else
           Debug.Log("[TrapSearch] final best = NONE");


        return trapTarget != null;

    }

    
    public float CalcDamage(int attackId)
    {
        float mul = EnemyAttackCatalog.GetMultiplier(attackId);
        float dmg = AttackPower * mul;
        Debug.Log($"[EnemyCore] CalcDamage id ={attackId}k, atk={AttackPower}, mul = {mul}, dmg = {dmg}");
        return dmg;
    }


    bool CanUseSkill()
    {
        if (Time.time < nextSkillTime)
        {
            return false;
        }
        return true;
    }
    
    int SelectAttackIndex()
    {
        const int SkillIndex = 2;

        bool hasSkill = AttackCount > SkillIndex;

        //스킬 사용 가능 
        if (hasSkill && CanUseSkill())
        {
            nextSkillTime = Time.time + skillCooldown;
            return SkillIndex;
        }


        //스킬 불가
        int lightAttackCount = Mathf.Max(1, AttackCount - 1);
        return Random.Range(0, lightAttackCount); // 0,1
    }

    //==============================


    public void AgentStop()
    {
        if (!AgentReady) return;
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
    }


    public void AgentResetPath()
    {
        if (!AgentReady) return;
        agent.ResetPath();
    }



    public void BeginAttackLock()
    {
       // Debug.Log($"[Lock] Begin @ {Time.frameCount}");
        lockPos = transform.position;
        lockPosActive = true;

        if (agent && agent.enabled)
        {
            agent.ResetPath();
            agent.isStopped = true;
            agent.velocity = Vector3.zero;

            agent.updatePosition = false;
            agent.updateRotation = false;
            agent.nextPosition = transform.position;
            //agent.enabled = false;
        }
    
    if (anim) anim.SetBool("Move", false); // 로코모션 혼블렌딩 차단
}

public void EndAttackLock() {
        lockPosActive = false;
        if (!agent) return;

    if (agent) 
    {
            agent.enabled = true;

            if (!agent.isOnNavMesh)
            {
                NavMeshHit hit;
                if (NavMesh.SamplePosition(transform.position, out hit, 2f, NavMesh.AllAreas))
                {
                    agent.Warp(hit.position);
                    anim.applyRootMotion = false;
                }
                else
                {
                    return;
                }
            }
            else
            {
                agent.Warp(transform.position);
                anim.applyRootMotion = false;
            }        
        agent.updatePosition = true;
        agent.updateRotation = true;
        agent.isStopped = false;
            agent.velocity = Vector3.zero;
        

        if(currentTarget)
        {
            SetDestinationSafe(agent, currentTarget.position);
        }
    }
}

    void LateUpdate()
    {
        if (lockPosActive)
        {
            if (agent && agent.enabled)
            {
                agent.nextPosition = transform.position;
            }
        }
    }


    //🌟 추가
    
    //플레이어 타깃 설정 
    public void SetPlayerTarget(Transform newTarget)
    {
        //기존 이벤트 해제 
        if (targetHealth != null)
            targetHealth.OnDied -= OnPlayerDied;

        playerTarget = newTarget;

        //새 이벤트 등록
        if (playerTarget != null)
        {
            targetHealth = playerTarget.GetComponent<PlayerHealth>();
            if (targetHealth != null)
                targetHealth.OnDied += OnPlayerDied;
        }
        else
        {
            targetHealth = null;
        }    
    }


    public bool SetDestinationSafe(NavMeshAgent ag, Vector3 dst, float maxSnap = 2f)
    {
        if (ag == null || !ag.enabled || !ag.isOnNavMesh)
            return false;

        NavMeshHit hit;
        if (!NavMesh.SamplePosition(dst, out hit, maxSnap, NavMesh.AllAreas)) return false;

        return ag.SetDestination(hit.position);
    }
    
    public bool EnsureAgentOnNavMesh(float snap = 3f)
    {
        if (agent == null) return false;
        if (!agent.enabled) agent.enabled = true;
        if (agent.isOnNavMesh) return true;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(transform.position, out hit, snap, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
            return true;
        }
        return false;// 이 경우는 호출부에서 이동 로직 스킵 
    }
    

    void UpdateAnimSpeed(bool isRemote)
    {

        //공격 중/ 정지 명령에는 강제로 0 고정 
        if (isAttacking || (agent && agent.enabled && agent.isOnNavMesh && (agent.isStopped || !agent.hasPath)))
        {
            smoothedSpeed = Mathf.Lerp(smoothedSpeed, 0f, 20f * Time.deltaTime);
            moveFlag = false;
            if (anim)
            {
                anim.SetFloat("Speed", 0f);
                anim.SetBool("Move", false);
            }
            return;
        }
        
        float speedLike = 0f;
        if (isRemote)
        {
            //위치 변화량으로 속도 추정
            var delta = (transform.position - _prevPos).magnitude;
            speedLike = delta / Mathf.Max(Time.deltaTime, 0.0001f);
            _prevPos = transform.position;
        }
        else
        {
            speedLike = (agent && agent.enabled && agent.isOnNavMesh) ? agent.desiredVelocity.magnitude : 0f;
        }
        if (speedLike < 0.01f && agent && agent.enabled && agent.isOnNavMesh)
        {
            speedLike = agent.velocity.magnitude;
        }


        smoothedSpeed = Mathf.Lerp(smoothedSpeed, speedLike, 7f * Time.deltaTime);

        if (!moveFlag && smoothedSpeed > MOVE_ON) moveFlag = true;
        else if (moveFlag && smoothedSpeed < MOVE_OFF) moveFlag = false;

        if (anim)
        {
            if (isAttacking)
            {
                anim.SetBool("Move", false);
            }
            else

            { 
                anim.SetFloat("Speed", smoothedSpeed);
                anim.SetBool("Move", moveFlag);
            }
        }
    }
    

    private IEnumerator DelayedInit()
    {
        
        if (wallPoints == null)
            wallPoints = FindObjectOfType<WallAttackPoints>(true);
        
        yield return null; // 1프레임 대기 (모든 매니저 Awake Starte 끝날 때 까지)

        FindTargetByTag(); // 

        if (wallTarget == null)
        {
            wallTarget = FindClosestWallPoint();
        }
        
        if(fsm != null)
        fsm.SwitchState(new MoveToWallState(fsm, this));
    }

    //=======================================
    public void PauseAgent()
    {
        if (!AgentReady) return;
        agent.ResetPath(); // 남아있는 목적지 지움 
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
    }

    public void ResumeAgent()
    {
        if (!agent || life != null && life.IsDead) return;
        if (lockPosActive) return; //공격 중엔 금지 
        if (!agent.enabled || !agent.isOnNavMesh) return;

        //Debug.Log($"[ResumeAgent] at {Time.frameCount}, lock={lockPosActive}, atk={isAttacking}, onNav={agent?.isOnNavMesh}");


        agent.isStopped = false;
    }

    //===============================
    
    public void TryStartWallAttack()
    {
#if PHOTON_UNITY_NETWORKING
        if(!PhotonNetwork.isMasterClient) return; 
#endif
        if (life == null || life.IsDead) return;

        WallManager wall = FindObjectOfType<WallManager>();
        if (wall == null) return;

        if (Time.time < nextAttackAt) return;
        int idx = SelectAttackIndex();

#if PHOTON_UNITY_NETWORKING
        if (IsNetReady()) pv.RPC("RPC_PlayerAttack", PhotonTargets.All, idx);
        else
        {
            anim.SetInteger("AttackIndex", idx); 
            anim.ResetTrigger("Attack");
            anim.SetTrigger("Attack");
        }
#else
        anim.SetInteger("AttackIndex", idx);
        anim.ResetTrigger("Attack");
        anim.SetTrigger("Attack");
#endif        
        nextAttackAt = Time.time + 0.2f;
        // 데미지는 마스터에서만
        float wallDamage = AttackPower * DamageToWallMul;
        wall.TakeDamage(wallDamage);
        Debug.Log($"WallDmg = {wallDamage}, ATK={AttackPower}, Mul={DamageToWallMul}");

    }


    bool IsNetReady()
    {
#if PHOTON_UNITY_NETWORKING
       return pv && PhotonNetwork.connected && PhotonNetwork.inRoom;
#else
        return false;
#endif        
    }

    [PunRPC]
    void RPC_PlayerAttack(int idx) 
    {
        anim.SetInteger("AttackIndex", idx);
        anim.ResetTrigger("Attack");
        anim.SetTrigger("Attack"); 
    }

    public void TryStartPlayerAttack(Transform target)
    {
#if PHOTON_UNITY_NETWORKING
        if(!PhotonNetwork.isMasterClient) return;
#endif        
        if (life == null || life.IsDead) return;
        if (target == null || anim == null) return;

        if (Time.time < nextAttackAt) return;


        //이미 공격 중이면 다시 공격하지 않음 
        var st = anim.GetCurrentAnimatorStateInfo(0);
        if (st.tagHash == Animator.StringToHash("Attack")) return;
        if (anim.IsInTransition(0)) return;

        if(agent && agent.enabled)
        agent.isStopped = true;

        int idx = SelectAttackIndex();
        //Debug.Log($"Do atk idx = {idx}");

#if PHOTON_UNITY_NETWORKING
        if (IsNetReady()) pv.RPC("RPC_PlayerAttack", PhotonTargets.All, idx);
        else { anim.SetInteger("AttackIndex", idx); anim.ResetTrigger("Attack"); anim.SetTrigger("Attack"); }
#else
        anim.SetInteger("AttackIndex", idx); anim.ResetTrigger("Attack"); anim.SetTrigger("Attack");
#endif
        nextAttackAt = Time.time + 0.2f; 
        Debug.Log($"ATK={AttackPower}, Mul={DamageToWallMul}");

    }

    //추후 사용할 것 
    // IEnumerator HitFlash()
    // {
    //     //히트 이펙트, 
    //     yield return new WaitForSeconds(0.1f); 
    // }

    // public void SetMoveAnim(bool isMoving)
    // {
    //     if (anim == null)
    //         return;

    //     anim.SetBool("Move", isMoving);
    // }

    //트리거 리셋
    public void ResetAttackTriggers()
    {
        if (anim) anim.ResetTrigger("Attack");

    }
    
    //=========================

    public Transform FindClosestWallPoint()
    {

        if (!wallPoints || wallPoints.Points.Count == 0)
        {
            Debug.LogError("[EnemyCore] Wall 포인트 레지스트리가 비어있습니다.", this);
            return null;
        }
            
            
        Transform best = null;
        float bestSqr = float.MaxValue;
        Vector3 p = transform.position;

        foreach (var t in wallPoints.Points)
        {
            if (!t) continue;
            float d = (t.position - p).sqrMagnitude;
            if (d < bestSqr) { bestSqr = d; best = t; }

        }
        return best;
    }


    //======================================================

    void FindTargetByTag()
    {
        //플레이어 중 가장 가까운 타겟을 찾기 (enemyManager 경유)
        if (playerTarget == null)
        {
            var em = GameManager.Instance ? GameManager.Instance.enemyManager : null;
            if (em != null)
            {
                //playerTarget = em.GetClosestPlayer(transform.position);
                playerTarget = em.GetClosestPlayer(transform.position);
            }
        }

        //폴백  A: 씬에서 직접 탐색 
        if (playerTarget == null)
        {
            var pc = FindObjectOfType<PlayerController>(true);
            if (pc) playerTarget = pc.transform;
            else
            {
                var ph = FindObjectOfType<PlayerHealth>(true);
                if (ph) playerTarget = ph.transform;
            }
        }
        
        if(playerTarget != null)
        {
            SetPlayerTarget(playerTarget); // 자동 이벤트 
        }

        //Colossal Titna은 여전히 PlayerManager에서 찾기 
        if (colossalTarget == null)
        {
            var sm = StageManager.Instance ?? FindObjectOfType<StageManager>();
            if (sm != null && sm.ColossalTitan != null)
            {
                colossalTarget = sm.ColossalTitan.transform;
            }
        }

        //벽 타겟 유지 
        if (wallTarget == null)
        {
            wallTarget = FindClosestWallPoint();
        }
    }


//🌟
    public void ApplyHit(int attackId, PhotonView attackerPv = null)
{
    if (life != null && !life.IsDead)
        {
            //막타 후보 저장 
            if (attackerPv != null)
            {
                var pc = attackerPv.GetComponent<PlayerController>()
                          ?? attackerPv.GetComponentInParent<PlayerController>();
                          
                if (pc != null)
                    lastAttacker = pc;
            }

            //네트워크용 ViewId 추출
            int attackerVid = 0;
#if PHOTON_UNITY_NETWORKING
            attackerVid = attackerPv ? attackerPv.viewID : 0;
            Debug.Log($"[EnemyCore] ApplyHit atkId = {attackId}, attackerVid = {attackerVid}");
#endif
            float damage = CalcDamage(attackId);
            life.TakeDamage(damage, attackerVid);
        }

    // // ▼ 전투 반응(공통) — 필요 최소치만 유지
    // if (ECurrentDefense == 0 && !isStunned && canBeStunned)
    //     EnterStun(stunDuration);

    hitLockUntil = Time.time + 0.30f;
    PauseAgent();
    if (anim) anim.ResetTrigger("Attack");
}


//🌟
    void ApplyHit_Authoritative(int attackId, PlayerController attackerPlayer)
    {

#if PHOTON_UNITY_NETWORKING
        if (PhotonNetwork.connected && !PhotonNetwork.isMasterClient) return;
#endif
        if (life == null || life.IsDead) return;

        //막타 기록 
        if (attackerPlayer != null)
        {
            lastAttacker = attackerPlayer;
        }


        //공격자 ViewID 얻기
        int attackerViewId = 0;
#if PHOTON_UNITY_NETWORKING
        var apv = attackerPlayer ? attackerPlayer.GetComponent<PhotonView>() : null;
        attackerViewId = apv ? apv.viewID : 0;
#endif
        float damage = CalcDamage(attackId);
        life.TakeDamage(damage, attackerViewId);


        // //4웨이브 보스 그로기
        // if (ECurrentDefense == 0 && isStunned == false && canBeStunned)
        // {
        //     EnterStun(stunDuration);
        // }

        //Instantiate 피격 이벤트 (프리팹 )
        //피격 사운드 

        //StartCoroutine(HitFlash());

        hitLockUntil = Time.time + 0.30f;
        PauseAgent();

        if(anim) anim.ResetTrigger("Attack");

    }
    

    //===========================스턴 함수==============================
    void EnterStun(float duration)
    {
        EndAttackLock();

        isStunned = true;
        stunTimer = duration;
        ECurrentDefense = 0;

        if (agent != null)
        {
            agent.isStopped = true;
        }

        anim.SetTrigger("Stun");

        //이동/공격 잠깐 멈추도록 다른 컴포넌트에게 알려주기
    }


    //=========================죽었을 때 행동 정리용 함수========================
    public void OnDeath()
    {
        EndAttackLock();

        isAttacking = false;
        if (fsm != null) fsm.enabled = false;

        anim.SetBool("Die", true);

        ResetAttackTriggers();
        //SetMoveAnim(false);

        anim.CrossFadeInFixedTime("Die", 0.1f);
        StartCoroutine(FreezeAfterDelay(0.45f));


        //추후 이펙트 추가
        //Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
        //AudioManager.Play("EnemyDeat");

        //킬카운트 +
        if (lastAttacker != null)
        {
            var killerPv = lastAttacker.GetComponent<PhotonView>();
            if (killerPv != null)
            {
                PlayerStatsTracker.Instance?.AddKill(killerPv.owner);
            }
        }

        if(targetHealth != null)
        {
            targetHealth.OnDied -= OnPlayerDied;
        }
    }




    private IEnumerator FreezeAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);


        if (agent != null)
        {
            //이동 물리 막기
            if (agent.enabled && agent.isOnNavMesh)
            {
                agent.ResetPath();
            }

                agent.isStopped = true;
                agent.updatePosition = false;
                agent.updateRotation = false;
                agent.nextPosition = transform.position;

            
        }
        col.enabled = false;
        rb.isKinematic = true;
    }



    // ----------- Chase/Attack 상태에서 쓰는 유틸 ------------
    //================= AI / MOVEMENT HELPERS ===============

    /*
     - 타겟 우선 순위
     1. Colossal  (멀리 있어도 인식)
     2. 일반 플레이어 (가까이 오면 인식)
     3. 벽
    
    
    */


    //❗누구를 쫓을지 결정 (벽 or 플레이어)❗
    public Transform DecideTarget()
    {
        if(IsWallOnly)
        {
            currentTarget = wallTarget;
            return currentTarget;
        }

        Transform best = wallTarget;

        //1) 초대형 우선 
        if (colossalTarget != null)
        {
            float colossalDist = Vector3.Distance(transform.position, colossalTarget.position);
            if (colossalDist <= ColossalRange)
            {
                best = colossalTarget;
            }
        }

        //2) 일반 플레이어 근접 시 스위치 
        if (playerTarget != null)
        {
            float playerDist = Vector3.Distance(transform.position, playerTarget.position);
            if (currentTarget == playerTarget)
            {
                if (!IsAggroExit(playerDist)) best = playerTarget;
                else best = wallTarget;
            }
            else
            {
                if (IsAggroEnter(playerDist)) best = playerTarget;
            }
        }

        currentTarget = best;
        // Debug.Log($"[Aggro] best={(currentTarget?currentTarget.name:"null")}, dist={(currentTarget?Vector3.Distance(transform.position,currentTarget.position):999f):F1}");

        return currentTarget;

    }



    public void DoTrapAttackTick()
    {
        if (!trapTarget) return;
        
        Trap trap = trapTarget.GetComponent<Trap>();
        if (!trap) return;

        //애니메이션 
        PlayTrapAttackAnim(); 

        //Trap 에 데미지 주기
        float damage = CalcDamage(1001); 
        trap.TakeDamage(damage, gameObject);
    }

    public void PlayTrapAttackAnim()
    {
        if (anim == null) return;

        int idx = SelectAttackIndex(); 

#if PHOTON_UNITY_NETWORKING
        if(IsNetReady())
        {
            pv.RPC("RPC_PlayerAttack", PhotonTargets.All, idx);
        }        
        else
        {
            anim.SetInteger("AttackIndex", idx);
            anim.ResetTrigger("Attack");
            anim.SetTrigger("Attack");
        }
#else
            anim.SetInteger("AttackIndex", idx);
            anim.ResetTrigger("Attack");
            anim.SetTrigger("Attack");
#endif    
    }


    //현재 타겟과의 거리 
    public float DistToTarget()
    {
        if (currentTarget == null) return Mathf.Infinity;
        return Vector3.Distance(transform.position, currentTarget.position);
    }




    //NavMesh로 이동 
    public void MoveToTarget()
    {
        if (!agent || !agent.enabled || !agent.isOnNavMesh || currentTarget == null)
            return;

        if (!agent.isOnNavMesh) return;

        if (IsHitLocked || isAttacking || lockPosActive || life.IsDead || isStunned)
        {
            if (agent && agent.enabled)
                agent.isStopped = true;
            anim?.SetBool("Move", false);
            return;
        }

        agent.isStopped = false;
        agent.speed = EMoveSpeed;
        SetDestinationSafe(agent, currentTarget.position);
        Face(currentTarget);
    }
    


    private void OnPlayerDied(PlayerHealth dead)
    {
        if (dead != targetHealth) return;

        // 1) 공격/락 완전 해제
        isAttacking = false;
        hitLockUntil = 0f;
        EndAttackLock(); // lockPosActive=false, agent 업데이트 복구
        anim.applyRootMotion = false;
        ResetAttackTriggers();

        // 2) 에이전트 재가동 + 목적지 보장
        if (agent && agent.enabled)
        {
            AgentResetPath();
            agent.isStopped = false;
            agent.velocity = Vector3.zero;
        }

        // 3) 즉시 로코모션로 페이드 (무브 파라미터 재적용)
        if (anim)
        {
            anim.ResetTrigger("Hit");
            anim.CrossFadeInFixedTime("Idle", 0.05f); // Idle/Move 블렌드 트리 이름
        }

        // 4) 타깃 정리 + 복귀
        playerTarget = null;
        targetHealth = null;

        //다음 프레임에 한번만 fSM 전환
        _pendingReturnToWall = true;
    }

    public void StopAndFace()
    {
        AgentStop(); 

        if(currentTarget != null)
        {
            Face(currentTarget);
        }
    }

    public void Face(Transform tgt)
    {
        Vector3 lookDir = tgt.position - transform.position;
        lookDir.y = 0f;
        if (lookDir.sqrMagnitude > 0.001f)
        {
            Quaternion r = Quaternion.LookRotation(lookDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, r, 5f * Time.deltaTime);
        }
    }


    public void FaceSoft(Transform tgt, float rotateSpeed = 3f)
    {
        if (tgt == null) return;

        Vector3 lookDir = tgt.position - transform.position;
        lookDir.y = 0f;
        if (lookDir.sqrMagnitude < 0.001f) return;

        Quaternion r = Quaternion.LookRotation(lookDir);
        transform.rotation = Quaternion.Slerp(transform.rotation, r, rotateSpeed * Time.deltaTime);
    }

    // public void AnimEvt_AttackEnd()
    // {
    //     if (anim) anim.SetTrigger("AttackEnd");
    //     MarkAttackExitPending(); 
    // }


    public void MarkAttackExitPending()
    {
        if (attackExitPending) return;
        StartCoroutine(Co_EndAfterTransition());
    }

    IEnumerator Co_EndAfterTransition()
    {
        attackExitPending = true;

        while (anim && anim.IsInTransition(0))
            yield return null;

        EndAttackLock();
        attackExitPending = false;    
    }

    public void SFX_EnemyAttack()
    {
        SFXManager.Instance.PlaySFX("Punch1");
    }

    public void SFX_EnemyAttack2()
    {
        SFXManager.Instance.PlaySFX("Punch2");
    }

    public void SFX_EnemyKick()
    {
        SFXManager.Instance.PlaySFX("FemailSkill");
    }

    public void SFX_BossSkill()
    {
        SFXManager.Instance.PlaySFX("ArmoredSkill");
    }

    
}
