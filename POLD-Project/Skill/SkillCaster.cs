using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkillCaster : MonoBehaviour
{
    public SkillEffectData skillData;
    private PhotonView pv;
    private Animator anim;

    // Start is called before the first frame update
    void Start()
    {
        pv = GetComponent<PhotonView>();
        anim = GetComponent<Animator>();
    }

    public void SkillFire()
    {
        if (pv.isMine)
            pv.RPC(nameof(RPC_PlaySkill), PhotonTargets.All);
    }

    [PunRPC]
    void RPC_PlaySkill()
    {
        SkillEffectSpawner.Instance.PlaySkill(skillData, this.transform);
    }

    
}
