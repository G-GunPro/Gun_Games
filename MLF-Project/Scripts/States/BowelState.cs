using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements.Experimental;

// 배변활동 상태
public class BowelState : ICatState
{
    private bool pooping = false;
    private Vector3 toiletPos;
    private GameObject poopPrefab;

    public BowelState(GameObject poop)
    {
        poopPrefab = poop;
    }
    public void Enter(CatCtrl cat)
    {
        pooping = false;
        // 화장실 위치로 이동
        toiletPos = GameObject.FindGameObjectWithTag("Toilet").transform.position;
        cat.MoveTo(toiletPos);
    }

    public void Update(CatCtrl cat)
    {
        if (!pooping && cat.HasArrived())
        {
            cat.StartCoroutine(DoPoop(cat));
            pooping = true;
        }

        GameObject[] poops = GameObject.FindGameObjectsWithTag("Poop");
        int poopCount = poops.Length;
        if (poopCount > 0)
        {
            cat.stats.AddCleanliness(Time.deltaTime * (-0.01f * poopCount));
        }
    }

    private IEnumerator DoPoop(CatCtrl cat)
    {
        cat.PlayAnimation("Idle");
        yield return new WaitForSeconds(10f);

        Vector3 spawnPos = toiletPos + new Vector3(0f,0.2f,-0.2f);
        Quaternion spawnRot = Quaternion.Euler(-90f, 90f, 0f);

        GameObject poop = GameObject.Instantiate(poopPrefab, spawnPos, spawnRot);

        cat.stats.AddBowel(-100f);
        cat.ChangeState(new IdleState());
    }
    public void Exit(CatCtrl cat)
    {
       
    }
    

}
