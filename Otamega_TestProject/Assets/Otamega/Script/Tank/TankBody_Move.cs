using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class TankBody_Move : MonoBehaviour
{
    [Header("Track Settings")]
    public float maxTrackSpeed = 6f;    // �L���^�s��1�{�̍ő呬�x[m/s]
    public float trackWidth = 2.0f;     // ���E�L���^�s���Ԃ̋���[m]

    [Header("Acceleration")]
    public float linearAccel = 12f;     // ���i�����x[m/s^2]
    public float angularAccel = 10f;    // ��������x[rad/s^2]

    Rigidbody rb;
    float curLinear;   // �O�i���x[m/s]
    float curAngular;  // ���[�p���x[rad/s]

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    void FixedUpdate()
    {
        // --- ���L���^�s������ ---
        float leftInput = 0f;
        if (Input.GetKey(KeyCode.E)) leftInput += 1f;  // �O�i
        if (Input.GetKey(KeyCode.D)) leftInput -= 1f;  // ���

        // --- �E�L���^�s������ ---
        float rightInput = 0f;
        if (Input.GetKey(KeyCode.Q)) rightInput += 1f; // �O�i
        if (Input.GetKey(KeyCode.A)) rightInput -= 1f; // ���

        // --- ���E�L���^�s�����x[m/s] ---
        float vL = leftInput * maxTrackSpeed;
        float vR = rightInput * maxTrackSpeed;

        // --- ���i���x�E�p���x ---
        float desiredLinear = (vR + vL) * 0.5f;
        float desiredAngular = (vR - vL) / Mathf.Max(0.01f, trackWidth);

        // --- �����x�ŕ�� ---
        curLinear = Mathf.MoveTowards(curLinear, desiredLinear, linearAccel * Time.fixedDeltaTime);
        curAngular = Mathf.MoveTowards(curAngular, desiredAngular, angularAccel * Time.fixedDeltaTime);

        // --- Rigidbody�ňړ� ---
        Vector3 forwardMove = transform.forward * (curLinear * Time.fixedDeltaTime);
        rb.MovePosition(rb.position + forwardMove);

        float yawDeg = curAngular * Mathf.Rad2Deg * Time.fixedDeltaTime;
        rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, yawDeg, 0f));
    }
}
