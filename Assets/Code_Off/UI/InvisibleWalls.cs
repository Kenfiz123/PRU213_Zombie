using UnityEngine;

/// <summary>
/// Tự tạo 4 bức tường trong suốt xung quanh Plane.
/// Gắn vào Plane (mặt đường).
/// </summary>
public class InvisibleWalls : MonoBehaviour
{
    [Tooltip("Chiều cao tường")]
    public float wallHeight = 10f;
    [Tooltip("Độ dày tường")]
    public float wallThickness = 1f;

    void Start()
    {
        // Lấy kích thước thực của Plane (Plane mặc định 10x10, scale nhân thêm)
        Vector3 scale = transform.lossyScale;
        float sizeX = 10f * scale.x; // Plane mặc định width = 10 * scaleX
        float sizeZ = 10f * scale.z;
        Vector3 center = transform.position;

        // 4 tường: Trước, Sau, Trái, Phải
        CreateWall("Wall_Front", center + new Vector3(0, wallHeight / 2f, sizeZ / 2f),
            new Vector3(sizeX + wallThickness * 2, wallHeight, wallThickness));

        CreateWall("Wall_Back", center + new Vector3(0, wallHeight / 2f, -sizeZ / 2f),
            new Vector3(sizeX + wallThickness * 2, wallHeight, wallThickness));

        CreateWall("Wall_Left", center + new Vector3(-sizeX / 2f, wallHeight / 2f, 0),
            new Vector3(wallThickness, wallHeight, sizeZ + wallThickness * 2));

        CreateWall("Wall_Right", center + new Vector3(sizeX / 2f, wallHeight / 2f, 0),
            new Vector3(wallThickness, wallHeight, sizeZ + wallThickness * 2));
    }

    void CreateWall(string name, Vector3 position, Vector3 size)
    {
        GameObject wall = new GameObject(name);
        wall.transform.position = position;
        wall.transform.SetParent(transform);

        BoxCollider col = wall.AddComponent<BoxCollider>();
        col.size = size;
        col.isTrigger = false;
    }
}
