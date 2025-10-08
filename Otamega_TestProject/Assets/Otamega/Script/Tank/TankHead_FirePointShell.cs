using UnityEngine;

public class TankProjectile : MonoBehaviour
{
    public float damage = 20f;
    public GameObject hitFx; // パーティクル等（任意）

    void OnCollisionEnter(Collision other)
    {
        // TODO: ダメージ処理（相手がHP持っていれば加算）
        if (hitFx) Instantiate(hitFx, transform.position, Quaternion.identity);

        Destroy(gameObject); // 命中で消滅（貫通しない場合）
    }
}
