using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//플레이어는 Rigidbody 와 Animator를 강제한다. 
[RequireComponent(typeof(Animator))]
public class PlayerAttack : MonoBehaviour
{
    //문자열 오타 방지
    const string P_Attack1 = "Attack1";
    const string P_Attack2 = "Attack2";
    const string P_Skill = "Skill";

//풀해쉬테그
    static readonly int Hash_Attack1 = Animator.StringToHash("Player.Combo.Attack1");
    static readonly int Hash_Attack2 = Animator.StringToHash("Player.Combo.Attack2");


    public string CurrentTriggerName { get; private set; } = "";
    //레퍼런스 연결
    Animator anim;
    //Rigidbody rb;
    PlayerMotor motor;
    PlayerController controller;
    int comboStep = 0; // 0: 준비, 1: Attack1 진행, 2: Attack2 진행(끝)
    bool comboWindowOpen = false; //공격1타 재생 도중 다음 타 입력 가능 여부 구간
    bool queuedNext = false;
    public bool IsAttacking = false; // 공격중 (공격중 이동 못하게)
    public bool skillActive = false; // 스킬 중 평타 못치게

    [Header("스킬 쿨다운")]
    [SerializeField]
    float minInterval = 0.2f; //쿨다운, 버퍼 
    [SerializeField]
    float skillCooldown = 5f; // 스킬 쿨다운
    float lastAttackTime;
    float lastSkillTime;
    bool lmbHeld = false;

    [Space(10f)]
    [Header("스킬을 쓰기 위한 조건")]
    [SerializeField]
    bool mustBeGroundedForSkill = true;
    [SerializeField]
    LayerMask groundMask;
    // [SerializeField]
    // float groundCheckDist = 0.2f;

    [Space(10f)]

    [Header("평타 1타에서 2타로 넘어가는 시간창 조절")]
    [SerializeField, Range(0f, 1f)]
    [Tooltip("창이 열리는 시점")]
    float open = 0.5f;
    [SerializeField, Range(0f, 1f)]
    [Tooltip("창이 닫히는 시점")]
    float close = 0.85f;
    [SerializeField, Range(0f, 1f)]
    [Tooltip("마우스 홀드 연계 허용 지연")]
    float holdDelay = 0.02f;

    [Space(10f)]
    [Header("스탯 연결")]
    PlayerStats stats;

    [SerializeField]
    [Tooltip("공격이 가능한가")]
    //bool hitWindowOpen = false;

    [System.Serializable]
    public struct AttackProfile
    {
        
        public float damageMul;
        public float knockback; // 왜 프로필이 있는데 쓰는거임 ?
    }

    public AttackProfile lightProfile;
    public AttackProfile skillProfile;

    [Header("평타 강화 버프")]
    public bool buffActive=false;
    private Coroutine buffRoutine;
    private float originalLightMul;
    public HitboxTrigger punchL;
    public HitboxTrigger punchR;
    public HitboxTrigger Foot;

    public bool IsUsingRootMotionSkill = false;
    //========================================================================

    public void StartAttackBuff(float newMul,float duration)
    {
        if(buffRoutine!=null)
        StopCoroutine(buffRoutine);

        buffRoutine=StartCoroutine(AttackBuffRoutine(newMul,duration));
    }

    IEnumerator AttackBuffRoutine(float newMul,float duration)
    {
        buffActive=true;

        originalLightMul=lightProfile.damageMul;

        lightProfile.damageMul=newMul;

        yield return new WaitForSeconds(duration);

        lightProfile.damageMul=originalLightMul;
        buffActive=false;


    }
    public void PunchL_Open()
    {
        float dmg = (stats ? stats.Attack : 100f) * lightProfile.damageMul;
        punchL.OpenHitWindow(dmg);
        Debug.Log(dmg);
    }

    public void PunchL_Close()
    {
        punchL.CloseHitWindow();
    }

    public void PunchR_Open()
    {   
        float dmg = (stats ? stats.Attack : 100f)* lightProfile.damageMul;
        punchR.OpenHitWindow(dmg);
        Debug.Log(dmg);
    }

    public void PunchR_Close()
    {
        punchR.CloseHitWindow();
    }

    public void Foot_Open()
    {
        float dmg = (stats ? stats.Attack : 100f)* skillProfile.damageMul;
        Foot.OpenHitWindow(dmg);//스킬은 평타보다 세게 
        Debug.Log(dmg);
    }
    
    public void Foot_Close()
    {
        Foot.CloseHitWindow();
    }
    void Awake()
    {
        anim = GetComponent<Animator>();
        stats = GetComponent<PlayerStats>();
        controller = GetComponent<PlayerController>();

        
    }

    void OnValidate() //만약 님들이 인스펙터에서 open시간은 close보다 길게 잡았을 경우 방어코드
   {
    open = Mathf.Clamp01(open);
    close = Mathf.Clamp01(close);
    if (close < open) close = open + 0.01f;
    holdDelay = Mathf.Clamp01(holdDelay);
    }

    public void HandleInput()
    {
        //========================우클 스킬(평타보다 강한 거)======================

        if (UIManager.IsUIOpen) return;

        if (Input.GetMouseButtonDown(1))
        {
            if (Time.time - lastSkillTime < skillCooldown) return;
            TrySkill();
            return; //스킬이 최우선, 즉시 종료
        }

        
        //====================스킬 중 LMB 무시================
        if (!skillActive)
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (comboStep == 0) TryAttack();
                else queuedNext = true;
            }
            lmbHeld = Input.GetMouseButton(0);
        }
        else
        {
            //스킬 중 콤보 버퍼 초기화 
            queuedNext = false;
            lmbHeld = false;
        }
        
        // ====================평타 연타==================================
        if(stats != null)
            anim.speed = stats.AttackSpeed;
            
        var st = anim.GetCurrentAnimatorStateInfo(0);
        bool inTransition = anim.IsInTransition(0); // 주어진 레이어에 전환이 있으면 ture, 그렇지 않으면 false;
        var next = anim.GetNextAnimatorStateInfo(0);

        //풀경로 해시 판정
        bool inAtk1 = (!inTransition && st.fullPathHash == Hash_Attack1) || (inTransition && next.fullPathHash == Hash_Attack1); //초기 프레임이 Attack이 false를 위해 보강 
        //열릴 구간(대략값): 타격 직후 ~ 수습 직전

        float norm = (!inTransition ? st.normalizedTime : next.normalizedTime) % 1f; //전환중이아니다 = 현재 꺼 노말라이즈 , 전환중이다. 다음꺼 노멀라이즈 


        //창 열림 조건을 먼저 계산하기 
        comboWindowOpen = (comboStep == 1) && inAtk1 && (norm >= open && norm <= close); // 콤보 시작, Attack1중, 타격 직후 시간보다 길고, 수습 직전 시간보다 작을때 
        //bool allowHold = lmbHeld && (norm > open + holdDelay); //미세 지연 
        bool allowHold = false;
        if (comboWindowOpen && (queuedNext || allowHold)) //lmbHeld == 홀드시 연타
        {
            queuedNext = false; //한 번만 소비 
            comboStep = 2;
            anim.ResetTrigger(P_Attack2);
            anim.SetTrigger(P_Attack2);
            CurrentTriggerName = P_Attack2;
            
            if (controller == null) return;
            controller.SendAttackRPC("Attack2");

            lastAttackTime = Time.time;
        }
        //=====================================================================
    }


    //평타 사용 
    void TryAttack()
    {
        
        if (mustBeGroundedForSkill && (motor != null && !motor.IsGrounded())) return;
        Debug.Log("TryAttack 실행됨");
        //너무 빠른 연타 방지 
        if (Time.time - lastAttackTime < minInterval)
            return;


        if (comboStep == 0)
        {//첫 타 시작 
            comboStep = 1;
            lastAttackTime = Time.time;
            IsAttacking = true;
            anim.ResetTrigger(P_Attack1); //이중 프레임 방지 
            anim.SetTrigger(P_Attack1);
            CurrentTriggerName = P_Attack1;

            if (controller == null) return;
            controller.SendAttackRPC("Attack1");

        }
        else if (comboStep == 1)
        {
            queuedNext = true; //창이 열리기 전에도 눌렀다를 기록하기 
        }
    }


    //스킬 사용 
    void TrySkill()
    {
        if (mustBeGroundedForSkill && !anim.GetBool("Grounded")) return; // 땅이 아닌데 스킬 썻다 리턴
        if (Time.time - lastSkillTime < skillCooldown) return; //스킬 쿨타임이 돌지 않았는데 썻다 리턴 

        comboStep = 0;
        IsAttacking = true;

        queuedNext = false;

        IsUsingRootMotionSkill = true;

        anim.ResetTrigger(P_Attack1);
        anim.ResetTrigger(P_Attack2);

        anim.ResetTrigger(P_Skill);
        anim.SetTrigger(P_Skill);
        CurrentTriggerName = P_Skill;

        lastAttackTime = Time.time;
        //필요 시 CrossFade로 강제 전이
        //anim.CrossFade(skillStateHash, 0.05f);
        if (controller == null) return;
            controller.SendAttackRPC("Skill");
        lastSkillTime = Time.time;
    }

    //애니메이션 끝에 붙일 이벤트용 함수
    public void SkillEnd()
    {
        comboStep = 0;
        queuedNext = false;
        comboWindowOpen = false;
        IsAttacking = false;
        skillActive = false;
        Debug.Log("SKilleENd소환");

        IsUsingRootMotionSkill = false;
    }

    //실제 타격 시점 (현재 Debug만 할 예정)  
    public void AttackHit()
    {
        Debug.Log("데미지 들어감!");
        Debug.Log($"AttackHit at step {comboStep}");
    }

    //각 애니메이션 끝부분에 Animation Event로 호출하기 
    public void AttackEnd()
    {
        comboStep = 0;
        queuedNext = false;
        comboWindowOpen = false;
        IsAttacking = false; // 모터에 알려서 이동 둔화를 풀고 싶을 때 사용 
    }

    public void AttackCancel()
    {
        comboStep = 0;
        queuedNext = false;
        comboWindowOpen = false;
        IsAttacking = false;
        anim.ResetTrigger(P_Attack1);
        anim.ResetTrigger(P_Attack2);
    }

    public void RemoteAttackTrigger(string triggerName)
    {
        if (IsAttacking) return;
        IsAttacking = true;

        comboStep = 0;
        queuedNext = false;
        comboWindowOpen = false;

        IsUsingRootMotionSkill = triggerName == "Skill";
        
        anim.ResetTrigger(P_Attack1);
        anim.ResetTrigger(P_Attack2);
        anim.ResetTrigger(P_Skill);

        anim.SetTrigger(triggerName);

    }

    public void SFX_Punch1()
    {
        SFXManager.Instance.PlaySFX("Punch1");
    }
    public void SFX_Punch2()
    {
        SFXManager.Instance.PlaySFX("Punch2");
    }
    public void SFX_ArmoredSkill()
    {
        SFXManager.Instance.PlaySFX("ArmoredSkill");
    }
    public void SFX_AttackSkill()
    {
        SFXManager.Instance.PlaySFX("AttackSkill");
    }
    public void SFX_BeastSkill()
    {
        SFXManager.Instance.PlaySFX("BeastSkill");
    }
    public void SFX_CartSkill()
    {
        SFXManager.Instance.PlaySFX("CartSkill_Heal");
    }
    public void SFX_ColossusSkill()
    {
        SFXManager.Instance.PlaySFX("ColossusSkill");
    }
    public void SFX_FemailSkill()
    {
        SFXManager.Instance.PlaySFX("FemailSkill");
    }
    public void SFX_JawSkill()
    {
        SFXManager.Instance.PlaySFX("JawSkill");
    }
    public void SFX_WarHammerSkill()
    {
        SFXManager.Instance.PlaySFX("WarHammerSkill");
    }
}
