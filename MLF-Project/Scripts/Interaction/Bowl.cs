using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bowl : MonoBehaviour
{
    public GameObject foodPrefab;
    private GameObject currentFood;
    public bool HasFood => currentFood != null;

    public void FillFood()
    {
        if (HasFood) return;

        Vector3 spawnPos = new Vector3(-9.487f, -0.023f, -1.485f);

        currentFood = Instantiate(foodPrefab, spawnPos, Quaternion.identity);
        currentFood.transform.SetParent(transform);
    }

    public void ClearFood()
    {
        if (currentFood != null)
        {
            Destroy(currentFood);
            currentFood = null;
        }
    }
}
