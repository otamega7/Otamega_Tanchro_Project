using UnityEngine;

public class TankBody_Camera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;              // 追従するプレイヤー
    public Vector3 targetOffset = new Vector3(0f, 1.5f, 0f); // 肩～頭くらいの高さ

    [Header("Orbit & Pitch")]
    public float mouseSensitivity = 150f; // マウス感度
    public float minPitch = -20f;         // 下向き限界
    public float maxPitch = 60f;          // 上向き限界

    [Header("Distance & Zoom")]
    public float distance = 4.5f;         // 基本距離
    public float minDistance = 2.0f;      // 最小ズーム
    public float maxDistance = 7.0f;      // 最大ズーム
    public float zoomSpeed = 5f;          // ホイールズーム速度

    [Header("Smoothing")]
    public float followSmooth = 0.00f;    // 位置スムーズ
    public float rotateSmooth = 0.00f;    // 回転スムーズ

    [Header("Collision")]
    public LayerMask obstacleLayers;      // 壁など
    public float collisionRadius = 0.2f;  // カメラ当たり判定
    public float collisionBuffer = 0.2f;  // 壁から少し離す

    float yaw;    // 水平角
    float pitch;  // 垂直角
    Vector3 camVelocity;       // SmoothDamp用
    Vector3 currentLookDir;    // 回転補間用

    void Start()
    {
        if (target == null)
        {
            Debug.LogWarning("[ThirdPersonCamera] Targetが未設定です。プレイヤーのTransformを割り当ててください。");
            enabled = false;
            return;
        }

        // 初期角度を現在向いている方向から計算
        Vector3 forward = target.forward;
        yaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
        pitch = 15f; // 俯瞰気味の初期角度
        currentLookDir = transform.forward;
        Cursor.lockState = CursorLockMode.None; // 必要に応じて.Lockedに
        Cursor.visible = true;
    }

    void LateUpdate()
    {
        if (target == null) return;

        // --- 入力（マウス回転 & ホイールズーム） ---
        float mx = Input.GetAxis("Mouse X");
        float my = Input.GetAxis("Mouse Y");
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        yaw += mx * mouseSensitivity * Time.deltaTime;
        pitch -= my * mouseSensitivity * Time.deltaTime;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        distance -= scroll * zoomSpeed;
        distance = Mathf.Clamp(distance, minDistance, maxDistance);

        // --- 目標注視点（プレイヤーの少し上） ---
        Vector3 focus = target.position + targetOffset;

        // --- 望ましいカメラ位置（角度と距離から算出） ---
        Quaternion desiredRot = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 desiredDir = desiredRot * Vector3.back; // 後方
        Vector3 desiredPos = focus + desiredDir * distance;

        // --- 障害物回避（SphereCastで壁とめり込み防止） ---
        float adjustedDistance = distance;
        if (Physics.SphereCast(focus, collisionRadius, desiredDir, out RaycastHit hit, distance, obstacleLayers, QueryTriggerInteraction.Ignore))
        {
            adjustedDistance = Mathf.Max(hit.distance - collisionBuffer, minDistance);
            desiredPos = focus + desiredDir * adjustedDistance;
        }

        // --- スムーズに追従 ---
        Vector3 newPos = Vector3.SmoothDamp(transform.position, desiredPos, ref camVelocity, followSmooth);
        transform.position = newPos;

        // --- 注視方向もスムーズに ---
        Vector3 lookDir = (focus - transform.position).normalized;
        currentLookDir = Vector3.Slerp(currentLookDir, lookDir, 1f - Mathf.Exp(-Time.deltaTime / rotateSmooth));
        transform.rotation = Quaternion.LookRotation(currentLookDir, Vector3.up);
    }

    // エディタで軌跡を視覚化（任意）
    void OnDrawGizmosSelected()
    {
        if (target == null) return;
        Gizmos.color = Color.cyan;
        Vector3 focus = target.position + targetOffset;
        Gizmos.DrawWireSphere(focus, 0.05f);
    }
}
