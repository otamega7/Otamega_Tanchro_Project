using UnityEngine;

public class TankHead_Move : MonoBehaviour
{
    [Header("Follow Body (����)")]
    public Transform tankBody;   // ����TankBody
    public float yOffset = 2f;   // ���㍂��
    public bool physicsFollow = false;

    [Header("Yaw (���E�FI / P)")]
    public float yawSpeed = 90f; // �x/�b
    public KeyCode yawRightKey = KeyCode.I;
    public KeyCode yawLeftKey = KeyCode.P;

    [Header("Pitch (�㉺�FO / L)")]
    public Transform System_TankHeadBarrel_Pivot;   // �C�g�̃q���W
    public float pitchSpeed = 60f;    // �x/�b
    public float minPitch = -14f;     // �����i�������j
    public float maxPitch = 20f;     // ����i������j
    public KeyCode pitchUpKey = KeyCode.O;
    public KeyCode pitchDownKey = KeyCode.L;

    float currentPitch = 0f; // �C�g�̌��݃s�b�`(�x)

    void LateUpdate()
    {
        if (physicsFollow) return;
        FollowBody();
        HandleYaw();
        HandlePitch();
    }

    void FixedUpdate()
    {
        if (!physicsFollow) return;
        FollowBody();
        HandleYaw();
        HandlePitch();
    }

    void FollowBody()
    {
        if (!tankBody) return;
        var p = tankBody.position; p.y += yOffset;
        transform.position = p;
        // TankHead�̏㉺�͉񂳂Ȃ��i�㉺�͖C�g�����ɔC����j
    }

    void HandleYaw()
    {
        float yaw = 0f;
        if (Input.GetKey(yawRightKey)) yaw += 1f; // �E��]
        if (Input.GetKey(yawLeftKey)) yaw -= 1f; // ����]
        if (Mathf.Abs(yaw) > 0f)
            transform.Rotate(Vector3.up, yaw * yawSpeed * Time.deltaTime, Space.World);
    }

    void HandlePitch()
    {
        if (!System_TankHeadBarrel_Pivot) return;

        float dir = 0f;
        if (Input.GetKey(pitchUpKey)) dir -= 1f;  // ��
        if (Input.GetKey(pitchDownKey)) dir += 1f;  // ��

        if (dir != 0f)
        {
            currentPitch = Mathf.Clamp(currentPitch + dir * pitchSpeed * Time.deltaTime, minPitch, maxPitch);
            // System_TankHeadBarrel_Pivot�̃��[�J��X��]������ύX�iYaw��TankHead�ɔC����j
            var e = System_TankHeadBarrel_Pivot.localEulerAngles;
            // Unity�̃I�C���[��0-360�Ȃ̂ŁA���ڊp�x��ݒ�
            e.x = currentPitch;
            e.y = 0f; // �C�g�͍��E�񂳂Ȃ��iYaw�͐e��TankHead�j
            e.z = 0f;
            System_TankHeadBarrel_Pivot.localEulerAngles = e;
        }
    }
}
