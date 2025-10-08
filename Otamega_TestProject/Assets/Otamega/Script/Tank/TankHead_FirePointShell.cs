using UnityEngine;

public class TankProjectile : MonoBehaviour
{
    public float damage = 20f;
    public GameObject hitFx; // �p�[�e�B�N�����i�C�Ӂj

    void OnCollisionEnter(Collision other)
    {
        // TODO: �_���[�W�����i���肪HP�����Ă���Ή��Z�j
        if (hitFx) Instantiate(hitFx, transform.position, Quaternion.identity);

        Destroy(gameObject); // �����ŏ��Łi�ђʂ��Ȃ��ꍇ�j
    }
}
