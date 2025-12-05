using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Shovel : MonoBehaviour
{
    public CatCtrl cat;
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Poop"))
        {
            Poop poop = other.GetComponent<Poop>();
            if (poop != null)
            {
                poop.CleanUp();
                cat.stats.AddCleanliness(30f);
            }
        }
    }
}
