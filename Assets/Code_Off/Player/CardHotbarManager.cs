using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Loại skill tương ứng với từng thẻ bài.
/// None = ô trống.
/// </summary>
public enum SkillType
{
    None        = 0,
    FullHealth  = 1,
    FullArmor   = 2,
    Summon      = 3
}

/// <summary>
/// Quản lý ô đựng bài 5 slot ở dưới màn hình.
/// - Phím 1-5: chọn slot (nhấn lại slot đang chọn để bỏ chọn)
/// - Phím E (khi có bài trên tay + không cạnh rương/vật phẩm + đang cầm dao): dùng skill
/// </summary>
public class CardHotbarManager : MonoBehaviour
{
    public static CardHotbarManager Instance { get; private set; }

    // ─────────────────────────────────────────────────────────────
    // THAM CHIẾU
    // ─────────────────────────────────────────────────────────────
    [Header("--- THAM CHIẾU ---")]
    public PlayerSkillManager skillManager;
    public WeaponSwitching weaponSwitching;

    // ─────────────────────────────────────────────────────────────
    // DỮ LIỆU SLOT
    // ─────────────────────────────────────────────────────────────
    [Header("--- DỮ LIỆU HOTBAR ---")]
    [HideInInspector] public SkillType[] slots = new SkillType[3];
    [HideInInspector] public int selectedSlot = -1;

    // ─────────────────────────────────────────────────────────────
    // UI
    // ─────────────────────────────────────────────────────────────
    [Header("--- UI: BACKGROUND MỖI SLOT (5 Image) ---")]
    public Image[] slotBackgrounds = new Image[3];

    [Header("--- UI: ICON THẺ BÀI (3 Image) ---")]
    public Image[] cardIcons = new Image[3];

    [Header("--- UI: VIỀN HIGHLIGHT KHI CHỌN (3 Image) ---")]
    public Image[] selectedHighlights = new Image[3];

    [Header("--- UI: TÊN SKILL ĐANG CHỌN ---")]
    public TextMeshProUGUI skillNameText;

    [Header("--- UI: SPRITE ĐẠI DIỆN TỪNG SKILL ---")]
    [Tooltip("Index 0=None, 1=FullHealth, 2=FullArmor, 3=Summon")]
    public Sprite[] skillSprites = new Sprite[4];

    [Header("--- MÀU SẮC ---")]
    public Color emptySlotColor   = new Color(1f, 1f, 1f, 0.3f);
    public Color filledSlotColor  = Color.white;
    public Color normalBorderColor   = new Color(1f, 1f, 1f, 0f);
    public Color selectedBorderColor = Color.yellow;

    // Cache KeyCode array để tránh if-else chain
    private static readonly KeyCode[] slotKeys = {
        KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3
    };

    // ─────────────────────────────────────────────────────────────
    // UNITY CALLBACKS
    // ─────────────────────────────────────────────────────────────
    void Awake()
    {
        Instance = this;
        for (int i = 0; i < 3; i++) slots[i] = SkillType.None;
    }

    void Start()
    {
        if (skillManager == null)
            skillManager = GetComponent<PlayerSkillManager>();

        if (weaponSwitching == null)
            weaponSwitching = GetComponentInChildren<WeaponSwitching>();

        RefreshUI();
    }

    void Update()
    {
        HandleSlotSelection();
        HandleUseCard();
    }

    // ─────────────────────────────────────────────────────────────
    // CHỌN SLOT (PHÍM 1–5)
    // ─────────────────────────────────────────────────────────────
    void HandleSlotSelection()
    {
        for (int i = 0; i < slotKeys.Length; i++)
        {
            if (Input.GetKeyDown(slotKeys[i]))
            {
                ToggleSlot(i);
                return;
            }
        }
    }

    void ToggleSlot(int index)
    {
        selectedSlot = (selectedSlot == index) ? -1 : index;
        RefreshUI();
    }

    // ─────────────────────────────────────────────────────────────
    // DÙNG SKILL (PHÍM E)
    // ─────────────────────────────────────────────────────────────
    void HandleUseCard()
    {
        if (!Input.GetKeyDown(KeyCode.E)) return;

        // Không có slot nào được chọn
        if (selectedSlot < 0 || selectedSlot >= 3) return;

        // Slot đang chọn trống
        if (slots[selectedSlot] == SkillType.None) return;

        // ƯU TIÊN: rương / vật phẩm gần Player → không dùng skill
        if (InteractionManager.HasNearbyInteraction) return;

        UseCard(selectedSlot);
    }

    // ─────────────────────────────────────────────────────────────
    // KIỂM TRA CẦM SÚNG HAY DAO
    // ─────────────────────────────────────────────────────────────
    bool IsHoldingGun()
    {
        if (weaponSwitching == null) return false;

        foreach (Transform weapon in weaponSwitching.transform)
        {
            if (weapon.gameObject.activeSelf &&
                weapon.GetComponent<PlayerShooting>() != null)
            {
                return true;
            }
        }
        return false;
    }

    // ─────────────────────────────────────────────────────────────
    // THÊM THẺ VÀO HOTBAR (gọi từ CardTrigger khi nhặt bài)
    // ─────────────────────────────────────────────────────────────
    public bool AddCard(SkillType type)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == SkillType.None)
            {
                slots[i] = type;
                RefreshUI();
                Debug.Log($"[Hotbar] Nhặt thẻ {GetSkillName(type)} -> slot {i + 1}");
                return true;
            }
        }

        Debug.Log("[Hotbar] Túi bài đã đầy (5/5)!");
        return false;
    }

    // ─────────────────────────────────────────────────────────────
    // DÙNG THẺ
    // ─────────────────────────────────────────────────────────────
    void UseCard(int index)
    {
        SkillType type = slots[index];

        if (skillManager == null)
        {
            Debug.LogWarning("[Hotbar] skillManager null, không thể dùng skill!");
            return;
        }

        switch (type)
        {
            case SkillType.FullHealth: skillManager.ActivateFullHealth(); break;
            case SkillType.FullArmor:  skillManager.ActivateFullArmor();  break;
            case SkillType.Summon:     skillManager.ActivateSummon();     break;
        }

        slots[index] = SkillType.None;
        selectedSlot = -1;
        RefreshUI();

        Debug.Log($"[Hotbar] Dùng thẻ {GetSkillName(type)} từ slot {index + 1}");
    }

    // ─────────────────────────────────────────────────────────────
    // CẬP NHẬT UI
    // ─────────────────────────────────────────────────────────────
    public void RefreshUI()
    {
        for (int i = 0; i < 3; i++)
        {
            bool hasCard   = slots[i] != SkillType.None;
            bool isSelected = i == selectedSlot;

            // Background slot
            if (slotBackgrounds != null && i < slotBackgrounds.Length && slotBackgrounds[i] != null)
                slotBackgrounds[i].color = hasCard ? filledSlotColor : emptySlotColor;

            // Icon thẻ bài
            if (cardIcons != null && i < cardIcons.Length && cardIcons[i] != null)
            {
                cardIcons[i].gameObject.SetActive(hasCard);

                if (hasCard && skillSprites != null)
                {
                    int spriteIdx = (int)slots[i];
                    if (spriteIdx < skillSprites.Length && skillSprites[spriteIdx] != null)
                        cardIcons[i].sprite = skillSprites[spriteIdx];
                }
            }

            // Viền highlight khi được chọn
            if (selectedHighlights != null && i < selectedHighlights.Length && selectedHighlights[i] != null)
                selectedHighlights[i].color = isSelected ? selectedBorderColor : normalBorderColor;
        }

        // Text tên skill
        if (skillNameText != null)
        {
            if (selectedSlot >= 0 && selectedSlot < 3 && slots[selectedSlot] != SkillType.None)
                skillNameText.text = GetSkillName(slots[selectedSlot]);
            else
                skillNameText.text = string.Empty;
        }
    }

    // ─────────────────────────────────────────────────────────────
    // HELPER
    // ─────────────────────────────────────────────────────────────
    string GetSkillName(SkillType type)
    {
        switch (type)
        {
            case SkillType.FullHealth: return "Hồi Máu";
            case SkillType.FullArmor:  return "Hồi Giáp";
            case SkillType.Summon:     return "Triệu Hồi";
            default:                   return "";
        }
    }
}
