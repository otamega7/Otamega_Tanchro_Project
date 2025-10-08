using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.ShaderGraph.Internal.KeywordDependentCollection;

[DefaultExecutionOrder(-1000)]
public class EfManager : MonoBehaviour
{
    [System.Serializable]
    public class EfEntry
    {
        public string key;        // ��: "MuzzleFlash", "ImpactSparks"
        public GameObject prefab; // Particle/Light/�C�ӂ�Prefab
        [Min(0)] public int preloadCount = 0; // ���O�v�[����
    }

    public static EfManager I { get; private set; }

    [Header("Registry")]
    public List<EfEntry> entries = new();

    // key -> prefab
    Dictionary<string, GameObject> _map = new();
    // prefab -> pool
    Dictionary<GameObject, Queue<GameObject>> _pools = new();

    void Awake()
    {
        if (I && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);

        _map.Clear();
        foreach (var e in entries)
        {
            if (e == null || string.IsNullOrEmpty(e.key) || e.prefab == null) continue;
            _map[e.key] = e.prefab;

            if (!_pools.ContainsKey(e.prefab))
                _pools[e.prefab] = new Queue<GameObject>();

            // preload
            for (int i = 0; i < e.preloadCount; i++)
            {
                var go = Instantiate(e.prefab);
                go.SetActive(false);
                _pools[e.prefab].Enqueue(go);
            }
        }
    }

    public GameObject Spawn(string key, Vector3 pos, Quaternion rot, Transform follow = null, float life = -1f)
    {
        if (!_map.TryGetValue(key, out var prefab) || prefab == null)
        {
            Debug.LogWarning($"[Ef] key '{key}' not found.");
            return null;
        }
        var go = GetFromPool(prefab);
        go.transform.SetPositionAndRotation(pos, rot);

        if (follow)
        {
            // �Ǐ]�͐e�q�ɂ��Ȃ��ō��W�X�V�������Ȃ� EfFollower ���g���Ă�OK
            go.transform.SetParent(follow, worldPositionStays: true);
        }
        go.SetActive(true);

        // �������Ƀ��Z�b�g�iParticle���j
        ResetEffect(go);

        // �������
        var inst = go.GetComponent<EfInstance>();
        if (!inst) inst = go.AddComponent<EfInstance>();
        inst.sourcePrefab = prefab;
        inst.manager = this;

        if (life > 0f) inst.AutoDespawnAfter(life);
        else inst.TryAutoDespawnByParticles(); // �p�[�e�B�N�����~�܂�����߂�

        return go;
    }

    GameObject GetFromPool(GameObject prefab)
    {
        if (_pools.TryGetValue(prefab, out var q) && q.Count > 0)
        {
            var go = q.Dequeue();
            if (go) return go;
        }
        return Instantiate(prefab);
    }

    internal void Despawn(GameObject go, GameObject prefab)
    {
        if (!go) return;
        go.SetActive(false);
        go.transform.SetParent(transform, false); // ���ݗ��߂ɖ߂�
        if (!_pools.ContainsKey(prefab)) _pools[prefab] = new Queue<GameObject>();
        _pools[prefab].Enqueue(go);
    }

    static void ResetEffect(GameObject go)
    {
        // ParticleSystem ��S�����X�^�[�g
        var ps = go.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var p in ps)
        {
            p.Clear(true);
            p.Play(true);
        }
        // Light/Animation/Audio�Ȃǂ��g���Ȃ炱���ŏ�����
        var audio = go.GetComponent<AudioSource>();
        if (audio) audio.Play();
    }
}
