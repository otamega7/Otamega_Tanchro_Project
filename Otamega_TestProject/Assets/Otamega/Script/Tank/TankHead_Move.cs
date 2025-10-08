using UnityEngine;

public class TankHead_Move : MonoBehaviour
{
    [Header("Follow Body (肩車)")]
    public Transform tankBody;   // 下のTankBody
    public float yOffset = 2f;   // 頭上高さ
    public bool physicsFollow = false;

    [Header("Yaw (左右：I / P)")]
    public float yawSpeed = 90f; // 度/秒
    public KeyCode yawRightKey = KeyCode.I;
    public KeyCode yawLeftKey = KeyCode.P;

    [Header("Pitch (上下：O / L)")]
    public Transform System_TankHeadBarrel_Pivot;   // 砲身のヒンジ
    public float pitchSpeed = 60f;    // 度/秒
    public float minPitch = -14f;     // 下限（下向き）
    public float maxPitch = 20f;     // 上限（上向き）
    public KeyCode pitchUpKey = KeyCode.O;
    public KeyCode pitchDownKey = KeyCode.L;

    float currentPitch = 0f; // 砲身の現在ピッチ(度)

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
        // TankHeadの上下は回さない（上下は砲身だけに任せる）
    }

    void HandleYaw()
    {
        float yaw = 0f;
        if (Input.GetKey(yawRightKey)) yaw += 1f; // 右回転
        if (Input.GetKey(yawLeftKey)) yaw -= 1f; // 左回転
        if (Mathf.Abs(yaw) > 0f)
            transform.Rotate(Vector3.up, yaw * yawSpeed * Time.deltaTime, Space.World);
    }

    void HandlePitch()
    {
        if (!System_TankHeadBarrel_Pivot) return;

        float dir = 0f;
        if (Input.GetKey(pitchUpKey)) dir -= 1f;  // 上
        if (Input.GetKey(pitchDownKey)) dir += 1f;  // 下

        if (dir != 0f)
        {
            currentPitch = Mathf.Clamp(currentPitch + dir * pitchSpeed * Time.deltaTime, minPitch, maxPitch);
            // System_TankHeadBarrel_PivotのローカルX回転だけを変更（YawはTankHeadに任せる）
            var e = System_TankHeadBarrel_Pivot.localEulerAngles;
            // Unityのオイラーは0-360なので、直接角度を設定
            e.x = currentPitch;
            e.y = 0f; // 砲身は左右回さない（Yawは親のTankHead）
            e.z = 0f;
            System_TankHeadBarrel_Pivot.localEulerAngles = e;
        }
    }
}
