using UnityEngine;

public class TankHead_FirePoint : MonoBehaviour
{
    [Header("Projectile")]
    public Rigidbody projectilePrefab;   // 砲弾プレハブ（Rigidbody付き）
    public float muzzleVelocity = 60f;   // 初速[m/s]
    public float lifeTime = 5f;          // 弾の寿命[秒]
    public float cooldown = 0.25f;       // 連射間隔[秒]
    [Range(0f, 5f)]
    public float spreadAngle = 0f;       // ばらつき角度[度]（不要なら0）

    [Header("Inputs")]
    public KeyCode fireKey = KeyCode.Space; // 発射キー（例：Space）

    [Header("Collision Ignore (任意)")]
    public Collider[] ownerColliders;    // 自機のCollider群（TankBody/TankHeadなど）

    [Header("Inherit Velocity (任意)")]
    public Rigidbody inheritFrom;        // 走行中の速度を弾に継承したい時に、TankBodyのRBなど

    float nextFireTime;

    void Update()
    {
        if (Input.GetKey(fireKey) && Time.time >= nextFireTime)
        {
            Fire();
            nextFireTime = Time.time + cooldown;
        }
    }

    void Fire()
    {
        if (!projectilePrefab) { Debug.LogWarning("projectilePrefab 未設定"); return; }

        // ばらつき（必要な時だけ適用）
        Quaternion spread = (spreadAngle > 0f)
            ? Quaternion.AngleAxis(Random.Range(-spreadAngle * 0.5f, spreadAngle * 0.5f), transform.up)
              * Quaternion.AngleAxis(Random.Range(-spreadAngle * 0.5f, spreadAngle * 0.5f), transform.right)
            : Quaternion.identity;

        // 生成
        Rigidbody proj = Instantiate(projectilePrefab, transform.position, spread * transform.rotation);

        // 自機と当たらないように無視設定
        if (ownerColliders != null && ownerColliders.Length > 0)
        {
            var projCol = proj.GetComponent<Collider>();
            if (projCol)
            {
                foreach (var c in ownerColliders)
                {
                    if (c) Physics.IgnoreCollision(projCol, c, true);
                }
            }
        }

        // 速度付与（自機の速度を上乗せしたい場合は inheritFrom を使う）
        Vector3 v = proj.transform.forward * muzzleVelocity;
        if (inheritFrom) v += inheritFrom.linearVelocity;

        proj.linearVelocity = v;

        // 物理の安定化（推奨設定）
        proj.interpolation = RigidbodyInterpolation.Interpolate;
        proj.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        // 一定時間後に消す
        Destroy(proj.gameObject, lifeTime);

        // 砲口フラッシュ
        EfManager.I.Spawn(
            key: "MuzzleFlash",
            pos: transform.position,
            rot: transform.rotation,
            follow: transform,    // 砲身に追従
            life: 0.12f           // 0以下ならParticle停止で自動回収
        );


        // 砲撃時ランダム（砲口位置で3D再生）
        SEManager.I.Play("TankFire", transform.position);

    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, transform.forward * 1.5f);
    }
#endif
}
