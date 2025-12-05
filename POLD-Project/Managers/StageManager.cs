using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Unity.VisualScripting;
public class StageManager : MonoBehaviour
{
    [Header("Photon UI")]
    // 접속된 플레이어 수를 표시할 Text UI 항목 연결 레퍼런스 (Text 컴포넌트 연결 레퍼런스)
    public Text txtConnect;
    // 접속 로그를 표시할 Text UI 항목 연결 레퍼런스 선언
    public Text txtLogMsg;
    // 채팅 로그를 표시할 Text UI 항목 연결 레퍼런스 선언
    public Text txtChatMsg;
    // 입력한 채팅을 가져올 InputField 레퍼런스 선언
    public InputField inputChat;

    [Header("Wave Settings")]
    public int currentWave = 0;
    public int maxWave = 5;
    public float startDelay = 5f;
    public float nextWaveDelay = 10f;

    private bool isWaveActive = false;
    private bool gameEnd = false;
    private bool nextWaveReady = false;

    HUDController hud;
    
    private PhotonView pv;
    private Transform[] playerPos;
    public Transform GetSpawnPoint(int actorNumber)
    {
        int index = (actorNumber - 1) % playerPos.Length;
        return playerPos[index];
    }

    public static StageManager Instance {get; private set;} 
    public PlayerController ColossalTitan { get; private set; }

    //🌟11.10 추가
    private List<PlayerHealth> allPlayers = new List<PlayerHealth>();
    void Awake()
    {
        Instance=this;
        pv = GetComponent<PhotonView>();
        PhotonNetwork.isMessageQueueRunning = true;
        PhotonNetwork.sendRate = 30;
        PhotonNetwork.sendRateOnSerialize = 30;
        playerPos = GameObject.Find("PlayerSpawnPoint").GetComponentsInChildren<Transform>();
        hud = FindObjectOfType<HUDController>();
    }
    void Start()
    {
        StartCoroutine(WaitForRoomAndStart());
    }
    void Update()
    {
        if(PhotonNetwork.isMasterClient && Input.GetKeyDown(KeyCode.K))
        {
            var em=GameManager.Instance != null ? GameManager.Instance.enemyManager : FindObjectOfType<EnemyManager>();
            if (em != null)
            {
                em.KillAllEnemiesDebug();
            }
            else
            {
                Debug.LogWarning("[StageManager] EnemyManager 를 찾을 수 없습니다.");
            }
        }
    }

    
    IEnumerator WaitForRoomAndStart()
    {
        // PhotonNetwork.inRoom 될 때까지 대기
        yield return new WaitUntil(() => PhotonNetwork.inRoom);

        // 플레이어 생성
        yield return StartCoroutine(CreatePlayer());

        // PerksVoteManager 네트워크 오브젝트 생성
        if (PhotonNetwork.isMasterClient && FindObjectOfType<PerksVoteManager>() == null)
        {
            if (FindObjectOfType<PerksVoteManager>() == null)
            {
                GameObject obj = PhotonNetwork.InstantiateSceneObject(
                "VoteManager",
                Vector3.zero,
                Quaternion.identity,
                0,
                null
                );
                DontDestroyOnLoad(obj);
                Debug.Log("[GameManager] PerksVoteManager 네트워크 오브젝트 생성 완료");
            }
        }

        // 마스터만 Enemy 생성 루프 실행
        if (PhotonNetwork.isMasterClient)
        {
            BGMManager.Instance?.PlayBGM(BGMManager.Instance.wave1ReadyAndPlay);

            Debug.Log("[Stage] 5초 대기 후 첫 웨이브 시작");
            yield return new WaitForSeconds(startDelay);
            StartCoroutine(WaveRoutine());
        }
    }
    IEnumerator WaveRoutine()
    {
        EnemyManager enemyManager = FindObjectOfType<EnemyManager>();
        
        if (enemyManager == null)
        {
            Debug.LogError("[StageManager] EnemyManager를 찾을 수 없습니다");
            yield break;
        }
        
        while (!gameEnd)
        {
            currentWave++;
            Debug.Log($"[Stage] Wave {currentWave} 시작");
            pv.RPC("RPC_OnWaveStart", PhotonTargets.All, currentWave);
            
            if (PhotonNetwork.isMasterClient)
                enemyManager.SpawnWaveEnemies(currentWave);
            
            isWaveActive = true;

            yield return new WaitUntil(() => enemyManager.ActiveEnemyCount == 0);
            
            isWaveActive = false;

            pv.RPC("RPC_OnWaveClear", PhotonTargets.All, currentWave);

            if (currentWave >= maxWave)
            {
                Debug.Log("[Stage] 마지막 웨이브 클리어 -> 게임 종료");
                gameEnd = true;
                GameManager.Instance.OnGameClear();
                yield break;
            }

            // 웨이브 종료 후 투표 시작
            if (PhotonNetwork.isMasterClient && PerksVoteManager.Instance != null && currentWave < maxWave)
            {
                Debug.Log("[Stage] 웨이브 종료 -> 팀 특전 투표 시작");
                PerksVoteManager.Instance.StartVote();
            }

            // 다음 웨이브 신호 대기
            Debug.Log("[Stage] 다음 웨이브 신호 대기 중..");
            yield return new WaitUntil(() => nextWaveReady);
            nextWaveReady = false;

            

            yield return new WaitForSeconds(nextWaveDelay);
        }
    }

    [PunRPC]
    void RPC_OnWaveStart(int wave)
    {
        Debug.Log($"[RPC] Wave {wave} 시작");
        var ui = GameManager.Instance.uiManager;
        if (ui != null)
            ui.ShowWaveStart();
        
        BGMManager.Instance?.PlayWaveBGM(wave);
        if (hud != null)
            hud.SetWaveAndTime(wave);
    }
    [PunRPC]
    void RPC_OnWaveClear(int wave)
    {
        Debug.Log($"[RPC] Wave {wave} 클리어");
        
        GameManager.Instance.OnWaveClear();
        var ui = GameManager.Instance.uiManager;
        if (ui != null)
            ui.ShowWaveClear();

        BGMManager.Instance?.PlayBetweenWaveBGM();
    }
    [PunRPC]
    public void RPC_NotifyNextWave()
    {
        Debug.Log("[Stage] 다음 웨이브 시작 신호 수신");
        nextWaveReady = true;

        BGMManager.Instance?.PlayBGM(BGMManager.Instance.wave1ReadyAndPlay);
    }


    // 플레이어를 생성하는 함수
    IEnumerator CreatePlayer()
    {
        PhotonNetwork.isMessageQueueRunning = false;
        yield return new WaitUntil(() => PhotonNetwork.inRoom);
        PhotonNetwork.isMessageQueueRunning = true;
    
        int myID = PhotonNetwork.player.ID;
        int rank = 0;
        PhotonPlayer[] all = PhotonNetwork.playerList;
        for (int i = 0; i < all.Length; i++)
            if (all[i].ID < myID) rank++;

        int spawnCount = Mathf.Max(0, playerPos.Length - 1);
        int index = (rank % spawnCount) + 1;
        Transform spawnPoint = playerPos[index];

        object selectedTitan;
        PhotonNetwork.player.CustomProperties.TryGetValue("SelectedTitan", out selectedTitan);
        string titanName = selectedTitan != null ? selectedTitan.ToString() : "Attack";
        string prefabPath = $"PlayerPrefab/{titanName}";

        object[] initData = new object[]
        {
            PhotonNetwork.player.NickName,
            titanName,
            PhotonNetwork.player.ID
        };

        GameObject player = PhotonNetwork.Instantiate(
            prefabPath,
            spawnPoint.position,
            spawnPoint.rotation,
            0,
            initData
        );

        player.name = $"Player_{PhotonNetwork.player.NickName}";

        var controller = player.GetComponent<PlayerController>();
        controller.ApplyTitanPower(titanName);
        Debug.Log($"[Spawn] {PhotonNetwork.player.NickName} -> {spawnPoint.name} (rank:{rank}, index:{index})");

        
        var ph = player.GetComponent<PlayerHealth>();
        if (ph != null && ph.playerTitanName == "Colossal Titan")
        {
            ColossalTitan = controller;
            Debug.Log("[Statemanager] Colossal Titan 등록 완료");
        }

        Debug.Log($"[Spawn] {PhotonNetwork.player.NickName} 스폰 완료");

        var invUI = FindObjectOfType<InventoryUI>(true);
        var ps = player.GetComponent<PlayerStats>();
        if (invUI && ps)
        {
            invUI.AttachPlayer(ps);
            Debug.Log("[StageManager] InventoryUI에 PlayerStats 연결 완료");
        }
        else
        {
            Debug.LogWarning("[StageManager] PlayerStats 또는 InventoryUI를 찾을 수 없음");
        }

        yield return null;

    }

    void GetConnectPlayerCount()
    {
        // 현재 입장한 룸 정보를 받아옴(레퍼런스 연결)
        Room currRoom = PhotonNetwork.room;

        // 현재 룸의 접속자 수와 최대 접속 가능한 수를 문자열로 구성한 다음 Text UI 항목에 출력
        txtConnect.text = $"{currRoom.PlayerCount}/{currRoom.MaxPlayers}";
    }

    void OnPhotonPlayerConnected(PhotonPlayer newPlayer)
    {
        Debug.Log($"[Photon] Player Connected : {newPlayer.NickName}");
    }

    public void OnClickChatBtn()
    {
        string msg = "\n\t<color=#ffffff>["
                    + PhotonNetwork.player.NickName
                    + " : "
                    + inputChat.text
                    + "]</Color>";
        pv.RPC("ChatMsg", PhotonTargets.AllBuffered, msg);
    }
    [PunRPC]
    void ChatMsg(string msg)
    {
        txtChatMsg.text += msg;
    }
    [PunRPC]
    void LogMsg(string msg)
    {
        txtLogMsg.text += msg;
    }

    //포톤 추가
    //룸 나가기 버튼 클릭 이벤트에 연결될 함수
    public void OnClickExitRoom()
    {
        //로그 메시지에 출력할 문자열 생성
        string msg = "\n\t<color=#ff0000>["
                    + PhotonNetwork.player.NickName
                    + "]Disconnected</color>";

        //RPC 함수 호출
        pv.RPC("LogMsg", PhotonTargets.AllBuffered, msg);

        //현재 룸을 빠져나가며 생성한 모든 네트워크 객체를 삭제
        PhotonNetwork.LeaveRoom();

        //(!) 서버에 통보한 후 룸에서 나가려는 클라이언트가 생성한 모든 네트워크 객체및 RPC를 제거하는 과정 진행(포톤 서버에서 진행)

    }

    // 포톤 추가
    // 룸에서 접속 종료됐을 때 호출되는 콜백 함수 ( (!) 과정 후 포톤이 호출 )
    void OnLeftRoom()
    {
        // 로비로 이동
        SceneManager.LoadScene("Lobby");
    }

    
    ////////////////////////////////////
}
