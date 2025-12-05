using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CatStats
{
    public float trust { get; private set; }          // 신뢰도(호감도)
    public float fullness { get; private set; }       // 포만감
    public float bowel{ get; private set; }           // 배변욕구
    public float cleanliness { get; private set; }    // 청결도
    public float happiness { get; private set; }      // 행복도(기분)
    public float energy { get; private set; }         // 에너지
    public float health { get; private set; }         // 건강

    private const float Min = 0f;
    private const float Max = 100f;

    public CatStats()
    {
        trust = 30f;
        fullness = 30f;
        bowel = 70f;
        cleanliness = 50f;
        happiness = 50f;
        energy = 100f;
        health = 100f;
    }

    // 스텟 최소,최대치는 밑의 함수로 제한
    private float ClampStat(float value) => Mathf.Clamp(value, Min, Max);

    // 스텟 증감 함수
    public void AddTrust(float amount) => trust = ClampStat(trust + amount);
    public void AddFullness(float amount) => fullness = ClampStat(fullness + amount);
    public void AddBowel(float amount) => bowel = ClampStat(bowel + amount);
    public void AddCleanliness(float amount) => cleanliness = ClampStat(cleanliness + amount);
    public void AddHappiness(float amount) => happiness = ClampStat(happiness + amount);
    public void AddEnergy(float amount) => energy = ClampStat(energy + amount);
    public void AddHealth(float amount) => health = ClampStat(health + amount);

    // 시간에 따른 스텟 변화
    public void Decay(float deltaTime)
    {
        AddFullness(-0.1f * deltaTime);
        AddEnergy(-0.1f * deltaTime);
        AddBowel(0.1f * deltaTime);
        
        StatLink(deltaTime);
    }

    private void StatLink(float deltaTime)
    {
        // 포만감 떨어질수록 행복도,건강도 하락
        if (fullness < 30f)
        {
            AddHappiness(-0.1f * deltaTime);
            AddHealth(-0.05f * deltaTime);
        }

        // 청결도 낮을수록 행복도, 신뢰도 하락
        if (cleanliness < 30f)
        {
            AddHappiness(-0.1f * deltaTime);
            AddTrust(-0.05f * deltaTime);
        }

        // 배변욕구 꽉차면 행복도, 건강도 하락
        if (bowel >= 100f)
        {
            AddHappiness(-0.1f * deltaTime);
            AddHealth(-0.05f * deltaTime);
        }

    }
}