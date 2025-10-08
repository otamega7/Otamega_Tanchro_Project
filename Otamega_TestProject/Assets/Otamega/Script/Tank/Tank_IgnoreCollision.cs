using UnityEngine;

public class TankIgnoreCollision : MonoBehaviour
{
    [Header("Head��Body�̃I�u�W�F�N�g�̏Փ˂𖳌�")]
    public Collider headCollider; // TankHead �� Collider
    public Collider bodyCollider; // TankBody �� Collider

    void Start()
    {
        if (headCollider && bodyCollider)
        {
            Physics.IgnoreCollision(headCollider, bodyCollider, true);
        }
    }
}
