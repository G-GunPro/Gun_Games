using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMotor : MonoBehaviour
{
    //=========================
    CharacterController cc;
    Animator anim;

    PlayerAttack playerAttack;
    PlayerController controller;

    [Header("Move")]
    public float moveSpeed = 8.0f;
    public float rotateSmoothTime = 0.05f;

    public bool isLocal = false;
    public CameraFollow camFollow;

    [Header("Gravity/Jump")]
    public float gravity = -9.81f; 
    public float jumpPower = 5f;
    Vector3 vel; //속도
    //private bool isBack = false; //뒤로 가기를 눌렀는가? 
    

    [Header("Dash/Stamina")]  //st = stamina임 
    public float stMax = 100f;
    public float st;    
    
    [Header("이동속도 PlayerStatse과 연결")]
    PlayerStats stats; 
    private float stDrainPerSec = 20f; // 대쉬 중 초당 소모 
    private float stRegenPerSec = 15f; //비대쉬 초당 회복 
    private float stRegenDelay = 0.5f; //대쉬 끝난 뒤 회복까지 대기 
    private float dashMultiplier = 1.8f; //대쉬 배속 속도

    public bool isBack;
    bool isDash;
    float stRegenTimer;
    public float CurrentSpeed{ get; private set; }
    public float PlayRate { get; private set; }
    
    bool isHit; // hit 중 움직임 금지
    public bool IsHit =>isHit; 

    HUDController hud;
    //===========레퍼런스 연결==============

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        anim = GetComponent<Animator>();
        playerAttack = GetComponent<PlayerAttack>();
        st = stMax; //100
        stats = GetComponent<PlayerStats>();
        controller = GetComponent<PlayerController>();
        hud = FindObjectOfType<HUDController>();
        moveSpeed = stats.MoveSpeed;
    }

    //=========================
    void Start()
    {
        vel = Vector3.zero;
        cc.Move(Vector3.down * 0.2f);
        Debug.Log($"[MOTOR] Start()에서 moveSpeed 초기값 = {moveSpeed}");

    }
    // Update is called once per frame
    public void HandleInput()
    {
        // 1. 비로컬 플레이어 중력 정지
        if (!isLocal) return;
        if (UIManager.IsUIOpen) return;

        if (anim.applyRootMotion)
            anim.applyRootMotion = false;

        if (camFollow == null)
            camFollow = FindObjectOfType<CameraFollow>();


        // 3. 카메라 방향 바라보기
        float cameraYaw = camFollow ? camFollow.GetYaw() : transform.eulerAngles.y;
        transform.rotation = Quaternion.Euler(0, cameraYaw, 0);

        // 4. 이동 입력
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        Vector3 inputDir = new Vector3(h, 0, v).normalized;
        bool hasInput = inputDir.magnitude > 0.1f;

        
        //===========공격 중 이동 잠금======
        if (playerAttack && playerAttack.IsAttacking)
            hasInput = false;
        //===========Hit 때 움직임 잠금
        if (isHit)
            hasInput = false;  
        // 5. 중력 계산
        bool grounded = cc.isGrounded;
        if (grounded && vel.y < 0)
            vel.y = -2f;
        else
            vel.y += gravity * Time.deltaTime;

        // 6. 이동
        if (hasInput && camFollow != null)
        {
            Vector3 moveDir = (Quaternion.Euler(0, cameraYaw, 0) * inputDir).normalized;

            float moveSpeedNow = moveSpeed;
            if (isDash)
                moveSpeedNow *= dashMultiplier;

            cc.Move(moveDir * moveSpeedNow * Time.deltaTime);
        }

        // 7. 점프
        if (grounded && Input.GetButtonDown("Jump"))
        {
            vel.y = jumpPower;
            anim.ResetTrigger("Jump");
            anim.SetTrigger("Jump");
            if (controller == null) return;
            controller.SendAttackRPC("Jump");
        }


        // 8. 중력 이동 적용
        cc.Move(vel * Time.deltaTime);
        
        // 9. 대쉬 / 스태미나 관리
        bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        bool forward = v > 0.1f;
        bool canDash = shift && forward && grounded && st > 0f;
        isDash = canDash;

        if (isDash)
        {
            st -= stDrainPerSec * Time.deltaTime;
            st = Mathf.Clamp(st, 0f, stMax);
            if (st <= 0f) isDash = false;
            stRegenTimer = 0f;
        }
        else
        {
            stRegenTimer += Time.deltaTime;
            if (stRegenTimer >= stRegenDelay)
                st = Mathf.Min(stMax, st + stRegenPerSec * Time.deltaTime);
        }

        isBack = v < -0.1f;
        // 10. 애니메이션 갱신
        anim.SetBool("Grounded", grounded);
        anim.SetBool("Dash", isDash);
        anim.SetBool("isBack", isBack);

        PlayRate = isDash ? 1.2f : 0.85f;
        anim.SetFloat("PlayRate", PlayRate);

        //===========최종 계산==============

        float targetSpeed = Mathf.Clamp01(Mathf.Max(0f, v) + Mathf.Abs(h) * 0.7f);
        CurrentSpeed = anim.GetFloat("Speed");
        anim.SetFloat("Speed", Mathf.Lerp(CurrentSpeed, targetSpeed, Time.deltaTime * 6f));

        if (hud != null)
        {
            hud.SetStamina(st, stMax);
        }
    }

    public bool IsGrounded()
    {
        return cc != null && cc.isGrounded;
    }
    void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 220, 24), $"Stamina: {(int)st} / {(int)stMax}");
    }

    public void SetHitLock(bool on)
    {
        isHit = on;
    }

    //hit 트리거 걸기 전 검사 
    public void PlayHitMotion()
    {
        Debug.Log($"[Hit] 요청 들어옴 / IsAttacking={playerAttack?.IsAttacking}, IsHit={IsHit}");

        //1) 공격 중이면 hit 모션 불가 
        if(playerAttack != null && playerAttack.IsAttacking)
        {
            return; //HP 감소, 애니메이션 변경 불가 
        }

        //2)hit 모션 중 -> 트리거 안 걸음 (여러번 맞아도 1번만)
        if(IsHit)
        {
            return; 
        }

        //3) 이검사를 다 지날 때  진짜 hit 애니메이션 실행 
        if(anim != null)
        {
            anim.ResetTrigger("Hit");
            anim.SetTrigger("Hit");
        }

    }
}
