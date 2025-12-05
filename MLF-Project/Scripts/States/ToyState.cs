using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 장난감(놀아주기) 상태
public class ToyState : ICatState
{
    //private Coroutine routine;
    public void Enter(CatCtrl cat)
    {
        cat.StartCoroutine(DoToy(cat));
        
    }

    public void Update(CatCtrl cat)
    {

    }

    public void Exit(CatCtrl cat)
    {
        
            cat.StopCoroutine(DoToy(cat));
            
    }

    private IEnumerator DoToy(CatCtrl cat)
    {
        float duration = 10f;
        float elapsed = 0;

        cat.PauseMovement();
        cat.PlayAnimation("Idle");
        cat.meow.Play();
        while (elapsed < duration)
        {


            cat.stats.AddTrust(1);
            cat.stats.AddHappiness(1);
            cat.stats.AddHealth(1);
            cat.stats.AddEnergy(-3);

            yield return new WaitForSeconds(1f);
            elapsed += 1f;
        }
        cat.StopAnimation("Jump");
        cat.ResumeMovement();
        cat.ChangeState(new IdleState());
    }
}
