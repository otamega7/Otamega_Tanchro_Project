using UnityEngine;

/// <summary>
/// �����L���^�s���i���E�g���b�N�j�x�[�X�̓G���AI�ړ��B
/// �EWaypoints ������ / target �ɒǐՁi�C�Ӂj
/// �ERigidbody.MovePosition/MoveRotation ���g�p
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class EnemyTankBody_Move : MonoBehaviour
{
    public enum AIMode { Patrol, ChaseTarget }
    [Header("AI")]
    public AIMode mode = AIMode.Patrol;
    public Transform[] waypoints;   // ����|�C���g
    public Transform target;        // �ǐՑΏہi�C�Ӂj
    public float arriveDistance = 2.0f;     // �ړI�n���B����
    public float switchDelay = 0.3f;        // �ړI�n�ؑւ̃f�t�H���g�x��

    [Header("Track (����)")]
    public float maxTrackSpeed = 6f; // �e�L���^�s���̍ō����x[m/s]
    public float trackWidth = 2.0f;  // �g���b�h��[m]
    public float linearAccel = 12f;  // ���i�����x[m/s^2]
    public float angularAccel = 10f; // �p�����x[rad/s^2]

    [Header("Steer�i����p�����[�^�j")]
    public float desiredSpeed = 4.5f;   // �ڕW���q���x[m/s]
    public float turnGain = 2.2f;       // �ڕW�p�x���ɑ΂������Q�C��
    public float slowAngle = 25f;       // ����ȏ�̊p�x���Ō������Đ���[deg]
    public float stopAngle = 120f;      // ����ȏ�̊p�x���ł͂��̏����D��[deg]

    [Header("Obstacle (�Ȉ�)")]
    public bool enableAvoid = true;
    public float avoidCheckDist = 4f;
    public float avoidTurnBias = 1.0f; // ��Q�������m�������Ƌt�։�

    Rigidbody rb;
    int wpIndex = 0;
    float curLinear, curAngular; // ���݂̕��i/�p���x
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

            // ���B����
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

        // �ڕW����
        Vector3 toGoal = (goalPos - transform.position);
        toGoal.y = 0f;
        float dist = toGoal.magnitude;
        if (dist < 0.001f) { BrakeToStop(); return; }

        Vector3 fwd = transform.forward;
        float signedAngle = Vector3.SignedAngle(fwd, toGoal.normalized, Vector3.up); // +�E��

        // ���x�w�߁i�󋵂Œ����j
        float targetLinear = desiredSpeed;
        float targetAngular = Mathf.Deg2Rad * (signedAngle * turnGain);

        // �傫���p�x���Y�����Ƃ��͐���D��Ō���/��~
        float a = Mathf.Abs(signedAngle);
        if (a > slowAngle) targetLinear *= Mathf.InverseLerp(stopAngle, slowAngle, a); // stopAngle��0, slowAngle��1
        if (a > stopAngle) targetLinear = 0f;

        // �ȈՉ���F�O�����C�Ńq�b�g�������Ƌt�ɉ񓪃o�C�A�X
        if (enableAvoid)
        {
            Vector3 origin = transform.position + Vector3.up * 0.5f;
            if (Physics.Raycast(origin, fwd, out RaycastHit hit, avoidCheckDist))
            {
                // ���E�Ƀ��C��ǉ����ĉ����������߂�
                bool leftBlocked = Physics.Raycast(origin, Quaternion.AngleAxis(-25f, Vector3.up) * fwd, out _, avoidCheckDist * 0.8f);
                bool rightBlocked = Physics.Raycast(origin, Quaternion.AngleAxis(+25f, Vector3.up) * fwd, out _, avoidCheckDist * 0.8f);

                if (leftBlocked && !rightBlocked) targetAngular += Mathf.Deg2Rad * 45f * avoidTurnBias;
                else if (!leftBlocked && rightBlocked) targetAngular += -Mathf.Deg2Rad * 45f * avoidTurnBias;
                else targetLinear *= 0.2f; // �����ǂ����Ă���Α��x�𗎂Ƃ�
            }
        }

        // �����g���b�N���x�֕ϊ��i�ڕW�l�j
        float vL_des = targetLinear - targetAngular * trackWidth * 0.5f; // v = (R+L)/2, w = (R-L)/W
        float vR_des = targetLinear + targetAngular * trackWidth * 0.5f;

        // ���i/�p���x�̌`�ŉ����x����
        float desiredLinear = (vR_des + vL_des) * 0.5f;
        float desiredAngular = (vR_des - vL_des) / Mathf.Max(0.01f, trackWidth);

        curLinear = Mathf.MoveTowards(curLinear, desiredLinear, linearAccel * Time.fixedDeltaTime);
        curAngular = Mathf.MoveTowards(curAngular, desiredAngular, angularAccel * Time.fixedDeltaTime);

        // ������ѐ��̂���ړ�
        Vector3 forwardMove = fwd * (curLinear * Time.fixedDeltaTime);
        rb.MovePosition(rb.position + forwardMove);

        float yawDeg = curAngular * Mathf.Rad2Deg * Time.fixedDeltaTime;
        rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, yawDeg, 0f));
    }

    void BrakeToStop()
    {
        curLinear = Mathf.MoveTowards(curLinear, 0f, linearAccel * Time.fixedDeltaTime);
        curAngular = Mathf.MoveTowards(curAngular, 0f, angularAccel * Time.fixedDeltaTime);
        // �ق�̏������������ړ�
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
