using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 잠자는 상태
public class SleepState : ICatState
{
    private bool sleeping = false;

    public void Enter(CatCtrl cat)
    {
        // 침대 위치 이동
        Vector3 bedPos = GameObject.FindGameObjectWithTag("Bed").transform.position;
        cat.MoveTo(bedPos);
    }

    public void Update(CatCtrl cat)
    {
        if (!sleeping && cat.HasArrived())
        {
            cat.StartCoroutine(SleepRoutine(cat));

        }
        


    }

    public void Exit(CatCtrl cat)
    {

    }

    private IEnumerator SleepRoutine(CatCtrl cat)
    {
        while (cat.stats.energy < 100f)
        {
            cat.stats.AddEnergy(2f);
            yield return new WaitForSeconds(1f);
        }

        sleeping = false;
        cat.ChangeState(new IdleState());

    }
}
