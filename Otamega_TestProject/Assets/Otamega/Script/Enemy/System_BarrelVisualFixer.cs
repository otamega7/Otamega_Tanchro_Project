using UnityEngine;

/// <summary>
/// 砲身メッシュの見た目向きを固定する（ピッチは親Pivotが担当）
/// Meshが-90度や180度ズレている時に補正値で固定して、反転を防ぐ。
/// </summary>
public class System_BarrelVisualFixer : MonoBehaviour
{
    [Tooltip("メッシュの見た目調整（度）。YawはY軸、RollはZ軸。Pitch(X)は親Pivotが回します。")]
    public Vector3 meshFixEuler = new Vector3(0, 180, 0); // 例：モデルが後ろ向きなら Y=180

    void LateUpdate()
    {
        // 親（Pivot）が回っても、メッシュは“この相対姿勢”を常に維持
        transform.localEulerAngles = meshFixEuler;
    }
}
