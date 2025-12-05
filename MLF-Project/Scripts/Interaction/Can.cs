using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Can : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        Bowl bowl = other.GetComponent<Bowl>();
        if (bowl != null)
        {
            bowl.FillFood();
        }
    }
}
