using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class TankBody_Move : MonoBehaviour
{
    [Header("Track Settings")]
    public float maxTrackSpeed = 6f;    // キャタピラ1本の最大速度[m/s]
    public float trackWidth = 2.0f;     // 左右キャタピラ間の距離[m]

    [Header("Acceleration")]
    public float linearAccel = 12f;     // 直進加速度[m/s^2]
    public float angularAccel = 10f;    // 旋回加速度[rad/s^2]

    Rigidbody rb;
    float curLinear;   // 前進速度[m/s]
    float curAngular;  // ヨー角速度[rad/s]

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    void FixedUpdate()
    {
        // --- 左キャタピラ入力 ---
        float leftInput = 0f;
        if (Input.GetKey(KeyCode.E)) leftInput += 1f;  // 前進
        if (Input.GetKey(KeyCode.D)) leftInput -= 1f;  // 後退

        // --- 右キャタピラ入力 ---
        float rightInput = 0f;
        if (Input.GetKey(KeyCode.Q)) rightInput += 1f; // 前進
        if (Input.GetKey(KeyCode.A)) rightInput -= 1f; // 後退

        // --- 左右キャタピラ速度[m/s] ---
        float vL = leftInput * maxTrackSpeed;
        float vR = rightInput * maxTrackSpeed;

        // --- 並進速度・角速度 ---
        float desiredLinear = (vR + vL) * 0.5f;
        float desiredAngular = (vR - vL) / Mathf.Max(0.01f, trackWidth);

        // --- 加速度で補間 ---
        curLinear = Mathf.MoveTowards(curLinear, desiredLinear, linearAccel * Time.fixedDeltaTime);
        curAngular = Mathf.MoveTowards(curAngular, desiredAngular, angularAccel * Time.fixedDeltaTime);

        // --- Rigidbodyで移動 ---
        Vector3 forwardMove = transform.forward * (curLinear * Time.fixedDeltaTime);
        rb.MovePosition(rb.position + forwardMove);

        float yawDeg = curAngular * Mathf.Rad2Deg * Time.fixedDeltaTime;
        rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, yawDeg, 0f));
    }
}
