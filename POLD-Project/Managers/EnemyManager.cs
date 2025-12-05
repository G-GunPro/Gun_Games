using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class EnemyManager : MonoBehaviour,IGameSystem
{
    [Header("Enemy")]
    // 스폰 장소
    private Transform[] EnemySpawnPoints;
    public Transform BossSpawnPoint;
    // Enemy 프리펩을 위한 레퍼런스
    public GameObject normalEnemyPrefab;
    public GameObject specialEnemyPrefab;
    public GameObject bossWave2;
    public GameObject bossWave3;
    public GameObject bossWave4;
    public GameObject bossWave5;

    private List<GameObject> activeEnemies = new List<GameObject>();
    private List<PlayerController> cachedPlayers = new List<PlayerController>();
    public int ActiveEnemyCount
    {
        get
        {
            // null 정리 후 개수 반환
            for (int i = activeEnemies.Count - 1; i >= 0; i--)
                if (activeEnemies[i] == null) activeEnemies.RemoveAt(i);
            return activeEnemies.Count;
        }
    }
    public void Init()
    {
        if (EnemySpawnPoints == null || EnemySpawnPoints.Length == 0)
        {
            var go = GameObject.Find("EnemySpawnPoint");
            if (go != null)
            {
                List<Transform> list = new List<Transform>();
                foreach(var t in go.GetComponentsInChildren<Transform>())
                {
                    if (t != go.transform)
                        list.Add(t);
                }
                EnemySpawnPoints = list.ToArray();
            }
            else
                Debug.LogError("[EnemyManager] EnemySpawnPoint 오브젝트를 찾을 수 없습니다.");
        }
        
    }
    public Transform GetPlayerTransform()
    {
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        return playerObj ? playerObj.transform : null;
    }
    
    public void RefreshPlayerList()
    {
        cachedPlayers.Clear();

        var players = FindObjectsOfType<PlayerController>();
        foreach (var p in players)
        {
            if (p == null) continue;
            var health = p.GetComponent<PlayerHealth>();
            if (health != null && !health.IsDead)
                cachedPlayers.Add(p);
        }
    }

    //플레이어 중에서 가장 가까운 사람 찾기 
    public Transform GetClosestPlayer(Vector3 fromPos)
    {
        RefreshPlayerList();

        var players = GameObject.FindGameObjectsWithTag("Player");
        
        Debug.Log("[EnemyManager] TAG 검색된 플레이어 수: " + players.Length);
    foreach (var p in players)
        Debug.Log("[EnemyManager] 발견된 Player: " + p.name + " | 태그=" + p.tag + " | 레이어=" + p.layer);
    
    
        float bestDist = Mathf.Infinity;
        Transform best = null;

        foreach (var p in players)
        {
            if (!p) continue;
            var health = p.GetComponent<PlayerHealth>();
            if (health != null && health.IsDead) continue; // 죽은 플레이어는 무시 

            float d = (p.transform.position - fromPos).sqrMagnitude;
            if (d < bestDist)
            {
                bestDist = d;
                best = p.transform;
            }
        }

        return best; 
    }
    public void ClearEnemies()
    {
        for (int i = activeEnemies.Count - 1; i >= 0; i--)
            if (activeEnemies[i] == null)
                activeEnemies.RemoveAt(i);
    }

    public void SpawnWaveEnemies(int wave)
    {
        ClearEnemies();

        int playerCount = PhotonNetwork.playerList.Length;
        int baseEnemy = GetBaseEnemyCountByPlayer(playerCount);
        int finalEnemyCount = ApplyWaveMultiplier(baseEnemy,wave);

        Debug.Log($"[EnemyManager] Wave {wave} 최종 적 수 : {finalEnemyCount}마리");

        SpawnBossIfNeeded(wave);

        StartCoroutine(SpawnNormalEnemies(finalEnemyCount, wave));

        // GameObject baseEnemy = (wave == 5) ? specialEnemyPrefab : normalEnemyPrefab;
        // if (baseEnemy == null)
        // {
        //     Debug.LogError($"[EnemyManager] Wave {wave}용 에너미 프리팹이 비어있습니다");
        //     return;
        // }

        // for (int i = 0; i < enemyCount && i < EnemySpawnPoints.Length; i++)
        // {
        //     Transform p = EnemySpawnPoints[i % EnemySpawnPoints.Length];
        //     GameObject enemy = PhotonNetwork.Instantiate(baseEnemy.name, p.position, p.rotation, 0);
        //     activeEnemies.Add(enemy);
        // }

        // GameObject bossPrefab = null;
        // switch (wave)
        // {
        //     case 2: bossPrefab = bossWave2; break;
        //     case 3: bossPrefab = bossWave3; break;
        //     case 4: bossPrefab = bossWave4; break;
        //     case 5: bossPrefab = bossWave5; break;
        // }

        // if (bossPrefab != null)
        // {
        //     Transform bp = EnemySpawnPoints[Random.Range(0, EnemySpawnPoints.Length)];
        //     GameObject boss = PhotonNetwork.Instantiate(bossPrefab.name, bp.position, bp.rotation, 0);
        //     activeEnemies.Add(boss);
        //     Debug.Log($"[EnemyManager] 보스 {bossPrefab.name} 생성 완료 (Wave {wave})");
        // }

        // Debug.Log($"[EnemyManager] Wave {wave} 생성 완료 : {activeEnemies.Count}마리");
    }

    IEnumerator SpawnNormalEnemies(int totalCount, int wave)
    {
        int spawnIndex=0;

        GameObject baseEnemy = (wave == 5) ? specialEnemyPrefab : normalEnemyPrefab;
        for(int i = 0; i < totalCount; i++)
        {
            Transform p=EnemySpawnPoints[spawnIndex % EnemySpawnPoints.Length];

            if(IsSpawnPointCrowded(p))
            {
                yield return new WaitForSeconds(5f);
                i--;
                continue;
            }

            GameObject enemy = PhotonNetwork.Instantiate(
                baseEnemy.name,
                p.position,
                p.rotation,
                0
            );

            activeEnemies.Add(enemy);

            spawnIndex++;

            yield return new WaitForSeconds(0.1f);
        }
    }

    bool IsSpawnPointCrowded(Transform point)
    {
        Collider[] cols=Physics.OverlapSphere(point.position, 2f);
        foreach(var col in cols)
        {
            if(col.CompareTag("Enemy"))
                return true;
        }
        return false;
    }

    void SpawnBossIfNeeded(int wave)
    {
        GameObject bossPrefab=null;

        switch (wave)
        {
            case 2:bossPrefab=bossWave2;break;
            case 3:bossPrefab=bossWave3;break;
            case 4:bossPrefab=bossWave4;break;
            case 5:bossPrefab=bossWave5;break;
        }
        if(bossPrefab ==null)return;

        if (BossSpawnPoint == null)
        {
            Debug.LogError("[EnemyManager] 보스 스폰포인트 없음");
            return;
        }

        GameObject boss=PhotonNetwork.Instantiate(
            bossPrefab.name,
            BossSpawnPoint.position,
            BossSpawnPoint.rotation,
            0
        );
        if(wave!=5)
        {
        activeEnemies.Add(boss);
        }
        Debug.Log($"[EnemyManager] 보스 생성 완료 : {bossPrefab.name}");

        var ctrl=boss.GetComponent<EnemyController>();
        var core=boss.GetComponent<EnemyCore>();
        if (ctrl != null && core != null && core.IsBoss && wave != 5)
        {
            var hud=FindObjectOfType<HUDController>();

            if (hud != null)
            {
                hud.ShowBossHP(true);
                StartCoroutine(TrackBossHpRoutine(hud,ctrl));
            }
        }
    }

    IEnumerator TrackBossHpRoutine(HUDController hud,EnemyController boss)
    {
        while (boss != null && !boss.IsDead)
        {
            hud.SetBossHp(boss.currentHp,boss.maxHp);
            yield return null;
        }
        hud.ShowBossHP(false);
    }
    public void RemoveEnemy(GameObject enemy)
    {
        if (activeEnemies.Contains(enemy))
        {
            activeEnemies.Remove(enemy);
        }
    }

    int GetBaseEnemyCountByPlayer(int playerCount)
    {
        int baseCount = 12 + playerCount;
        float bonusRate = playerCount * 0.10f;
        return Mathf.RoundToInt(baseCount * (1f + bonusRate));
    }

    int ApplyWaveMultiplier(int baseCount, int wave)
    {
        float multiplier = 1f + (wave - 1) * 0.20f;
        return Mathf.RoundToInt(baseCount * multiplier);
    }
    public void UpdateSystem()
    {
        // 적 전체 상태 체크
    }
    public void ReleaseSystem()
    {

    }
    
    // 테스트용 적 전멸 코드
    public void KillAllEnemiesDebug()
    {
        if(PhotonNetwork.connected && PhotonNetwork.inRoom && !PhotonNetwork.isMasterClient) return;

        EnemyController[] enemies = FindObjectsOfType<EnemyController>();

        int killCount = 0;
        foreach(var e in enemies)
        {
            if(e == null) continue;
            if(e.IsDead) continue;

            e.ApplyDamage_Authoritative(e.currentHp + 99999f);
            killCount++;
        }
        Debug.Log($"[EnemyManager] KillAllEnemiesDebug 호출: {killCount}마리 처리 시도");
    }
}
