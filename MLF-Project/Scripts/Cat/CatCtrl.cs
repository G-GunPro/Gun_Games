using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Playables;

public class CatCtrl : MonoBehaviour
{
    //NavMeshAgent 연결 레퍼런스
    public NavMeshAgent agent;
    //행동 제어 수치 연결
    public CatStats stats;
    public Animator animator;
    private ICatState currentState;
    public GameObject poopPrefab;
    public Bowl bowl;
    public AudioSource meow;
    private bool isWalking = false;
    private bool canMove = true;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        stats = new CatStats();
        meow = GetComponentInChildren<AudioSource>();
    }
    // Start is called before the first frame update
    void Start()
    {
        ChangeState(new IdleState());
    }

    // Update is called once per frame
    void Update()
    {
        stats.Decay(Time.deltaTime);
        if (!canMove) return;
        currentState?.Update(this);

        

        if (agent.velocity.magnitude > 0.1f)
        {
            isWalking = true;
        }
        else
            isWalking = false;
        
        
        animator.SetBool("isWalking", isWalking);
    }

    public void PauseMovement()
    {
        canMove = false;
        animator.SetBool("isWalking", false);
    }
    public void ResumeMovement()
    {
        canMove = true;
    }
    public void ChangeState(ICatState newState)
    {
        currentState?.Exit(this);
        currentState = newState;
        currentState.Enter(this);
    }

    public void PlayAnimation(string name)
    {
        animator.SetTrigger(name);
    }

    public void StopAnimation(string name)
    {
        animator.SetTrigger(name);
    }

    public void MoveTo(Vector3 targetPos)
    {
        agent.SetDestination(targetPos);
    }

    public bool HasArrived()
    {
        return !agent.pathPending && agent.remainingDistance < 0.5f;
    }

    // 상호작용 이벤트
    public void OnPet() //쓰다듬기
    {
        
        ChangeState(new PetState());
    }

    public void OnPlay() //놀아주기
    {
        
        ChangeState(new ToyState());
    }

    public void OnFeedSnack() //간식주기
    {
        
        ChangeState(new SnackState());
    }

    public bool IsInIdleState()
    {
        return currentState is IdleState;
    }
}
