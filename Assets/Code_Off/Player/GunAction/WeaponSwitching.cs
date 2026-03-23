using UnityEngine;

public class WeaponSwitching : MonoBehaviour
{
    public int selectedWeapon = 0; // 0 là súng đầu tiên, 1 là súng thứ 2...

    void Start()
    {
        SelectWeapon();
    }

    void Update()
    {
        // Không cho đổi vũ khí khi đang Last Stand
        if (LastStandManager.IsInLastStand) return;

        int previousSelectedWeapon = selectedWeapon;

        // 1. LĂN CHUỘT LÊN (Đổi súng kế tiếp)
        if (Input.GetAxis("Mouse ScrollWheel") > 0f)
        {
            if (selectedWeapon >= transform.childCount - 1)
                selectedWeapon = 0;
            else
                selectedWeapon++;
        }

        // 2. LĂN CHUỘT XUỐNG (Đổi súng trước đó)
        if (Input.GetAxis("Mouse ScrollWheel") < 0f)
        {
            if (selectedWeapon <= 0)
                selectedWeapon = transform.childCount - 1;
            else
                selectedWeapon--;
        }

        // 3. PHÍM SỐ 1-2 đã được dùng cho CardHotbar → chỉ dùng scroll wheel để đổi vũ khí

        // Nếu có sự thay đổi thì mới cập nhật
        if (previousSelectedWeapon != selectedWeapon)
        {
            SelectWeapon();
        }
    }

    public void SelectWeapon()
    {
        int i = 0;
        // Duyệt qua tất cả súng con nằm trong WeaponHolder
        foreach (Transform weapon in transform)
        {
            // Nếu index trùng với súng đang chọn -> Bật (True)
            // Nếu không trùng -> Tắt (False)
            if (i == selectedWeapon)
                weapon.gameObject.SetActive(true);
            else
                weapon.gameObject.SetActive(false);
            i++;
        }
    }
}