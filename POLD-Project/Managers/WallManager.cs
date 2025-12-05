using UnityEngine;



public class WallManager : MonoBehaviour,IGameSystem
{
    public static WallManager Instance { get; private set; }
    
    [Header("벽 체력")]
    //벽 최대 체력
    public float wallMaxHP = 10000f;
    //벽 현재 체력
    [SerializeField] private float wallHP;

    public PhotonView pv = null;
    HUDController hud;

    public void Awake()
    {
        pv = GetComponent<PhotonView>();
        if(pv ==null)
        {
            pv = gameObject.AddComponent<PhotonView>();
        }
        hud = FindObjectOfType<HUDController>();

        Instance = this;    // Instance 할당 (있으면 더 편함)
    }

    public void Init()
    {
        //wallHP = wallMaxHP;
    }

    void Start()
    {
        wallHP = wallMaxHP;    
        UpdateHUD();
    }

    void UpdateHUD()
    {
        if (hud == null) hud = FindObjectOfType<HUDController>();
        if (hud == null) return;

        hud.SetWallHp(wallHP, wallMaxHP);
    }

    public void UpdateSystem()
    {
        if (wallHP <= 0)
        {
            Debug.Log("벽 파괴됨");
            GameManager.Instance.OnGameOver();
        }
    }

    //벽 체력 회복
    public void RepairWall(float amount)
    {
        if (PhotonNetwork.isMasterClient)
        {
            wallHP = Mathf.Min(wallHP + amount, wallMaxHP);
            //Debug.Log($"벽 수리됨 현재체력 : {wallHP}/{wallMaxHP}");
            //모든 클라이언트에 HP 갱신 알림
            pv.RPC("RPC_UpdateWallHP", PhotonTargets.All, wallHP);
        }
        if (hud != null)
        {
            hud.SetWallHp(wallHP, wallMaxHP);
        }
    }

    //벽 데미지 처리
    public void TakeDamage(float dmg)
    {
        if (PhotonNetwork.isMasterClient)
        {
            wallHP -= dmg;
            if (wallHP < 0) wallHP = 0;

            //Debug.Log($"벽 피격 {dmg} 데미지. 남은 체력 : {wallHP}");
            //모든 클라이언트에 HP 갱신 알림
            pv.RPC("RPC_UpdateWallHP", PhotonTargets.All, wallHP);
        }
        if (hud != null)
        {
            hud.SetWallHp(wallHP, wallMaxHP);
        }
    }

    [PunRPC]
    void RPC_UpdateWallHP(float newHP)
    {
        wallHP = newHP;


        //Debug.Log("벽 HP 동기화됨:" + wallHP);
    }

    
    [PunRPC]
    public void RPC_IncreaseWallMaxHP(float amount)
    {
        float increaseMax = wallMaxHP * amount;
        wallMaxHP+=increaseMax;

        float healAmount = wallMaxHP * amount;
        wallHP+=healAmount;

        if(wallHP>wallMaxHP)
        wallHP=wallMaxHP;

        Debug.Log($"[Wall] 강화 적용됨 | MaxHP:{wallMaxHP}, HP:{wallHP}, Increase:{increaseMax}, Heal:{healAmount}");
        UpdateHUD();
    }
    public void ReleaseSystem()
    {
        wallHP = wallMaxHP;
        Debug.Log("[WallManager] 초기화 완료");
    }

    // ===========================================================
    // ➤ 여기 추가 : 결과창에서 사용할 벽 HP Getter
    // ===========================================================
    public int GetCurrentWallHp()
    {
        return Mathf.RoundToInt(wallHP);
    }
}
