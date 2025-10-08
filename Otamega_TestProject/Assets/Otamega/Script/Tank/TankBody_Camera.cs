using UnityEngine;

public class TankBody_Camera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;              // �Ǐ]����v���C���[
    public Vector3 targetOffset = new Vector3(0f, 1.5f, 0f); // ���`�����炢�̍���

    [Header("Orbit & Pitch")]
    public float mouseSensitivity = 150f; // �}�E�X���x
    public float minPitch = -20f;         // ���������E
    public float maxPitch = 60f;          // ��������E

    [Header("Distance & Zoom")]
    public float distance = 4.5f;         // ��{����
    public float minDistance = 2.0f;      // �ŏ��Y�[��
    public float maxDistance = 7.0f;      // �ő�Y�[��
    public float zoomSpeed = 5f;          // �z�C�[���Y�[�����x

    [Header("Smoothing")]
    public float followSmooth = 0.00f;    // �ʒu�X���[�Y
    public float rotateSmooth = 0.00f;    // ��]�X���[�Y

    [Header("Collision")]
    public LayerMask obstacleLayers;      // �ǂȂ�
    public float collisionRadius = 0.2f;  // �J���������蔻��
    public float collisionBuffer = 0.2f;  // �ǂ��班������

    float yaw;    // �����p
    float pitch;  // �����p
    Vector3 camVelocity;       // SmoothDamp�p
    Vector3 currentLookDir;    // ��]��ԗp

    void Start()
    {
        if (target == null)
        {
            Debug.LogWarning("[ThirdPersonCamera] Target�����ݒ�ł��B�v���C���[��Transform�����蓖�ĂĂ��������B");
            enabled = false;
            return;
        }

        // �����p�x�����݌����Ă����������v�Z
        Vector3 forward = target.forward;
        yaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
        pitch = 15f; // ���ՋC���̏����p�x
        currentLookDir = transform.forward;
        Cursor.lockState = CursorLockMode.None; // �K�v�ɉ�����.Locked��
        Cursor.visible = true;
    }

    void LateUpdate()
    {
        if (target == null) return;

        // --- ���́i�}�E�X��] & �z�C�[���Y�[���j ---
        float mx = Input.GetAxis("Mouse X");
        float my = Input.GetAxis("Mouse Y");
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        yaw += mx * mouseSensitivity * Time.deltaTime;
        pitch -= my * mouseSensitivity * Time.deltaTime;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        distance -= scroll * zoomSpeed;
        distance = Mathf.Clamp(distance, minDistance, maxDistance);

        // --- �ڕW�����_�i�v���C���[�̏�����j ---
        Vector3 focus = target.position + targetOffset;

        // --- �]�܂����J�����ʒu�i�p�x�Ƌ�������Z�o�j ---
        Quaternion desiredRot = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 desiredDir = desiredRot * Vector3.back; // ���
        Vector3 desiredPos = focus + desiredDir * distance;

        // --- ��Q������iSphereCast�ŕǂƂ߂荞�ݖh�~�j ---
        float adjustedDistance = distance;
        if (Physics.SphereCast(focus, collisionRadius, desiredDir, out RaycastHit hit, distance, obstacleLayers, QueryTriggerInteraction.Ignore))
        {
            adjustedDistance = Mathf.Max(hit.distance - collisionBuffer, minDistance);
            desiredPos = focus + desiredDir * adjustedDistance;
        }

        // --- �X���[�Y�ɒǏ] ---
        Vector3 newPos = Vector3.SmoothDamp(transform.position, desiredPos, ref camVelocity, followSmooth);
        transform.position = newPos;

        // --- �����������X���[�Y�� ---
        Vector3 lookDir = (focus - transform.position).normalized;
        currentLookDir = Vector3.Slerp(currentLookDir, lookDir, 1f - Mathf.Exp(-Time.deltaTime / rotateSmooth));
        transform.rotation = Quaternion.LookRotation(currentLookDir, Vector3.up);
    }

    // �G�f�B�^�ŋO�Ղ����o���i�C�Ӂj
    void OnDrawGizmosSelected()
    {
        if (target == null) return;
        Gizmos.color = Color.cyan;
        Vector3 focus = target.position + targetOffset;
        Gizmos.DrawWireSphere(focus, 0.05f);
    }
}
