using UnityEngine;

public class TankHead_Projectile : MonoBehaviour
{
    [Header("Lifetime")]
    public float maxLife = 5f;           // ���̑΍�F��ё����Ă�����

    [Header("Effects (optional)")]
    public string impactFxKey = "HitExplosion";   // EfManager �œo�^�����L�[
    public string impactSeKey = "HitExplosion";      // SEManager �œo�^�����L�[

    [Header("Hit Filter (optional)")]
    public LayerMask hitLayers = ~0;     // �����肽�����C���i����͑S���j
    public bool destroyOnTrigger = true; // Trigger�ł����������Ȃ�true

    bool _dead;

    void Start()
    {
        if (maxLife > 0f) Destroy(gameObject, maxLife);
    }

    void OnCollisionEnter(Collision c)
    {
        if (_dead) return;
        if (!IsLayerAllowed(c.collider.gameObject.layer)) return;

        // ���e�ʒu�E�@��
        Vector3 p = c.GetContact(0).point;
        Vector3 n = c.GetContact(0).normal;

        // ������
        // ��������EfManager���Ăԁi�G�t�F�N�g�����j
        EfManager.I.Spawn(
            key: "HitExplosion",
            pos: p,
            rot: Quaternion.LookRotation(n)
        );

        Die();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!destroyOnTrigger || _dead) return;
        if (!IsLayerAllowed(other.gameObject.layer)) return;

        // --- �@������i����ƌ��h�����ǂ��j ---
        Vector3 p = transform.position;
        Vector3 n = -transform.forward;

        // ���x�����ɒZ�����C�L���X�g�ŕ\�ʖ@�����E���i�C�Ӂj
        var rb = GetComponent<Rigidbody>();
        if (rb && rb.linearVelocity.sqrMagnitude > 0.0001f)
        {
            Vector3 dir = rb.linearVelocity.normalized;
            if (Physics.Raycast(p - dir * 0.3f, dir, out RaycastHit hit, 0.6f, hitLayers, QueryTriggerInteraction.Ignore))
            {
                p = hit.point;
                n = hit.normal;
            }
        }

        SpawnImpact(p, n);
        Die();
    }

    bool IsLayerAllowed(int layer)
    {
        return (hitLayers.value & (1 << layer)) != 0;
    }

    void SpawnImpact(Vector3 pos, Vector3 normal)
    {
        // �G�t�F�N�g
        if (!string.IsNullOrEmpty(impactFxKey) && EfManager.I != null)
        {
            EfManager.I.Spawn(impactFxKey, pos, Quaternion.LookRotation(normal));
        }
        // SE
        if (!string.IsNullOrEmpty(impactSeKey) && SEManager.I != null)
        {
            SEManager.I.Play(impactSeKey, pos);
        }
    }

    void Die()
    {
        _dead = true;
        Destroy(gameObject);
    }

}
