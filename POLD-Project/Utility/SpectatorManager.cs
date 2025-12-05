using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class SpectatorManager
{
    static List<PlayerController> alivePlayers=new();
    static int currentIndex=0;
    static CameraFollow cam;

    public static void BeginSpectate()
    {
        cam=Object.FindObjectOfType<CameraFollow>();
        RefreshAliveList();

        if (alivePlayers.Count == 0)
        {
            return;
        }

        currentIndex=0;
        AttachCamera();
        Debug.Log($"[Spectator] 관전 시작 -> {alivePlayers[currentIndex].name}");
    }

    static void RefreshAliveList()
    {
        alivePlayers.Clear();
        var players=Object.FindObjectsOfType<PlayerHealth>();

        foreach(var p in players)
        {
            if (!p.IsDead)
            {
                var pc=p.GetComponent<PlayerController>();
                if(pc!=null)
                alivePlayers.Add(pc);
            }
        }
    }
    
    public static void Next()
    {
        RefreshAliveList();
        if(alivePlayers.Count==0)return;

        currentIndex=(currentIndex+1)%alivePlayers.Count;
        AttachCamera();
    }

    public static void Prev()
    {
        RefreshAliveList();
        if(alivePlayers.Count==0)return;

        currentIndex--;
        if(currentIndex<0)currentIndex=alivePlayers.Count-1;
        AttachCamera();
    }

    static void AttachCamera()
    {
        if(!cam) cam=Object.FindObjectOfType<CameraFollow>();
        if(!cam)return;

        cam.AttachTarget(alivePlayers[currentIndex].transform);
        Debug.Log($"[Spectator] 시점 변경 -> {alivePlayers[currentIndex].name}");
    }
}
