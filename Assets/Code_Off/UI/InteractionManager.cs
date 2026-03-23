using System.Collections.Generic;

/// <summary>
/// Static class quản lý ưu tiên phím E.
/// Các script nhặt đồ / mở rương gọi Register()/Unregister() khi Player vào/rời vùng.
/// CardHotbarManager kiểm tra HasNearbyInteraction trước khi dùng skill.
/// </summary>
public static class InteractionManager
{
    // Dùng HashSet để tránh lỗi đăng ký trùng
    private static readonly HashSet<int> _nearbySet = new HashSet<int>();

    /// <summary>Trả về true nếu đang có rương / vật phẩm gần Player (E ưu tiên cho chúng)</summary>
    public static bool HasNearbyInteraction => _nearbySet.Count > 0;

    /// <summary>Gọi khi Player vào vùng tương tác (OnTriggerEnter hoặc phát hiện gần)</summary>
    public static void Register(int instanceId)
    {
        _nearbySet.Add(instanceId);
    }

    /// <summary>Gọi khi Player rời vùng tương tác (OnTriggerExit hoặc vật phẩm bị Destroy)</summary>
    public static void Unregister(int instanceId)
    {
        _nearbySet.Remove(instanceId);
    }
}
