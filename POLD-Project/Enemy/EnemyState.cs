using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class EnemyState 
{
protected EnemyStateMachine fsm;
    protected EnemyCore core;

    public EnemyState(EnemyStateMachine fsm, EnemyCore core) // EnemyCore = 몬스터의 실제 행동, EnemyState AI 행동 서브 클래스들이 공통으로 쓰는 도구, EnemyStateMahine 행동지지시? 
    {
        this.fsm = fsm;
        this.core = core;
    }


    /*virtual를 준 이유 
    어떤 상태는 OnExit에서 할 게 없을 수도, 어떤 상태는 OnEnter에서 아무 것도 안 할 수도 있기 때문*/
    //상태 진입 시 1번 호출
    public virtual void OnEnter() { }

    //매 프레임 호출
    public virtual void OnUpdate() { }

    //상태에서 나갈 때 1번 호출
    public virtual void OnExit() { }

}
