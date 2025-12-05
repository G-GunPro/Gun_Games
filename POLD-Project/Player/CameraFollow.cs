using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("View Settings")]
    public float distance = 10f;     // 플레이어와 거리
    public float height = 8f;        // 카메라 높이
    public float sensitivity = 5f;   // 마우스 감도
    public float pitchMin = -20f;
    public float pitchMax = 60f;

    private float yaw;
    private float pitch;

    void Start()
    {
        //  혹시 타겟이 아직 없을 때 대비해서 자동 연결 시도
        StartCoroutine(Co_TryAutoConnect());
    }

    IEnumerator Co_TryAutoConnect()
    {
        // 플레이어 스폰 직후엔 아직 없을 수 있으니까 약간 기다렸다가 연결 시도
        yield return new WaitForSeconds(0.2f);
        TryReconnectTarget();
    }

    void LateUpdate()
    {
        //  target 없으면 계속 재시도 (로딩 타이밍 대비)
        if (target == null)
        {
            TryReconnectTarget();
            return;
        }

        // UI 열려있으면 입력 중지
        if (UIManager.IsUIOpen) return;

        
        if(UIManager.IsUIOpen || OptionsUIController.IsOptionsOpen)
        {
            return;
        }

        // 마우스 입력으로 회전 계산
        yaw += Input.GetAxis("Mouse X") * sensitivity;
        pitch -= Input.GetAxis("Mouse Y") * sensitivity;
        pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
        Vector3 offset = rotation * new Vector3(0, 0, -distance) + Vector3.up * height;

        // 최종 위치/회전 적용
        transform.position = target.position + offset;
        transform.LookAt(target.position + Vector3.up * 4f);
    }

    void Update()
    {
        if(target==null)return;

        if(Input.GetKeyDown(KeyCode.PageUp))
        SpectatorManager.Next();

        if(Input.GetKeyDown(KeyCode.PageDown))
        SpectatorManager.Prev();
    }
    // ================================================
    //  외부에서 플레이어를 직접 연결할 때 사용하는 함수
    // ================================================
    public void AttachTarget(Transform newTarget)
    {
        target = newTarget;
        if (target != null)
        {
            yaw = transform.eulerAngles.y;
            pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);
            Debug.Log($"[CameraFollow] Target attached: {target.name}");
        }
    }

    // ================================================
    // 📌 로컬 플레이어 자동 탐색 (PhotonView.isMine)
    // ================================================
    public void TryReconnectTarget()
    {
        if (target != null) return;

        var players = FindObjectsOfType<PlayerController>();
        foreach (var p in players)
        {
            var pv = p.GetComponent<PhotonView>();
            if (pv != null && pv.isMine)
            {
                AttachTarget(p.transform);
                Debug.Log("[CameraFollow] 로컬 플레이어 자동 연결 완료");
                break;
            }
        }
    }

    public float GetYaw() => yaw;

    public void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
