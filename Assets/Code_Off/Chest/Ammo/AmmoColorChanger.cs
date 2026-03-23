using UnityEngine;

public class AmmoColorChanger : MonoBehaviour
{
    public enum AmmoType { Pistol, AK }
    public AmmoType thisAmmoType;

    [Header("--- MÀU SẮC ---")]
    public Color pistolColor = Color.green; // Mặc định là Xanh Lục
    public Color akColor = new Color(1f, 0.5f, 0f); // Màu Cam (RGB: 1, 0.5, 0)

    private Renderer rend;

    void Start()
    {
        rend = GetComponent<Renderer>();
        if (rend != null)
        {
            // Tạo một bản sao Material để không ảnh hưởng các hộp đạn khác
            rend.material = new Material(rend.material);

            // Đổi màu dựa trên loại đạn
            if (thisAmmoType == AmmoType.Pistol)
            {
                // Đổi màu chính (Albedo/Base Map)
                rend.material.color = pistolColor;
                // Nếu muốn nó phát sáng nhẹ thì uncomment dòng dưới
                // rend.material.SetColor("_EmissionColor", pistolColor * 0.5f);
            }
            else if (thisAmmoType == AmmoType.AK)
            {
                rend.material.color = akColor;
                // rend.material.SetColor("_EmissionColor", akColor * 0.5f);
            }
        }
    }
}