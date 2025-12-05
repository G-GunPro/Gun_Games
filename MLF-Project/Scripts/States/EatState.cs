using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 먹는 상태(식사/간식 구분)
public class EatState : ICatState
{
    private bool eating = false;

    public void Enter(CatCtrl cat)
    {
        // 밥그릇 위치 찾아서 이동
        Vector3 bowlPos = cat.bowl.transform.position + new Vector3(0f, 0f, -0.3f);
        cat.MoveTo(bowlPos);
    }

    public void Update(CatCtrl cat)
    {
        if (!eating && cat.HasArrived())
        {
            // 도착 후 먹기 시작
            cat.StartCoroutine(EatRoutine(cat));
            eating = true;
        }


    }

    public void Exit(CatCtrl cat)
    {
    }

    private IEnumerator EatRoutine(CatCtrl cat)
    {
        cat.PlayAnimation("Eat"); 
        float eatingTime = 20f;
        float timer = 0f;

        while (timer < eatingTime)
        {
            timer += Time.deltaTime;
            cat.stats.AddFullness(2f * Time.deltaTime);
            yield return null;
        }

        cat.bowl.ClearFood();

        cat.ChangeState(new IdleState());
    }
}
