using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Aegis.GrenadeSystem.HiEx;

public class InventoryController : MonoBehaviour
{
    [Header("--- PANEL ---")]
    public GameObject inventoryPanel;

    [Header("--- TEXT SỐ LƯỢNG ---")]
    public TMP_Text medkitText;
    public TMP_Text armorText;
    public TMP_Text pistolAmmoText;
    public TMP_Text akAmmoText;
    public TMP_Text grenadeText;

    [Header("--- LỰU ĐẠN ---")]
    public GrenadeSystem grenadeSystem;

    [Header("--- NÚT BẤM (tùy chọn) ---")]
    public Button medkitButton;
    public Button armorButton;

    [Header("--- THANH MÁU/GIÁP HIỆN TẠI ---")]
    public TMP_Text healthStatusText;
    public TMP_Text armorStatusText;

    [Header("--- DATA ---")]
    public PlayerInventory inventoryData;

    private bool isTabHeld = false;

    void Start()
    {
        Time.timeScale = 1f;
        isTabHeld = false;

        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (inventoryData == null)
            inventoryData = FindObjectOfType<PlayerInventory>();
    }

    void OnDestroy()
    {
        Time.timeScale = 1f;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
            OpenMenu();
        else if (Input.GetKeyUp(KeyCode.Tab))
            CloseMenu();

        if (isTabHeld)
            UpdateUIText();
    }

    void OpenMenu()
    {
        isTabHeld = true;
        inventoryPanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 0f;
    }

    void CloseMenu()
    {
        isTabHeld = false;
        inventoryPanel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Time.timeScale = 1f;
        PlayerShooting.BlockInputBriefly();
    }

    void UpdateUIText()
    {
        if (inventoryData == null) return;

        if (medkitText)
            medkitText.text = inventoryData.medkitCount.ToString();

        if (armorText)
            armorText.text = inventoryData.armorPlateCount.ToString();

        if (pistolAmmoText)
            pistolAmmoText.text = inventoryData.pistolAmmoStock.ToString();

        if (akAmmoText)
            akAmmoText.text = inventoryData.akAmmoStock.ToString();

        if (grenadeText && grenadeSystem != null)
            grenadeText.text = grenadeSystem.GrenadeCount.ToString();

        // Cập nhật trạng thái máu/giáp
        if (healthStatusText && inventoryData.playerHealth != null)
        {
            float hp = inventoryData.playerHealth.CurrentHealth;
            float maxHp = inventoryData.playerHealth.maxHealth;
            healthStatusText.text = $"{hp:F0} / {maxHp:F0}";
        }

        if (armorStatusText && inventoryData.playerArmor != null)
        {
            float armor = inventoryData.playerArmor.CurrentArmor;
            float maxArmor = inventoryData.playerArmor.maxArmor;
            armorStatusText.text = $"{armor:F0} / {maxArmor:F0}";
        }

        // Disable nút nếu không dùng được
        if (medkitButton)
            medkitButton.interactable = inventoryData.medkitCount > 0 &&
                inventoryData.playerHealth != null &&
                inventoryData.playerHealth.CurrentHealth < inventoryData.playerHealth.maxHealth;

        if (armorButton)
            armorButton.interactable = inventoryData.armorPlateCount > 0 &&
                inventoryData.playerArmor != null &&
                inventoryData.playerArmor.CurrentArmor < inventoryData.playerArmor.maxArmor;
    }

    public void OnClickMedkit()
    {
        if (inventoryData != null) inventoryData.UseMedkit();
    }

    public void OnClickArmor()
    {
        if (inventoryData != null) inventoryData.UseArmorPlate();
    }
}
