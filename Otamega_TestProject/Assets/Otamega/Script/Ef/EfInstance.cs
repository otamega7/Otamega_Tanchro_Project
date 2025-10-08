using System.Collections;
using UnityEngine;

public class EfInstance : MonoBehaviour
{
    [HideInInspector] public EfManager manager;
    [HideInInspector] public GameObject sourcePrefab;

    Coroutine lifeCo;

    public void AutoDespawnAfter(float seconds)
    {
        if (lifeCo != null) StopCoroutine(lifeCo);
        lifeCo = StartCoroutine(CoDespawnAfter(seconds));
    }

    public void TryAutoDespawnByParticles()
    {
        // �p�[�e�B�N�����S���~�܂�̂�҂��ĉ��
        if (lifeCo != null) StopCoroutine(lifeCo);
        lifeCo = StartCoroutine(CoDespawnWhenParticlesDone());
    }

    IEnumerator CoDespawnAfter(float t)
    {
        yield return new WaitForSeconds(t);
        Despawn();
    }

    IEnumerator CoDespawnWhenParticlesDone()
    {
        var list = GetComponentsInChildren<ParticleSystem>(true);
        // �Z�[�t�e�B�^�C���A�E�g
        float timeout = 10f;
        float elapsed = 0f;

        while (elapsed < timeout)
        {
            bool anyAlive = false;
            foreach (var p in list)
            {
                if (p && (p.IsAlive(true) || p.particleCount > 0))
                {
                    anyAlive = true; break;
                }
            }
            if (!anyAlive) break;
            elapsed += Time.deltaTime;
            yield return null;
        }
        Despawn();
    }

    public void Despawn()
    {
        if (manager && sourcePrefab) manager.Despawn(gameObject, sourcePrefab);
        else Destroy(gameObject); // ����}�l�[�W���s�݂ł����[�N�����Ȃ�
    }
}
