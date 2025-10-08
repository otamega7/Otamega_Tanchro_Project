using UnityEngine;

/// <summary>
/// �G�^���N�̖C���R���g���[���i���ԁj
/// - ��e�q�̂܂� Body �ɒǐ��i�ʒuY�I�t�Z�b�g�j
/// - Yaw�i�C���j�񓪁FBody�̑O���܂��̓^�[�Q�b�g������
/// - Pitch�i�C�g�j�㉺�FbarrelPivot �̃��[�J��X�Ő���i����v�Z���j
/// - �㉺���t�̃��f���ɑΉ����� invertPitch �g�O������
/// </summary>
public class EnemyTankHead_Controller : MonoBehaviour
{
    public enum AimMode { FollowBodyForward, TrackTarget }

    [Header("Follow (����)")]
    public Transform tankBody;           // �ǐ����iRigidbody�t����Body�����j
    public float headHeight = 2.0f;      // ���ԃI�t�Z�b�g�iY�j
    public bool physicsFollow = false;   // Body��FixedUpdate�쓮�Ȃ�true

    [Header("Aiming")]
    public AimMode aimMode = AimMode.FollowBodyForward;
    public Transform target;             // TrackTarget���̑���
    public float aimYOffset = 0f;        // �ڕW�̑_�������i��/���Ȃǁj

    [Header("Yaw (�C����)")]
    public float yawSpeed = 120f;        // �x/�b
    public float yawDeadZone = 1f;       // ���p�����i���U��h�~�j

    [Header("Pitch (�C�g�㉺)")]
    public Transform barrelPivot;        // ���[�J��X�ŏ㉺
    public float pitchSpeed = 80f;       // �x/�b
    public float minPitch = -10f;        // ������(��) ���E
    public float maxPitch = 25f;        // �����(��) ���E
    public float pitchDeadZone = 0.5f;   // �����ω�����
    public bool invertPitch = false;    // ���㉺���]�F���f���s���ŋt�Ȃ�ON��

    float _currentPitch; // ���[�J��X�i�x�j

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

    // ������������������������������������������������������������������������������������������

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
        // �񓪂� AimUpdate() �ōs��
    }

    void AimUpdate(float dt)
    {
        if (!tankBody || !barrelPivot) return;

        // �ڕW�_�̌���
        Vector3 aimPoint;
        if (aimMode == AimMode.TrackTarget && target)
            aimPoint = target.position + Vector3.up * aimYOffset;
        else
            aimPoint = transform.position + tankBody.forward * 100f; // �ԑ̑O���̉��z�_

        // ���� Yaw�F�����񓪁iXZ���e�j
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

        // ���� Pitch�F����v�Z�i�O�������͑O�����x�N�g���Ƃ̓��ςŎ擾�j
        Vector3 aimVec = (aimPoint - barrelPivot.position);
        float forwardDist = Mathf.Abs(Vector3.Dot(aimVec, transform.forward)); // �O�㐬���̐�Βl
        float height = Vector3.Dot(aimVec, transform.up);                 // �㉺����
        float desiredPitch = Mathf.Rad2Deg * Mathf.Atan2(height, Mathf.Max(0.001f, forwardDist));


        // �O���������ɏ��ł��\��Ȃ��悤�ɉ�����݂�Abs�ň��艻
        float desired = Mathf.Rad2Deg * Mathf.Atan2(
            height,
            Mathf.Max(0.001f, Mathf.Abs(forwardDist))
        );

        // �㉺���]�ɑΉ�
        float lo = minPitch, hi = maxPitch;
        if (invertPitch)
        {
            desired = -desired;
            float tmp = lo; lo = -hi; hi = -tmp; // ������������]
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

    // ���� helpers
    static float NormalizeDeg(float deg) => Mathf.Repeat(deg + 180f, 360f) - 180f; // [-180,180)
    static float Wrap180(float deg) => NormalizeDeg(deg);
    static float Wrap360(float deg) => (deg % 360f + 360f) % 360f;

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // �C�gforward�i�ԁj��Yaw�ڕW�i���j
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
