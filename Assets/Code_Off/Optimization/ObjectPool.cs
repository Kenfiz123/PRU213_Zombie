using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Object Pooling System - Tái sử dụng object thay vì Instantiate/Destroy.
/// Giảm GC (Garbage Collection) → tăng FPS đáng kể.
/// Gắn vào 1 Empty GameObject trong scene.
/// </summary>
public class ObjectPool : MonoBehaviour
{
    public static ObjectPool Instance { get; private set; }

    [System.Serializable]
    public class PoolConfig
    {
        public string poolName;
        public GameObject prefab;
        [Tooltip("Số object tạo sẵn khi Start")]
        public int preloadCount = 5;
        [Tooltip("Tự tăng pool nếu hết object")]
        public bool canExpand = true;
    }

    [Header("--- CẤU HÌNH POOL ---")]
    public PoolConfig[] pools;

    // poolName → Queue<GameObject>
    private Dictionary<string, Queue<GameObject>> poolDict = new Dictionary<string, Queue<GameObject>>();
    // prefab instanceID → poolName (để ReturnToPool biết trả về đâu)
    private Dictionary<int, string> prefabToPool = new Dictionary<int, string>();
    // instance → poolName
    private Dictionary<int, string> instanceToPool = new Dictionary<int, string>();

    // Fallback: prefab → poolName (cho Get bằng prefab)
    private Dictionary<int, string> prefabIdToPool = new Dictionary<int, string>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        InitializePools();
    }

    void InitializePools()
    {
        foreach (var config in pools)
        {
            if (config.prefab == null || string.IsNullOrEmpty(config.poolName))
                continue;

            Queue<GameObject> queue = new Queue<GameObject>();

            for (int i = 0; i < config.preloadCount; i++)
            {
                GameObject obj = CreateNewObject(config.prefab, config.poolName);
                queue.Enqueue(obj);
            }

            poolDict[config.poolName] = queue;
            prefabIdToPool[config.prefab.GetInstanceID()] = config.poolName;
        }
    }

    GameObject CreateNewObject(GameObject prefab, string poolName)
    {
        GameObject obj = Instantiate(prefab, transform);
        obj.SetActive(false);

        // Thêm component để tự trả về pool
        AutoReturn ar = obj.GetComponent<AutoReturn>();
        if (ar == null)
            ar = obj.AddComponent<AutoReturn>();
        ar.poolName = poolName;

        instanceToPool[obj.GetInstanceID()] = poolName;
        return obj;
    }

    /// <summary>
    /// Lấy object từ pool bằng tên
    /// </summary>
    public GameObject Get(string poolName, Vector3 position, Quaternion rotation)
    {
        if (!poolDict.ContainsKey(poolName))
        {
            Debug.LogWarning($"[ObjectPool] Pool '{poolName}' không tồn tại!");
            return null;
        }

        Queue<GameObject> queue = poolDict[poolName];
        GameObject obj = null;

        // Tìm object không null và inactive
        while (queue.Count > 0)
        {
            obj = queue.Dequeue();
            if (obj != null) break;
            obj = null;
        }

        if (obj == null)
        {
            // Tìm config để tạo mới
            PoolConfig config = FindConfig(poolName);
            if (config != null && config.canExpand)
            {
                obj = CreateNewObject(config.prefab, poolName);
            }
            else
            {
                Debug.LogWarning($"[ObjectPool] Pool '{poolName}' hết object!");
                return null;
            }
        }

        obj.transform.position = position;
        obj.transform.rotation = rotation;
        obj.SetActive(true);
        return obj;
    }

    /// <summary>
    /// Lấy object từ pool bằng prefab reference
    /// </summary>
    public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        int prefabId = prefab.GetInstanceID();
        if (prefabIdToPool.TryGetValue(prefabId, out string poolName))
        {
            return Get(poolName, position, rotation);
        }

        // Không có trong pool → fallback Instantiate bình thường
        return Instantiate(prefab, position, rotation);
    }

    /// <summary>
    /// Trả object về pool (thay vì Destroy)
    /// </summary>
    public void ReturnToPool(GameObject obj)
    {
        if (obj == null) return;

        int id = obj.GetInstanceID();
        if (instanceToPool.TryGetValue(id, out string poolName))
        {
            obj.SetActive(false);
            obj.transform.SetParent(transform);
            if (poolDict.ContainsKey(poolName))
                poolDict[poolName].Enqueue(obj);
        }
        else
        {
            // Không thuộc pool nào → destroy bình thường
            Destroy(obj);
        }
    }

    /// <summary>
    /// Trả object về pool sau delay giây (thay thế Destroy(obj, delay))
    /// </summary>
    public void ReturnToPool(GameObject obj, float delay)
    {
        if (obj == null) return;
        AutoReturn ar = obj.GetComponent<AutoReturn>();
        if (ar != null)
        {
            ar.ReturnAfterDelay(delay);
        }
        else
        {
            Destroy(obj, delay);
        }
    }

    /// <summary>
    /// Tạo pool mới từ code (runtime). Dùng cho DamageNumber, v.v.
    /// </summary>
    public void CreatePool(string poolName, GameObject prefab, int preloadCount)
    {
        if (poolDict.ContainsKey(poolName)) return;

        Queue<GameObject> queue = new Queue<GameObject>();
        for (int i = 0; i < preloadCount; i++)
        {
            GameObject obj = CreateNewObject(prefab, poolName);
            queue.Enqueue(obj);
        }
        poolDict[poolName] = queue;
        prefabIdToPool[prefab.GetInstanceID()] = poolName;

        // Tạo config giả để expand khi hết
        var configList = new List<PoolConfig>(pools);
        configList.Add(new PoolConfig
        {
            poolName = poolName,
            prefab = prefab,
            preloadCount = preloadCount,
            canExpand = true
        });
        pools = configList.ToArray();
    }

    PoolConfig FindConfig(string poolName)
    {
        foreach (var c in pools)
        {
            if (c.poolName == poolName) return c;
        }
        return null;
    }
}
