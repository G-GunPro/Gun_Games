using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance{ get; private set; }

    //[Header("System References")]
    public EnemyManager enemyManager;
    public ItemManager itemManager;
    public WallManager wallManager;
    public UIManager uiManager;
    public PerksManager perksManager;
    public TeamResourceManager teamResourceManager;
    public StageManager stageManager;
    public ResultsBootstrap resultsBootstrap;
    PhotonView pv;
    private bool isGameActive = false;
    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        pv=GetComponent<PhotonView>();
    }
    void Start()
    {
        if (uiManager == null)
            uiManager = FindObjectOfType<UIManager>();
        InitSystems();
    }

    void Update()
    {
        if (!isGameActive) return;

        enemyManager.UpdateSystem();
        wallManager.UpdateSystem();
        uiManager.UpdateSystem();

        if (Input.GetKeyDown(KeyCode.F9))
        {
            PhotonView pv = GetComponent<PhotonView>();
            pv.RPC("RPC_OnGameClear", PhotonTargets.All);
        }
        //테스트용: TAB 누르면 적 전부 제거 -> 정상적인 "웨이브 클리어" 플로우 타도록
        // if (Input.GetKeyDown(KeyCode.Tab))
        // {
        //     Debug.Log("[Debug] TAB: 강제로 모든 적 제거");
        //     //enemyManager.KillAllEnemis();
        // }
    }

    void InitSystems()
    {
        enemyManager.Init();
        perksManager.Init();
        itemManager.Init();
        wallManager.Init();
        teamResourceManager.Init();
        uiManager.Init();
        isGameActive = true;
    }

    public void ReleaseAllSystems()
    {
        Debug.Log("[GameManager] ReleaseAllSystems() called");

        if (enemyManager != null) enemyManager.ReleaseSystem();
        if (itemManager != null) itemManager.ReleaseSystem();
        if (wallManager != null) wallManager.ReleaseSystem();
        if (teamResourceManager != null) teamResourceManager.ReleaseSystem();
        if (uiManager != null) uiManager.ReleaseSystem();
        if (perksManager != null) perksManager.ReleaseSystem();

        isGameActive = false;
    }


    public void OnWaveClear()
    {
        // 1) 코인 지급 (딱 1회만)
        teamResourceManager.GiveWaveReward();   // 팀 + 개인 코인 +1

        // 2) 상점 웨이브 제한 초기화 (다음 웨이브 다시 구매 가능)
        var shop = FindObjectOfType<ShopUI>(true);
        if (shop)
        {
            shop.OnWaveEnd();   // purchasedThisWave = false; 내부에서 RefreshHeader()도 호출됨
                                // 필요하면 여기서 한 번 더: shop.RefreshHeader();
        }

    
    }
    [PunRPC]
    public void RPC_OnGameClear()
    {
        if (!isGameActive) return;
        isGameActive = false;

        Debug.Log("[GameManager] Game Clear");
        Time.timeScale = 0f; // 일시정지
        BGMManager.Instance?.PlayGameClearBGM();
        if (uiManager != null)
        {
            uiManager.ShowGameClearPanel();
            // 널 가드 (stageManager null일 때 방지)
            if (stageManager != null)
                uiManager.UpdateWaveUI(stageManager.currentWave, stageManager.maxWave);
        }

        //  여기부터 결과 테이블(Victory 결과창) 호출
        if (resultsBootstrap != null)
        {
            // 팀 벽 HP 가져오기 (WallManager에 맞는 이름으로 바꿔도 됨)
            int wallHpLeft = 0;
            if (wallManager != null)
            {
                // 예시: WallManager에 이런 프로퍼티/함수가 있다고 가정
                // wallHpLeft = wallManager.currentWallHP;
                // 또는:
                wallHpLeft = wallManager.GetCurrentWallHp();   // 없으면 아래 참고
            }

            int waveIndex = 0;
            if (stageManager != null)
            {
                waveIndex = stageManager.currentWave;
            }

            //  여기서 실제 결과 데이터 생성 + Victory 테이블 표시
            resultsBootstrap.OnWaveCleared(waveIndex, wallHpLeft);
        }
        else
        {
            Debug.LogWarning("[GameManager] resultsBootstrap가 인스펙터에 연결되어 있지 않습니다.");
        }
    }

    
    public void OnGameClear()
    {
        pv.RPC(nameof(RPC_OnGameClear),PhotonTargets.All);
    }

    public void OnGameOver()
    {
        pv.RPC(nameof(RPC_OnGameOver),PhotonTargets.All);
    }

    [PunRPC]
    public void RPC_OnGameOver()
    {
        if (!isGameActive) return;
        isGameActive = false;

        Debug.Log("[GameManager] Game Over");
        Time.timeScale = 0f;  // 게임 멈추기
        BGMManager.Instance?.PlayGameOverBGM();
        // 게임 종료 직전에 모든 매니저 초기화
        if (GameManager.Instance != null)
            GameManager.Instance.ReleaseAllSystems();


        if (uiManager != null)
        {
            //  패널 직접 SetActive 하지 말고 UIManager에게 맡기기
            uiManager.ShowGameOverPanel();
        }
    }



}
