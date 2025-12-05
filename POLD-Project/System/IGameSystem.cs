using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PlayerLoop;

public interface IGameSystem
{
    void Init();
    void UpdateSystem();
    void ReleaseSystem();
}