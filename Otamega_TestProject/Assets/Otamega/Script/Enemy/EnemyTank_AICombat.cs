using UnityEngine;
using System.Collections;

[RequireComponent(typeof(EnemyTankHead_Controller))]
public class EnemyTank_AICombat : MonoBehaviour
{
    [Header("Refs")]
    public EnemyTankHead_Controller headController;  // 砲塔Yaw/Pitch制御
    public Transform barrelPivot;                    // ピッチ軸
    public Transform barrelBore;                     // 砲身の“正方向”基準（+Z）
    public Transform firePoint;                      // 発射口（+Z 発射）
    public Rigidbody projectilePrefab;               // 砲弾
    public Rigidbody inheritFromBody;                // 走行速度を弾に継承（任意）

    [Header("Target")]
    public Transform target;
    public string targetTag = "Player";

    [Header("Detect / Reacquire")]
    public float detectRadiusEnter = 35f;
    public float detectRadiusExit = 40f;
    [Range(0f, 180f)] public float fovEnter = 120f;
    [Range(0f, 180f)] public float fovExit = 140f;
    public float lostMemoryTime = 2.0f;
    public float reacquireInterval = 0.25f;
    public LayerMask targetMask = ~0;    // プレイヤーのレイヤ
    public LayerMask obstacleMask = ~0;  // 壁/地形のレイヤ

    [Header("Close-Range Override")]
    public float innerStickRadius = 6f;
    public float innerNoLoSDist = 3f;

    [Header("Aim / Fire")]
    public float maxFireDistance = 40f;
    public float aimToleranceDeg = 6f;
    public float aimHoldTime = 0.30f;
    public float reloadTime = 1.2f;
    public int burstCount = 1;
    public float burstInterval = 0.15f;
    public float muzzleVelocity = 60f;
    public float aimYOffset = 0.9f;

    [Header("Effects")]
    public string fireFxKey = "MuzzleFlash";  // 発射時に出すエフェクト名（EfManagerで登録済み）
    public string fireSeKey = "TankFire";     // 発射時に鳴らすSE名（SEManagerで登録済み）


    [Header("When Lost")]
    public float pitchRecoverSpeed = 120f;

    [Header("Clear Shot (遮蔽チェック)")]
    public bool requireClearLoS = true; // ★遮蔽物があれば射撃“中止”
    public float losExtra = 0.2f;        // Ray長の余裕

    [Header("Debug / Gizmos")]
    public bool debugShowDetectGizmos = true;   // ★索敵範囲表示
    public bool debugDrawRays = true;           // 視線Rayを色分け表示
    public bool debugLog = false;
    public bool invertBarrelForward = false;    // forwardが逆向きならON

    enum State { Idle, Tracking, WindUp, Firing, Reload }
    State _state = State.Idle;

    float _aimTimer, _lostTimer, _reacquireTimer;

    void Reset() { headController = GetComponent<EnemyTankHead_Controller>(); }

    void OnEnable()
    {
        StopAllCoroutines();
        StartCoroutine(StateLoop());
    }

    IEnumerator StateLoop()
    {
        while (enabled)
        {
            // 目標未設定ならTagで拾う
            if (!target && !string.IsNullOrEmpty(targetTag))
            {
                var go = GameObject.FindGameObjectWithTag(targetTag);
                if (go) target = go.transform;
            }

            // 検知/再取得
            bool detected = ReacquireOrStick();

            // 砲塔へターゲットを渡す（猶予中も追尾）
            headController.SetTarget((detected || _lostTimer > 0f) ? target : null);

            // 再発見 or 猶予中は Idle/Reload 固着を解除（パッチ1）
            bool stickOrDetected = detected || _lostTimer > 0f;
            if (stickOrDetected && (_state == State.Idle || _state == State.Reload))
            {
                _aimTimer = 0f;
                _state = State.Tracking;
            }

            switch (_state)
            {
                case State.Idle:
                    RecoverPitchToZero(Time.deltaTime);
                    if (stickOrDetected) _state = State.Tracking;
                    break;

                case State.Tracking:
                    {
                        if (!stickOrDetected) { _state = State.Idle; _aimTimer = 0f; break; }

                        bool aimed = IsAimedAtTarget();
                        bool clear = !requireClearLoS || HasClearShot();  // ★遮蔽チェック

                        if (aimed && clear)
                        {
                            _aimTimer += Time.deltaTime;
                            _state = (_aimTimer >= aimHoldTime) ? State.Firing : State.WindUp;
                        }
                        else _aimTimer = 0f; // 遮蔽やブレでリセット
                        break;
                    }

                case State.WindUp:
                    {
                        if (!stickOrDetected) { _state = State.Idle; _aimTimer = 0f; break; }

                        bool aimed = IsAimedAtTarget();
                        bool clear = !requireClearLoS || HasClearShot();

                        if (aimed && clear)
                        {
                            _aimTimer += Time.deltaTime;
                            if (_aimTimer >= aimHoldTime) { _aimTimer = 0f; _state = State.Firing; }
                        }
                        else { _aimTimer = 0f; _state = State.Tracking; }
                        break;
                    }

                case State.Firing:
                    yield return StartCoroutine(FireBurst_WithClearShotGate());
                    _state = State.Reload;
                    break;

                case State.Reload:
                    RecoverPitchToZero(Time.deltaTime);
                    yield return new WaitForSeconds(reloadTime);
                    _state = (stickOrDetected) ? State.Tracking : State.Idle;
                    break;
            }

            if (_lostTimer > 0f) _lostTimer -= Time.deltaTime;
            yield return null;
        }
    }

    // ───────── Detect / Reacquire ─────────
    bool ReacquireOrStick()
    {
        if (!target) return false;

        _reacquireTimer -= Time.deltaTime;
        if (_reacquireTimer > 0f)
            return _lostTimer > 0f || LastDetectByGeometry(relaxed: true);

        _reacquireTimer = reacquireInterval;

        bool seen = LastDetectByGeometry(relaxed: false);
        if (seen) { _lostTimer = lostMemoryTime; return true; }

        // 近距離優先
        float dist = Vector3.Distance(GetOrigin(), TargetAimPos());
        if (dist <= innerStickRadius)
        {
            if (dist <= innerNoLoSDist) { _lostTimer = lostMemoryTime; return true; }
            if (HasLineOfSight(TargetAimPos(), dist + losExtra)) { _lostTimer = lostMemoryTime; return true; }
        }
        return false;
    }

    bool LastDetectByGeometry(bool relaxed)
    {
        if (!target) return false;

        Vector3 origin = GetOrigin();
        Vector3 aimPos = TargetAimPos();
        float dist = Vector3.Distance(origin, aimPos);

        float radius = relaxed ? detectRadiusExit : detectRadiusEnter;
        if (dist > radius) return false;

        // 水平方向FOV（XZ）。XZ距離が極小ならFOVスキップ
        Vector3 fwd = transform.forward; fwd.y = 0f; fwd.Normalize();
        Vector3 toXZ = (aimPos - transform.position); toXZ.y = 0f;
        if (toXZ.sqrMagnitude > 0.25f)
        {
            float fov = relaxed ? fovExit : fovEnter;
            if (Vector3.Angle(fwd, toXZ.normalized) > fov * 0.5f) return false;
        }

        // LoS（実距離）
        return HasLineOfSight(aimPos, Mathf.Min(dist + losExtra, Mathf.Max(detectRadiusExit, maxFireDistance)));
    }

    bool HasLineOfSight(Vector3 aimPos, float maxDist)
    {
        Vector3 origin = GetOrigin();
        Vector3 dir = (aimPos - origin);
        float len = dir.magnitude;
        if (len < 0.001f) return true;
        dir /= len;

        int mask = targetMask | obstacleMask;
        if (Physics.Raycast(origin, dir, out RaycastHit hit, Mathf.Min(maxDist, len + losExtra), mask, QueryTriggerInteraction.Ignore))
        {
            bool isTarget = ((1 << hit.collider.gameObject.layer) & targetMask) != 0;
            return isTarget; // 最初に当たったのがターゲットなら見えている
        }
        return false;
    }

    Vector3 GetOrigin() => firePoint ? firePoint.position : transform.position;
    Vector3 TargetAimPos() => target ? target.position + Vector3.up * aimYOffset : transform.position;

    // ───────── Aim / Fire ─────────
    bool IsAimedAtTarget()
    {
        if (!target) return false;

        Transform basis = barrelBore ? barrelBore : barrelPivot;
        if (!basis) return false;

        Vector3 aimPos = TargetAimPos();
        Vector3 aimDir = (aimPos - basis.position).normalized;

        Vector3 fwd = invertBarrelForward ? -basis.forward : basis.forward;
        float ang = Vector3.Angle(fwd, aimDir);

        float dist = Vector3.Distance(basis.position, aimPos);
        if (debugLog) Debug.Log($"[AICombat] Aim ang={ang:F1} tol={aimToleranceDeg} dist={dist:F1}");

        return ang <= aimToleranceDeg && dist <= maxFireDistance;
    }

    bool HasClearShot()
    {
        if (!requireClearLoS || !target) return true;

        Vector3 origin = GetOrigin();
        Vector3 aimPos = TargetAimPos();

        int mask = targetMask | obstacleMask;
        Vector3 dir = (aimPos - origin);
        float len = dir.magnitude;
        if (len < 0.001f) return true;
        dir /= len;

        bool clear = false;
        if (Physics.Raycast(origin, dir, out RaycastHit hit, len + losExtra, mask, QueryTriggerInteraction.Ignore))
        {
            clear = ((1 << hit.collider.gameObject.layer) & targetMask) != 0;
        }

        if (debugDrawRays)
            Debug.DrawRay(origin, dir * (len + 0.1f), clear ? Color.green : new Color(1f, 0.6f, 0f), 0f, false);

        if (debugLog && !clear) Debug.Log("[AICombat] Blocked by obstacle → hold fire");
        return clear;
    }

    IEnumerator FireBurst_WithClearShotGate()
    {
        if (!projectilePrefab || !firePoint) yield break;

        int shots = Mathf.Max(1, burstCount);
        for (int i = 0; i < shots; i++)
        {
            // 発射直前にも遮蔽チェック（壁撃ち防止）
            if (requireClearLoS && !HasClearShot()) break;

            // 2発目以降は照準再確認
            if (i > 0 && !IsAimedAtTarget()) break;

            Quaternion rot = Quaternion.LookRotation(firePoint.forward, Vector3.up);
            var proj = Instantiate(projectilePrefab, firePoint.position, rot);

            Vector3 v = firePoint.forward * muzzleVelocity;
            if (inheritFromBody) v += inheritFromBody.linearVelocity;
            proj.linearVelocity = v;
            proj.interpolation = RigidbodyInterpolation.Interpolate;
            proj.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            if (!string.IsNullOrEmpty(fireFxKey) && EfManager.I != null)
                EfManager.I.Spawn(fireFxKey, firePoint.position, rot, follow: firePoint, life: 0.12f);
            if (!string.IsNullOrEmpty(fireSeKey) && SEManager.I != null)
                SEManager.I.Play(fireSeKey, firePoint.position);

            if (debugLog) Debug.Log($"[AICombat] Fire #{i + 1}");

            if (i < shots - 1) yield return new WaitForSeconds(burstInterval);
        }
        yield return null;
    }

    // 見失い時：砲身ピッチを水平(0°)に戻す
    void RecoverPitchToZero(float dt)
    {
        if (!barrelPivot) return;

        float cur = NormalizeDeg(barrelPivot.localEulerAngles.x);
        float next = Mathf.MoveTowards(cur, 0f, pitchRecoverSpeed * dt);
        var e = barrelPivot.localEulerAngles;
        e.x = Wrap360(next);
        e.y = 0f; e.z = 0f;
        barrelPivot.localEulerAngles = e;
    }

    static float NormalizeDeg(float deg) => Mathf.Repeat(deg + 180f, 360f) - 180f;
    static float Wrap360(float deg) => (deg % 360f + 360f) % 360f;

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!debugShowDetectGizmos) return;

        // 索敵半径 Enter/Exit（塗りつぶし円）
        UnityEditor.Handles.color = new Color(1f, 1f, 0f, 0.25f);
        UnityEditor.Handles.DrawSolidDisc(transform.position, Vector3.up, detectRadiusEnter);
        UnityEditor.Handles.color = new Color(1f, 0.8f, 0f, 0.1f);
        UnityEditor.Handles.DrawSolidDisc(transform.position, Vector3.up, detectRadiusExit);

        // 視野角（FOV）の扇
        DrawFOVArc(fovEnter, detectRadiusEnter, new Color(0.2f, 0.8f, 1f, 0.4f));
        DrawFOVArc(fovExit, detectRadiusExit, new Color(0.2f, 0.6f, 1f, 0.2f));

        // 赤：現在の砲身向き／緑：理想の狙い方向
        Transform basis = barrelBore ? barrelBore : barrelPivot;
        if (basis)
        {
            Gizmos.color = Color.red;
            Vector3 fwd = invertBarrelForward ? -basis.forward : basis.forward;
            Gizmos.DrawRay(basis.position, fwd * 3f);
        }
        if (target && basis)
        {
            Gizmos.color = Color.green;
            Vector3 aim = (TargetAimPos() - basis.position).normalized;
            Gizmos.DrawRay(basis.position, aim * 3f);
        }
    }

    void DrawFOVArc(float fov, float radius, Color c)
    {
        using (new UnityEditor.Handles.DrawingScope(c, Matrix4x4.identity))
        {
            Vector3 pos = transform.position;
            Vector3 fwd = transform.forward; fwd.y = 0f; fwd.Normalize();
            float half = fov * 0.5f;
            Quaternion qL = Quaternion.AngleAxis(-half, Vector3.up);
            Quaternion qR = Quaternion.AngleAxis(+half, Vector3.up);
            UnityEditor.Handles.DrawWireArc(pos, Vector3.up, qL * fwd, fov, radius);
            UnityEditor.Handles.DrawLine(pos, pos + (qL * fwd * radius));
            UnityEditor.Handles.DrawLine(pos, pos + (qR * fwd * radius));
        }
    }
#endif
}
