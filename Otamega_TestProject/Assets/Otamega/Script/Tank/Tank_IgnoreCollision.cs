using UnityEngine;

public class TankIgnoreCollision : MonoBehaviour
{
    [Header("HeadとBodyのオブジェクトの衝突を無効")]
    public Collider headCollider; // TankHead の Collider
    public Collider bodyCollider; // TankBody の Collider

    void Start()
    {
        if (headCollider && bodyCollider)
        {
            Physics.IgnoreCollision(headCollider, bodyCollider, true);
        }
    }
}
