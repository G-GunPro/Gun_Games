using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 간식 상태
public class SnackState : ICatState
{
    //private Coroutine routine;
    public void Enter(CatCtrl cat)
    {
        cat.StartCoroutine(DoSnack(cat));
        
    }

    public void Update(CatCtrl cat)
    {

    }

    public void Exit(CatCtrl cat)
    {
        
            cat.StopCoroutine(DoSnack(cat));
      
    }

    private IEnumerator DoSnack(CatCtrl cat)
    {
        float duration = 10f;
        float elapsed = 0f;

        cat.PauseMovement();
        cat.PlayAnimation("Eat");
        while (elapsed < duration)
        {
            
            
            cat.stats.AddTrust(1);
            cat.stats.AddHappiness(1);
            cat.stats.AddFullness(1);
            cat.stats.AddBowel(1);

            yield return new WaitForSeconds(1f);
            elapsed += 1f;
        }
        cat.StopAnimation("Idle");
        cat.ResumeMovement();
        cat.ChangeState(new IdleState());
    }
}
