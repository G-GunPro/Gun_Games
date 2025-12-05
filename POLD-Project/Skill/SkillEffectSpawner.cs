using System.Collections;
using UnityEngine;

public class SkillEffectSpawner : MonoBehaviour
{
    public static SkillEffectSpawner Instance;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void PlaySkill(SkillEffectData data, Transform root)
    {
        switch (data.spawnType)
        {
            case SkillSpawnType.Bone:
                SpawnOnBone(data, root);
                break;

            case SkillSpawnType.BothHands:
                SpawnBothHands(data, root);
                break;

            case SkillSpawnType.Ground:
                SpawnOnGround(data, root);
                break;

            case SkillSpawnType.Forward:
                SpawnForward(data, root);
                break;

            case SkillSpawnType.SelfCenter:
                SpawnSelfCenter(data, root);
                break;
            
            case SkillSpawnType.TripleGroundEffect:
                SpawnTripleGroundEffect(data, root);
                break;

            case SkillSpawnType.ShotgunRocks:
                SpawnShotgunRocks(data, root);
                break;

            case SkillSpawnType.HealingArea:
                SpawnHealingArea(data, root);
                break;
        }
    }

    // ==========================
    // 1) 본에서 생성 (Bone)
    // ==========================
    void SpawnOnBone(SkillEffectData data, Transform root)
    {
        Transform bone = FindBone(root, data.boneName);
        if (!bone) bone = root;

        foreach (var prefab in data.particlePrefabs)
        {
            var vfx = Instantiate(prefab, bone.position, bone.rotation);
            vfx.transform.SetParent(bone);
            Destroy(vfx, data.duration);
        }

    }

    // ==========================
    // 2) 양손 생성 (BothHands)
    // ==========================
    void SpawnBothHands(SkillEffectData data, Transform root)
    {
        Transform right = FindBone(root, data.rightHandBoneName);
        Transform left = FindBone(root, data.leftHandBoneName);

        foreach (var prefab in data.particlePrefabs)
        {
            if (right)
            {
                var vfxR = Instantiate(prefab, right.position, right.rotation);
                vfxR.transform.SetParent(right);
                Destroy(vfxR, data.duration);
            }

            if (left)
            {
                var vfxL = Instantiate(prefab, left.position, left.rotation);
                vfxL.transform.SetParent(left);
                Destroy(vfxL, data.duration);
            }
        }

        var pv=root.GetComponent<PhotonView>();
        if (pv != null && pv.isMine)
        {
            var atk=root.GetComponent<PlayerAttack>();
            if (atk != null)
            {
                float buffMul=1.5f;
                float buffDur=30f;

                atk.StartAttackBuff(buffMul,buffDur);
                
            }
        }
    }

    // ==========================
    // 3) 바닥 생성 (Ground)
    // ==========================
    void SpawnOnGround(SkillEffectData data, Transform root)
    {
        Vector3 origin = root.position+root.forward*1.0f + Vector3.up * 1.0f;
        RaycastHit hit;

        Vector3 spawnPos;

        int groundMask = LayerMask.GetMask("Ground");

        if (Physics.Raycast(origin, Vector3.down, out hit, 10f, groundMask))
        {
            spawnPos = hit.point + Vector3.up * 0.5f;
        }
        else
        {
            spawnPos = root.position + Vector3.up * 0.5f;
        }

        Quaternion rot=Quaternion.LookRotation(root.forward,Vector3.up) * Quaternion.Euler(0, -90f, 0);

        foreach (var prefab in data.particlePrefabs)
        {
            var vfx = Instantiate(prefab, spawnPos, rot);
            Destroy(vfx, data.duration);
        }

    }

    // ==========================
    // 4) 던지기 (Forward)
    // ==========================
    void SpawnForward(SkillEffectData data, Transform root)
    {
        foreach (var prefab in data.particlePrefabs)
        {
            var vfx = Instantiate(prefab, root.position + root.forward * 1f, root.rotation);
            vfx.GetComponent<Rigidbody>()?.AddForce(root.forward * 10f, ForceMode.VelocityChange);
            Destroy(vfx, data.duration);
        }

    }

    // ==========================
    // 5) 자기 중심 (SelfCenter)
    // ==========================
    void SpawnSelfCenter(SkillEffectData data, Transform root)
    {
        Vector3 pos = root.position;
        foreach (var prefab in data.particlePrefabs)
        {
            var vfx = Instantiate(prefab, pos, Quaternion.identity);
            Destroy(vfx, data.duration);
        }

    }

    void SpawnTripleGroundEffect(SkillEffectData data, Transform root)
    {
        // 바닥 찾기
        Vector3 start = root.position + root.forward * 1f + Vector3.up * 1f;
        RaycastHit hit;
        Vector3 spawnPos = root.position;

        if (Physics.Raycast(start, Vector3.down, out hit, 5f, LayerMask.GetMask("Ground")))
            spawnPos = hit.point;

        // 캐릭터 정면 기본 회전 + 파티클 보정(-90°)
        Quaternion baseRot =
            Quaternion.LookRotation(root.forward, Vector3.up) *
            Quaternion.Euler(0, -90f, 0);

        // 3갈래 회전값
        Quaternion rotMid = baseRot;
        Quaternion rotLeft = baseRot * Quaternion.Euler(0, -30f, 0);
        Quaternion rotRight = baseRot * Quaternion.Euler(0, +30f, 0);

        // 3개 생성
        SpawnFX(data, spawnPos, rotLeft);
        SpawnFX(data, spawnPos, rotMid);
        SpawnFX(data, spawnPos, rotRight);
    }


    void SpawnFX(SkillEffectData data, Vector3 pos, Quaternion rot)
    {
        foreach (var prefab in data.particlePrefabs)
        {
            var fx = Instantiate(prefab, pos, rot);

            // FTME 파티클 특성 때문에 localRotation 강제
            fx.transform.localRotation = rot;

            Destroy(fx, data.duration);
        }
    }

    void SpawnShotgunRocks(SkillEffectData data,Transform root)
    {   
        Transform hand=FindBone(root,data.rightHandBoneName);
        int count = data.projectileCount;
        float spread=data.spreadAngle;
        float speed=data.projectileSpeed;

        for(int i = 0; i < count; i++)
        {
             float angle=Random.Range(-spread,spread);
             Quaternion rot=Quaternion.LookRotation(root.forward,Vector3.up)*
             Quaternion.Euler(0,angle,0);

            var rock=Instantiate(data.particlePrefabs[0],hand.position,rot);

            var rb=rock.GetComponent<Rigidbody>();
            if(rb)rb.velocity=rock.transform.forward*speed;

            Destroy(rock,data.duration);
        }
    }

    // ==========================
    // 본 찾기
    // ==========================
    Transform FindBone(Transform root, string boneName)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>())
            if (t.name == boneName)
                return t;

        return null;
    }

    void SpawnHealingArea(SkillEffectData data,Transform root)
    {
        Vector3 start=root.position+Vector3.up*1f;
        Vector3 spawnPos=root.position;

        if(Physics.Raycast(start,Vector3.down,out RaycastHit hit,3f,LayerMask.GetMask("Ground")))
        spawnPos=hit.point;

        foreach(var prefab in data.particlePrefabs)
        {
            var fx=Instantiate(prefab,spawnPos,Quaternion.identity);
            Destroy(fx,data.duration);
        }

        StartCoroutine(HealingRoutine(root,data));
    }

    IEnumerator HealingRoutine(Transform caster,SkillEffectData data)
    {
        var stats=caster.GetComponent<PlayerStats>();
        if (stats == null)
        {
            Debug.LogWarning("[Heal] PlayerStats 없음");
            yield break;
        }

        float totalHeal=stats.Attack;
        float duration=data.healDuration;
        float range=data.healRange;
        float tick=1f;
        int ticks=Mathf.RoundToInt(duration/tick);
        float healPerTick=totalHeal/ticks;

        float timer=0f;

        Debug.Log($"[Heal] 광역 힐 시작 : 총 {totalHeal},tick당 {healPerTick}");

        while (timer < duration)
        {
            if (PhotonNetwork.isMasterClient)
            {
                foreach(var pc in FindObjectsOfType<PlayerController>())
                {
                    float dist=Vector3.Distance(pc.transform.position,caster.position);
                    if (dist <= range)
                    {
                        var hp=pc.GetComponent<PlayerHealth>();
                        if(hp!=null)
                        hp.Heal((int)healPerTick);
                    }
                }
            }
            timer+=tick;
            yield return new WaitForSeconds(tick);
        }
        Debug.Log("[Heal] 광역 힐 종료");
    }
}
