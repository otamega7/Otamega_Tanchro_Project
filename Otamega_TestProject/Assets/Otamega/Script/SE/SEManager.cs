using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-900)]
public class SEManager : MonoBehaviour
{
    [System.Serializable]
    public class SEEntry
    {
        public string key;                 // ��: "TankFire"
        public AudioClip[] clips;          // �����̌��SE
        [Range(0f, 1f)] public float volume = 1f;
    }

    public static SEManager I { get; private set; }

    [Header("Registry")]
    public List<SEEntry> entries = new();

    [Header("Settings")]
    public int poolSize = 10;              // �����ɖ点�鐔
    public bool dontDestroyOnLoad = true;

    Dictionary<string, SEEntry> _map = new();
    Queue<AudioSource> _pool = new();

    void Awake()
    {
        if (I && I != this) { Destroy(gameObject); return; }
        I = this;
        if (dontDestroyOnLoad) DontDestroyOnLoad(gameObject);

        // �o�^�\���쐬
        _map.Clear();
        foreach (var e in entries)
        {
            if (!string.IsNullOrEmpty(e.key) && e.clips != null && e.clips.Length > 0)
                _map[e.key] = e;
        }

        // �v�[���쐬
        for (int i = 0; i < poolSize; i++)
        {
            var src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 0f; // �f�t�H���g��2D
            _pool.Enqueue(src);
        }
    }

    AudioSource GetSource()
    {
        var src = _pool.Dequeue();
        _pool.Enqueue(src); // �����O�o�b�t�@�I�Ɏg��
        return src;
    }

    /// <summary>
    /// 2D�T�E���h�Đ��i�ʒu�w��Ȃ��j
    /// </summary>
    public void Play(string key)
    {
        if (!_map.TryGetValue(key, out var entry))
        {
            Debug.LogWarning($"[SE] key '{key}' not found");
            return;
        }

        var clip = PickRandomClip(entry);
        if (!clip) return;

        var src = GetSource();
        src.transform.position = Vector3.zero;
        src.spatialBlend = 0f; // 2D
        src.PlayOneShot(clip, entry.volume);
    }

    /// <summary>
    /// 3D�T�E���h�Đ��i�ʒu�w�肠��j
    /// </summary>
    public void Play(string key, Vector3 pos)
    {
        if (!_map.TryGetValue(key, out var entry))
        {
            Debug.LogWarning($"[SE] key '{key}' not found");
            return;
        }

        var clip = PickRandomClip(entry);
        if (!clip) return;

        var src = GetSource();
        src.transform.position = pos;
        src.spatialBlend = 1f; // 3D
        src.minDistance = 2f;
        src.maxDistance = 50f;
        src.dopplerLevel = 0f;
        src.rolloffMode = AudioRolloffMode.Logarithmic;
        src.PlayOneShot(clip, entry.volume);
    }

    AudioClip PickRandomClip(SEEntry entry)
    {
        if (entry.clips == null || entry.clips.Length == 0) return null;
        int index = Random.Range(0, entry.clips.Length);
        return entry.clips[index];
    }

#if UNITY_EDITOR
    // �G�f�B�^�p�f�o�b�O���j���[
    [ContextMenu("SE Test (2D)")]
    void _SE_Test2D()
    {
        if (entries.Count > 0) Play(entries[0].key);
        else Debug.LogWarning("[SE] entries ����ł�");
    }

    [ContextMenu("SE Test (3D at Listener)")]
    void _SE_Test3D()
    {
        var listener = FindObjectOfType<AudioListener>();
        if (!listener) { Debug.LogWarning("[SE] AudioListener ��������܂���"); return; }
        if (entries.Count > 0) Play(entries[0].key, listener.transform.position);
        else Debug.LogWarning("[SE] entries ����ł�");
    }
#endif
}
