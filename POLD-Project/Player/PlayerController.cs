using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;

public enum SkillType
{
    Attack,Armored,Beast,Cart,Colossus,Female,Jaw,WarHammer
}
public class PlayerController : MonoBehaviour
{
    public string nickname;
    public bool isLocalPlayer;

    private PlayerMotor motor;
    private PlayerAttack attack;
    private PlayerStats stats;
    private PlayerHealth health;
    private Animator anim;
    private PhotonView pv;
    private Transform myTr;
    private Vector3 networkPos;
    private Quaternion networkRot;
    private CharacterController cc;
    private List<ItemType> inventory = new();
    HUDController hud;

    //[Header("Stats")]

    void Awake()
    {
        myTr = GetComponent<Transform>();
        pv = GetComponent<PhotonView>();
        if (pv == null)
        {
            Debug.LogError("[PlayerController] PhotonView 없음, 네트워크 비활성");
            enabled = false;
            return;
        }

        if (pv.viewID == 0 && !PhotonNetwork.offlineMode)
        {
            Debug.LogWarning($"[PlayerController] viewID=0 인 인스턴스 발견 → 네트워크 기능 비활성 ({gameObject.name})");
            enabled = false;
            return;
        }
        if (pv.isMine)
        {
            HUDController hud = FindObjectOfType<HUDController>();
            hud.BindLocalPlayer(GetComponent<PlayerHealth>());
        }
        stats = GetComponent<PlayerStats>();
        motor = GetComponent<PlayerMotor>();
        attack = GetComponent<PlayerAttack>();
        health = GetComponent<PlayerHealth>();
        anim = GetComponent<Animator>();
        cc = GetComponent<CharacterController>();
        isLocalPlayer = pv.isMine;
        motor.isLocal = pv.isMine;
        pv.ObservedComponents[0] = this;
        pv.synchronization = ViewSynchronization.UnreliableOnChange;
        if (pv.isMine)
        {
            nickname = PhotonNetwork.player.NickName;
            gameObject.name = $"LocalPlayer_{nickname}";

            var cam = FindObjectOfType<CameraFollow>();
            if (cam != null)
            {
                cam.AttachTarget(transform);
            }
        }
        else
        {
            gameObject.name = $"RemotePlayer_{pv.owner.NickName}";
        }
        networkPos = myTr.position;
        networkRot = myTr.rotation;

        nickname=pv.owner.NickName;
    }
    void Start()
    {
        

        stats.OnStatsChanged += ApplyStats;
    }
    void Update()
    {
        if (UIManager.IsUIOpen) return;
        if (!pv.isMine) return;
        if (attack != null && attack.IsUsingRootMotionSkill)
            anim.applyRootMotion = true;
        else
            anim.applyRootMotion = false;

        if (pv.isMine)
        {
            motor.HandleInput();
            attack.HandleInput();

        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, networkPos, Time.deltaTime * 15f);
            transform.rotation = Quaternion.Lerp(transform.rotation, networkRot, Time.deltaTime * 10f);

        }
    }
    void ApplyStats()
    {
        if (stats == null) stats = GetComponent<PlayerStats>();
        if (motor == null) motor = GetComponent<PlayerMotor>();
        if (attack == null) attack = GetComponent<PlayerAttack>();
        if (health == null) health = GetComponent<PlayerHealth>();

        // 이동속도 반영
        motor.moveSpeed = stats.MoveSpeed;

        // 방어력 반영
        health.defensePower = stats.Defense;

        // 체력 반영
        health.maxHP = stats.MaxHP;

        if (health.currentHP > health.maxHP)
        health.currentHP = health.maxHP;

        health.UpdateHUD();
        // 공격력은 이미 Damage RPC에서 반영됨
    }
    void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        PhotonView pv = info.photonView ?? GetComponent<PhotonView>();
        if (pv == null)
        {
            Debug.LogError("[PlayerController] info.photonView is null!");
            return;
        }
        //info.sender.TagObject = this.GameObject;
        // 네트워크 플레이어 생성시 전달 인자 확인
        object[] data = pv.instantiationData;
        if (data == null || data.Length == 0)
        {
            Debug.LogWarning("[PlayerController] instantiationData is null or empty");
            return;
        }

                //🌟
        var headHudCanvas = GetComponentInChildren<Canvas>(true);
        if(headHudCanvas != null)
        {
            var root = headHudCanvas.transform.parent;
            if(root != null && !root.gameObject.activeSelf)
            {
                root.gameObject.SetActive(true);
            }
            else
            {
                headHudCanvas.gameObject.SetActive(true);
            }
            Debug.Log("[PlayerController] Head HUD 활성화 완료");
        }
        else
        {
            Debug.LogWarning("[PlayerController] Head HUD Canvas를 찾을 수 없음");
        }

        Debug.Log($"[PlayerController] Player instantiated with data[0]={data[0]}");
    }

    void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        bool isRootMotionNow = attack != null && attack.IsUsingRootMotionSkill;
        if (stream.isWriting)
        {
            if (!isRootMotionNow)
            {
                stream.SendNext(transform.position);
                stream.SendNext(transform.rotation);
            }
            // else
            // {
            //     stream.SendNext(Vector3.zero);
            //     stream.SendNext(Quaternion.identity);
            // }
            stream.SendNext(motor.CurrentSpeed);
            stream.SendNext(motor.PlayRate);
            stream.SendNext(motor.isBack);
            stream.SendNext(cc.isGrounded);
        }
        else
        {
            networkPos = (Vector3)stream.ReceiveNext();
            networkRot = (Quaternion)stream.ReceiveNext();
            float remoteSpeed = (float)stream.ReceiveNext();
            float remoteRate = (float)stream.ReceiveNext();
            bool remoteIsBack = (bool)stream.ReceiveNext();
            bool remoteIsGrounded = (bool)stream.ReceiveNext();
            if (!pv.isMine)
            {
                if (attack == null || attack.IsUsingRootMotionSkill == false)
                {
                    transform.position = Vector3.Lerp(transform.position, networkPos, Time.deltaTime * 10f);
                    transform.rotation = Quaternion.Lerp(transform.rotation, networkRot, Time.deltaTime * 10f);
                }
                anim.SetFloat("Speed", remoteSpeed);
                anim.SetFloat("PlayRate", remoteRate);
                anim.SetBool("isBack", remoteIsBack);
                anim.SetBool("Grounded", remoteIsGrounded);
            }
        }
    }

    public void ApplyTitanPower(string titanName)
    {
        Debug.Log($"{nickname}이 {titanName} 거인 선택");
    }

    public void AddItem(ItemType type)
    {
        inventory.Add(type);
    }


    

    public void SendAttackRPC(string trigger)
    {
        if (!pv.isMine) return;
        pv.RPC("RPC_PlayAttack", PhotonTargets.Others, trigger);
        Debug.Log($"[RPC] 공격 전송 : {trigger}");
    }

    //🌟 Hit RPC 추가
    public void SendHitRPC()
    {
        if(!pv.isMine) return; //내캐릭터만 PRC 
        pv.RPC("RPC_PlayHit", PhotonTargets.All); // 모두에게 hit 지시 
    }


    //실제 hit 재생 
    [PunRPC]
    void RPC_PlayHit()
    {
        if (motor == null)
        {
            motor = GetComponent<PlayerMotor>();
        }

        if (motor != null)
        {
            motor.PlayHitMotion(); 
        }
    }

    [PunRPC]
    public void RPC_PlayAttack(string trigger)
    {
        EnsureAnimator();
        if (!anim) return;

        Debug.Log($"[RPC] 공격 수신 : {trigger}");

        switch (trigger)
        {
            case "Attack1":
                anim.CrossFade("Player.Combo.Attack1", 0.05f);
                break;
            case "Attack2":
                anim.CrossFade("Player.Combo.Attack2", 0.05f);
                break;
            case "Skill":
                anim.CrossFade("Skill", 0.05f);
                break;
            case "Jump":
                anim.CrossFade("Jump", 0.05f);
                break;
            default:
                anim.SetTrigger(trigger);
                break;
        }
    }

    void EnsureAnimator()
    {
        if (anim == null)
            anim = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
    }

    public void OnAnimatorMove()
    {

        if (!pv.isMine) return;
        Vector3 dp = anim.deltaPosition;
        Quaternion dr = anim.deltaRotation;

        if (attack != null && attack.IsUsingRootMotionSkill)
        {
            transform.position += dp;
            transform.rotation *= dr;
        }

        pv.RPC("RPC_ApplyRemoteRootMotion", PhotonTargets.Others, dp, dr);
    }

    [PunRPC]
    void RPC_ApplyRemoteRootMotion(Vector3 dp,Quaternion dr)
    {
        if (pv.isMine) return;

        transform.position += dp;
        transform.rotation *= dr;
    }

    public void SFX_PlayerHit()
    {
        SFXManager.Instance.PlaySFX("Hit1");
    }
}
