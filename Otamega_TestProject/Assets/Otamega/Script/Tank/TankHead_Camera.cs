using UnityEngine;

[RequireComponent(typeof(Camera))]
public class TankHead_Camera : MonoBehaviour
{
    [Header("Follow Targets")]
    public Transform head;          // TankHead（親：Yaw & 位置基準）
    public Transform barrelPivot;   // TankHead_BarrelPivot（子：Pitch基準）

    [Header("View")]
    public Vector3 eyeOffset = new Vector3(0f, 0.2f, 0.1f); // 頭の少し前・上

    [Header("Clip & FOV")]
    public float nearClip = 0.03f;
    public float fieldOfView = 60f;

    Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
        cam.nearClipPlane = nearClip;
        cam.fieldOfView = fieldOfView;
    }

    void LateUpdate()
    {
        if (!head || !barrelPivot) return;

        // 位置は TankHead を基準に目線オフセット
        transform.position = head.TransformPoint(eyeOffset);

        // 回転は「砲身のforward」に向ける（＝Yawは親に従い、Pitchは砲身に連動）
        // 上向きベクトルはワールドUpに固定してロール（傾き）を防ぐ
        transform.rotation = Quaternion.LookRotation(barrelPivot.forward, Vector3.up);

        // もし微ロールを許容したいなら↑を barrelPivot.rotation に変える
        // transform.rotation = barrelPivot.rotation; // ロールも追随
    }
}
