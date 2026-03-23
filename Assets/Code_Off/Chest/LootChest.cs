using UnityEngine;
using System.Collections.Generic;

public class LootChest : MonoBehaviour
{
    [Header("--- CÀI ĐẶT RƯƠNG ---")]
    public Animator animator;
    public Transform spawnPoint;
    [SerializeField] private Vector3 lootSpawnOffset = new Vector3(0f, 1.0f, 0f);

    [Header("--- DANH SÁCH ĐỒ (Kéo Prefab vào đây) ---")]
    public GameObject ammoPistolPrefab;
    public GameObject ammoAKPrefab;
    public GameObject healthPrefab;
    public GameObject armorPrefab;

    [Header("--- THẺ SKILL (Kéo Prefab thẻ bài vào) ---")]
    public GameObject cardHealPrefab;
    public GameObject cardArmorPrefab;
    public GameObject cardSummonPrefab;

    [Header("--- UI NHẮC NHỞ ---")]
    public GameObject pressEPrompt;

    private bool isOpened = false;
    private bool isPlayerNearby = false;
    private GameObject spawnedLoot = null;

    void Start()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (spawnPoint == null) spawnPoint = transform;
        if (pressEPrompt != null) pressEPrompt.SetActive(false);
    }

    // Static flag: chỉ cho phép 1 rương mở mỗi lần ấn E
    private static bool chestOpenedThisFrame = false;
    private static int lastOpenFrame = -1;

    void Update()
    {
        // Reset flag mỗi frame mới
        if (Time.frameCount != lastOpenFrame)
            chestOpenedThisFrame = false;

        if (isPlayerNearby && !isOpened && !chestOpenedThisFrame && Input.GetKeyDown(KeyCode.E))
        {
            chestOpenedThisFrame = true;
            lastOpenFrame = Time.frameCount;
            OpenChest();
        }

        // Rương đã mở + vật phẩm đã bị nhặt → xóa rương
        if (isOpened && spawnedLoot == null && Time.frameCount > lastOpenFrame + 10)
        {
            Destroy(gameObject);
        }
    }

    void OpenChest()
    {
        isOpened = true;

        if (animator != null) animator.SetTrigger("Open");
        if (pressEPrompt != null) pressEPrompt.SetActive(false);

        Collider chestCollider = GetComponent<Collider>();
        if (chestCollider != null) chestCollider.enabled = false;

        InteractionManager.Unregister(GetInstanceID());

        Invoke("SpawnRandomLoot", 0.1f);
    }

    void SpawnRandomLoot()
    {
        List<GameObject> potentialLoot = new List<GameObject>();

        if (ammoPistolPrefab != null) potentialLoot.Add(ammoPistolPrefab);
        if (ammoAKPrefab != null) potentialLoot.Add(ammoAKPrefab);
        if (healthPrefab != null) potentialLoot.Add(healthPrefab);
        if (armorPrefab != null) potentialLoot.Add(armorPrefab);

        // Boss phase → không mở ra thẻ bài
        if (!WaveManager.IsBossPhase)
        {
            if (cardHealPrefab != null) potentialLoot.Add(cardHealPrefab);
            if (cardArmorPrefab != null) potentialLoot.Add(cardArmorPrefab);

            if (cardSummonPrefab != null && CanSpawnSummonCard())
                potentialLoot.Add(cardSummonPrefab);
        }

        if (potentialLoot.Count > 0 && spawnPoint != null)
        {
            int randomIndex = Random.Range(0, potentialLoot.Count);
            GameObject selectedItem = potentialLoot[randomIndex];

            Vector3 spawnPos = spawnPoint.position + lootSpawnOffset;
            spawnedLoot = Instantiate(selectedItem, spawnPos, Quaternion.identity);

            // Đánh dấu nếu vừa spawn thẻ summon
            if (selectedItem == cardSummonPrefab)
                summonCardExists = true;

            Debug.Log("Rương cho: " + selectedItem.name);
        }
    }

    // Static flag: chỉ cho 1 thẻ summon tồn tại trên map cùng lúc
    private static bool summonCardExists = false;

    public static void ResetSummonCardFlag()
    {
        summonCardExists = false;
    }

    bool CanSpawnSummonCard()
    {
        // Đã có thẻ summon trên map (vừa spawn từ rương khác)
        if (summonCardExists) return false;

        // Kiểm tra hotbar có thẻ summon chưa
        if (CardHotbarManager.Instance != null)
        {
            for (int i = 0; i < CardHotbarManager.Instance.slots.Length; i++)
            {
                if (CardHotbarManager.Instance.slots[i] == SkillType.Summon)
                    return false;
            }
        }

        // Kiểm tra có ally đang sống không (tìm bằng layer "Ally")
        int allyLayer = LayerMask.NameToLayer("Ally");
        foreach (var obj in GameObject.FindObjectsByType<AllyController>(FindObjectsSortMode.None))
        {
            if (obj.gameObject.layer == allyLayer)
                return false;
        }

        return true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isOpened)
        {
            isPlayerNearby = true;
            InteractionManager.Register(GetInstanceID());
            if (pressEPrompt != null) pressEPrompt.SetActive(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = false;
            InteractionManager.Unregister(GetInstanceID());
            if (pressEPrompt != null) pressEPrompt.SetActive(false);
        }
    }

    void OnDestroy()
    {
        InteractionManager.Unregister(GetInstanceID());
    }
}
