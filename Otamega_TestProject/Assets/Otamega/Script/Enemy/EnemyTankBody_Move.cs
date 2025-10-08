using UnityEngine;

/// <summary>
/// 差動キャタピラ（左右トラック）ベースの敵戦車AI移動。
/// ・Waypoints を巡回 / target に追跡（任意）
/// ・Rigidbody.MovePosition/MoveRotation を使用
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class EnemyTankBody_Move : MonoBehaviour
{
    public enum AIMode { Patrol, ChaseTarget }
    [Header("AI")]
    public AIMode mode = AIMode.Patrol;
    public Transform[] waypoints;   // 巡回ポイント
    public Transform target;        // 追跡対象（任意）
    public float arriveDistance = 2.0f;     // 目的地到達判定
    public float switchDelay = 0.3f;        // 目的地切替のデフォルト遅延

    [Header("Track (物理)")]
    public float maxTrackSpeed = 6f; // 各キャタピラの最高速度[m/s]
    public float trackWidth = 2.0f;  // トレッド幅[m]
    public float linearAccel = 12f;  // 並進加速度[m/s^2]
    public float angularAccel = 10f; // 角加速度[rad/s^2]

    [Header("Steer（制御パラメータ）")]
    public float desiredSpeed = 4.5f;   // 目標巡航速度[m/s]
    public float turnGain = 2.2f;       // 目標角度差に対する旋回ゲイン
    public float slowAngle = 25f;       // これ以上の角度差で減速して旋回[deg]
    public float stopAngle = 120f;      // これ以上の角度差ではその場旋回優先[deg]

    [Header("Obstacle (簡易)")]
    public bool enableAvoid = true;
    public float avoidCheckDist = 4f;
    public float avoidTurnBias = 1.0f; // 障害物を検知した側と逆へ回頭

    Rigidbody rb;
    int wpIndex = 0;
    float curLinear, curAngular; // 現在の並進/角速度
    float switchCooldown;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    void FixedUpdate()
    {
        Vector3 goalPos;
        if (mode == AIMode.ChaseTarget && target)
        {
            goalPos = target.position;
        }
        else
        {
            if (waypoints == null || waypoints.Length == 0) { BrakeToStop(); return; }
            goalPos = waypoints[wpIndex].position;

            // 到達判定
            if ((transform.position - goalPos).sqrMagnitude <= arriveDistance * arriveDistance)
            {
                if (switchCooldown <= 0f)
                {
                    wpIndex = (wpIndex + 1) % waypoints.Length;
                    switchCooldown = switchDelay;
                }
            }
            if (switchCooldown > 0f) switchCooldown -= Time.fixedDeltaTime;
        }

        // 目標方向
        Vector3 toGoal = (goalPos - transform.position);
        toGoal.y = 0f;
        float dist = toGoal.magnitude;
        if (dist < 0.001f) { BrakeToStop(); return; }

        Vector3 fwd = transform.forward;
        float signedAngle = Vector3.SignedAngle(fwd, toGoal.normalized, Vector3.up); // +右回頭

        // 速度指令（状況で調整）
        float targetLinear = desiredSpeed;
        float targetAngular = Mathf.Deg2Rad * (signedAngle * turnGain);

        // 大きく角度がズレたときは旋回優先で減速/停止
        float a = Mathf.Abs(signedAngle);
        if (a > slowAngle) targetLinear *= Mathf.InverseLerp(stopAngle, slowAngle, a); // stopAngleで0, slowAngleで1
        if (a > stopAngle) targetLinear = 0f;

        // 簡易回避：前方レイでヒットした側と逆に回頭バイアス
        if (enableAvoid)
        {
            Vector3 origin = transform.position + Vector3.up * 0.5f;
            if (Physics.Raycast(origin, fwd, out RaycastHit hit, avoidCheckDist))
            {
                // 左右にレイを追加して回避方向を決める
                bool leftBlocked = Physics.Raycast(origin, Quaternion.AngleAxis(-25f, Vector3.up) * fwd, out _, avoidCheckDist * 0.8f);
                bool rightBlocked = Physics.Raycast(origin, Quaternion.AngleAxis(+25f, Vector3.up) * fwd, out _, avoidCheckDist * 0.8f);

                if (leftBlocked && !rightBlocked) targetAngular += Mathf.Deg2Rad * 45f * avoidTurnBias;
                else if (!leftBlocked && rightBlocked) targetAngular += -Mathf.Deg2Rad * 45f * avoidTurnBias;
                else targetLinear *= 0.2f; // 両方塞がっていれば速度を落とす
            }
        }

        // 差動トラック速度へ変換（目標値）
        float vL_des = targetLinear - targetAngular * trackWidth * 0.5f; // v = (R+L)/2, w = (R-L)/W
        float vR_des = targetLinear + targetAngular * trackWidth * 0.5f;

        // 並進/角速度の形で加速度制限
        float desiredLinear = (vR_des + vL_des) * 0.5f;
        float desiredAngular = (vR_des - vL_des) / Mathf.Max(0.01f, trackWidth);

        curLinear = Mathf.MoveTowards(curLinear, desiredLinear, linearAccel * Time.fixedDeltaTime);
        curAngular = Mathf.MoveTowards(curAngular, desiredAngular, angularAccel * Time.fixedDeltaTime);

        // 物理一貫性のある移動
        Vector3 forwardMove = fwd * (curLinear * Time.fixedDeltaTime);
        rb.MovePosition(rb.position + forwardMove);

        float yawDeg = curAngular * Mathf.Rad2Deg * Time.fixedDeltaTime;
        rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, yawDeg, 0f));
    }

    void BrakeToStop()
    {
        curLinear = Mathf.MoveTowards(curLinear, 0f, linearAccel * Time.fixedDeltaTime);
        curAngular = Mathf.MoveTowards(curAngular, 0f, angularAccel * Time.fixedDeltaTime);
        // ほんの少しだけ減速移動
        Vector3 forwardMove = transform.forward * (curLinear * Time.fixedDeltaTime);
        rb.MovePosition(rb.position + forwardMove);
        float yawDeg = curAngular * Mathf.Rad2Deg * Time.fixedDeltaTime;
        rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, yawDeg, 0f));
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position + Vector3.up * 0.5f, transform.forward * avoidCheckDist);
        if (waypoints != null && waypoints.Length > 0)
        {
            Gizmos.color = Color.yellow;
            Vector3 prev = transform.position;
            foreach (var w in waypoints)
            {
                if (!w) continue;
                Gizmos.DrawSphere(w.position + Vector3.up * 0.1f, 0.2f);
                Gizmos.DrawLine(prev, w.position);
                prev = w.position;
            }
        }
    }
#endif
}
