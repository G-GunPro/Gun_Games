using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SkillSpawnType
{
    Bone,
    BothHands,
    Ground,
    Forward,
    SelfCenter,
    TripleGroundEffect,
    ShotgunRocks,
    HealingArea

}
[CreateAssetMenu(fileName = "SkillEffect", menuName = "Game/SkillEffectData")]
public class SkillEffectData : ScriptableObject
{
    [Header("이펙트 생성 타입")]
    public SkillSpawnType spawnType = SkillSpawnType.Bone;

    [Header("본 이름(Bone 타입일 때만)")]
    public string boneName;

    [Header("본 이름(Both 타입일 때만)")]
    public string leftHandBoneName;
    public string rightHandBoneName;

    [Header("파티클 프리팹")]
    public GameObject[] particlePrefabs;

    [Header("지속시간")]
    public float duration = 1f;

    [Header("카메라 흔들림, 사운드 등")]
    public AudioClip sfx;

    public int projectileCount = 12;
    public float spreadAngle = 25f;
    public float projectileSpeed = 20f;

    [Header("힐링 옵션")]
    public bool isHealingSkill = false;
    public float healDuration = 4f;
    public float healRange = 13f;
    
}
