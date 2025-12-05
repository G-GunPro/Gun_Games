using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 쓰다듬기 상태
public class PetState : ICatState
{
    //private Coroutine routine;
    public void Enter(CatCtrl cat)
    {
        cat.StartCoroutine(DoPet(cat));
        
    }

    public void Update(CatCtrl cat)
    {

    }

    public void Exit(CatCtrl cat)
    {
        
        cat.StopCoroutine(DoPet(cat));
        
    }

    private IEnumerator DoPet(CatCtrl cat)
    {
        float duration = 5f;
        float elapsed = 0f;

        cat.PauseMovement();
        cat.PlayAnimation("Sound");
        cat.meow.Play();
        while (elapsed < duration)
        {

            cat.stats.AddTrust(1);
            cat.stats.AddHappiness(1);

            yield return new WaitForSeconds(1f);
            elapsed += 1f;
        }
        cat.StopAnimation("Idle");
        cat.ResumeMovement();
        cat.ChangeState(new IdleState());
    }
}
