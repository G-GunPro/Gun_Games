using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IdleState : ICatState
{
    private float idleTimer;

    public void Enter(CatCtrl cat)
    {
        idleTimer = Random.Range(3f, 6f);
    }

    public void Update(CatCtrl cat)
    {
        idleTimer -= Time.deltaTime;

        if (idleTimer <= 0f)
        {
            Vector3 randomPos = cat.transform.position + Random.insideUnitSphere * 5f;
            randomPos.y = cat.transform.position.y;
            cat.MoveTo(randomPos);

            idleTimer = Random.Range(3f, 6f);
        }

        if (cat.stats.fullness < 30f&&cat.bowl.HasFood)
            cat.ChangeState(new EatState());
        else if (cat.stats.energy < 30f)
            cat.ChangeState(new SleepState());
        else if (cat.stats.bowel >= 100f)
            cat.ChangeState(new BowelState(cat.poopPrefab));
    }

    public void Exit(CatCtrl cat)
    {
        
    }
}
