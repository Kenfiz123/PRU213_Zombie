using UnityEngine;
using TMPro;

/// <summary>
/// Singleton spawn số damage tại world position.
/// Tự tạo prefab từ code, dùng ObjectPool nếu có.
/// Gắn vào Empty GameObject.
/// </summary>
public class DamageNumberManager : MonoBehaviour
{
    public static DamageNumberManager Instance { get; private set; }

    [Header("═══ CÀI ĐẶT ═══")]
    [Tooltip("Offset Y so với điểm trúng (tránh chìm vào model)")]
    public float spawnOffsetY = 0.5f;
    [Tooltip("Thời gian tồn tại mỗi số")]
    public float lifetime = 1.2f;

    private GameObject dmgNumPrefab;

    void Awake()
    {
        Instance = this;
        CreatePrefab();
    }

    void CreatePrefab()
    {
        // Tạo prefab runtime
        dmgNumPrefab = new GameObject("DmgNumPrefab");

        TextMeshPro tmp = dmgNumPrefab.AddComponent<TextMeshPro>();
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 6;
        tmp.enableWordWrapping = false;
        tmp.sortingOrder = 100;

        dmgNumPrefab.AddComponent<DamageNumber>();

        dmgNumPrefab.SetActive(false);

        // Pre-pool
        if (ObjectPool.Instance != null)
            ObjectPool.Instance.CreatePool("DamageNumber", dmgNumPrefab, 15);
    }

    /// <summary>
    /// Spawn số damage tại vị trí world.
    /// </summary>
    public void Spawn(Vector3 position, float damage, bool isHeadshot)
    {
        Vector3 pos = position + Vector3.up * spawnOffsetY;

        GameObject go;
        if (ObjectPool.Instance != null)
        {
            go = ObjectPool.Instance.Get("DamageNumber", pos, Quaternion.identity);
            if (go == null)
            {
                go = Instantiate(dmgNumPrefab, pos, Quaternion.identity);
                go.SetActive(true);
            }
        }
        else
        {
            go = Instantiate(dmgNumPrefab, pos, Quaternion.identity);
            go.SetActive(true);
        }

        DamageNumber dn = go.GetComponent<DamageNumber>();
        if (dn != null)
            dn.Init(damage, isHeadshot, lifetime);
    }

    void OnDestroy()
    {
        if (dmgNumPrefab != null)
            Destroy(dmgNumPrefab);
        if (Instance == this)
            Instance = null;
    }
}
