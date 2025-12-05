using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ICatState  // 상태 인터페이스
{
    void Enter(CatCtrl cat);    // 상태 진입시
    void Update(CatCtrl cat);   // 상태 진행중
    void Exit(CatCtrl cat);     // 상태 종료시
}
