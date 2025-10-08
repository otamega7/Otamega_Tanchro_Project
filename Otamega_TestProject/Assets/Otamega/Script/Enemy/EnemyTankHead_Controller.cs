using UnityEngine;

/// <summary>
/// 敵タンクの砲塔コントローラ（肩車）
/// - 非親子のまま Body に追随（位置Yオフセット）
/// - Yaw（砲塔）回頭：Bodyの前方またはターゲット方向へ
/// - Pitch（砲身）上下：barrelPivot のローカルXで制御（安定計算式）
/// - 上下が逆のモデルに対応する invertPitch トグル内蔵
/// </summary>
public class EnemyTankHead_Controller : MonoBehaviour
{
    public enum AimMode { FollowBodyForward, TrackTarget }

    [Header("Follow (肩車)")]
    public Transform tankBody;           // 追随元（Rigidbody付きのBody推奨）
    public float headHeight = 2.0f;      // 肩車オフセット（Y）
    public bool physicsFollow = false;   // BodyがFixedUpdate駆動ならtrue

    [Header("Aiming")]
    public AimMode aimMode = AimMode.FollowBodyForward;
    public Transform target;             // TrackTarget時の相手
    public float aimYOffset = 0f;        // 目標の狙い高さ（胸/頭など）

    [Header("Yaw (砲塔回頭)")]
    public float yawSpeed = 120f;        // 度/秒
    public float yawDeadZone = 1f;       // 小角無視（微振れ防止）

    [Header("Pitch (砲身上下)")]
    public Transform barrelPivot;        // ローカルXで上下
    public float pitchSpeed = 80f;       // 度/秒
    public float minPitch = -10f;        // 下向き(負) 限界
    public float maxPitch = 25f;        // 上向き(正) 限界
    public float pitchDeadZone = 0.5f;   // 微小変化無視
    public bool invertPitch = false;    // ★上下反転：モデル都合で逆ならONに

    float _currentPitch; // ローカルX（度）

    void Awake()
    {
        if (barrelPivot)
            _currentPitch = NormalizeDeg(barrelPivot.localEulerAngles.x);
    }

    void LateUpdate()
    {
        if (physicsFollow) return;
        FollowBody();
        AimUpdate(Time.deltaTime);
    }

    void FixedUpdate()
    {
        if (!physicsFollow) return;
        FollowBody();
        AimUpdate(Time.fixedDeltaTime);
    }

    // ─────────────────────────────────────────────

    public void SetTarget(Transform t)
    {
        target = t;
        aimMode = t ? AimMode.TrackTarget : AimMode.FollowBodyForward;
    }

    void FollowBody()
    {
        if (!tankBody) return;
        Vector3 p = tankBody.position;
        p.y += headHeight;
        transform.position = p;
        // 回頭は AimUpdate() で行う
    }

    void AimUpdate(float dt)
    {
        if (!tankBody || !barrelPivot) return;

        // 目標点の決定
        Vector3 aimPoint;
        if (aimMode == AimMode.TrackTarget && target)
            aimPoint = target.position + Vector3.up * aimYOffset;
        else
            aimPoint = transform.position + tankBody.forward * 100f; // 車体前方の仮想点

        // ── Yaw：水平回頭（XZ投影）
        Vector3 toAimXZ = aimPoint - transform.position; toAimXZ.y = 0f;
        if (toAimXZ.sqrMagnitude > 1e-6f)
        {
            float signedYaw = Vector3.SignedAngle(transform.forward, toAimXZ.normalized, Vector3.up);
            if (Mathf.Abs(signedYaw) > yawDeadZone)
            {
                float step = Mathf.Sign(signedYaw) * yawSpeed * dt;
                if (Mathf.Abs(step) > Mathf.Abs(signedYaw)) step = signedYaw;
                transform.Rotate(Vector3.up, step, Space.World);
            }
        }

        // ── Pitch：安定計算（前方成分は前向きベクトルとの内積で取得）
        Vector3 aimVec = (aimPoint - barrelPivot.position);
        float forwardDist = Mathf.Abs(Vector3.Dot(aimVec, transform.forward)); // 前後成分の絶対値
        float height = Vector3.Dot(aimVec, transform.up);                 // 上下成分
        float desiredPitch = Mathf.Rad2Deg * Mathf.Atan2(height, Mathf.Max(0.001f, forwardDist));


        // 前方成分が極小でも暴れないように下限を設けAbsで安定化
        float desired = Mathf.Rad2Deg * Mathf.Atan2(
            height,
            Mathf.Max(0.001f, Mathf.Abs(forwardDist))
        );

        // 上下反転に対応
        float lo = minPitch, hi = maxPitch;
        if (invertPitch)
        {
            desired = -desired;
            float tmp = lo; lo = -hi; hi = -tmp; // 上限下限も反転
        }

        desiredPitch = Mathf.Clamp(desiredPitch, minPitch, maxPitch);

        float delta = Mathf.DeltaAngle(_currentPitch, desired);
        if (Mathf.Abs(delta) > pitchDeadZone)
        {
            float step = Mathf.Sign(delta) * pitchSpeed * dt;
            if (Mathf.Abs(step) > Mathf.Abs(delta)) step = delta;
            _currentPitch = Wrap180(_currentPitch + step);

            Vector3 e = barrelPivot.localEulerAngles;
            e.x = Wrap360(_currentPitch);
            e.y = 0f; e.z = 0f;
            barrelPivot.localEulerAngles = e;
        }
    }

    // ── helpers
    static float NormalizeDeg(float deg) => Mathf.Repeat(deg + 180f, 360f) - 180f; // [-180,180)
    static float Wrap180(float deg) => NormalizeDeg(deg);
    static float Wrap360(float deg) => (deg % 360f + 360f) % 360f;

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // 砲身forward（赤）とYaw目標（黄）
        if (barrelPivot)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(barrelPivot.position, barrelPivot.forward * 2.5f);
        }
        if (tankBody)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position + Vector3.up * 0.2f, tankBody.forward * 3f);
        }
        if (target)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(barrelPivot ? barrelPivot.position : transform.position,
                            target.position + Vector3.up * aimYOffset);
        }
    }
#endif
}
