using UnityEngine;

/// <summary>
/// Quản lý độ khó game. Static để giữ giữa các scene.
/// Được set bởi DifficultySelectUI trước khi game bắt đầu.
/// WaveManager và các script khác đọc giá trị từ đây.
/// </summary>
public static class DifficultyManager
{
    public enum Difficulty { Easy, Normal, Hard, Asian }

    public static Difficulty Current { get; private set; } = Difficulty.Normal;
    public static bool HasSelected { get; private set; } = false;

    // ═══ ZOMBIE MULTIPLIERS ═══
    public static float ZombieHealthMul { get; private set; } = 1f;
    public static float ZombieDamageMul { get; private set; } = 1f;
    public static float ZombieSpeedMul { get; private set; } = 1f;
    public static float ZombieCountMul { get; private set; } = 1f;

    // ═══ PLAYER MULTIPLIERS ═══
    public static float PlayerDamageMul { get; private set; } = 1f;
    public static float PointsMul { get; private set; } = 1f;
    public static float ChestSpawnMul { get; private set; } = 1f;

    // ═══ BOSS ═══
    public static float BossHealthMul { get; private set; } = 1f;
    public static float BossDamageMul { get; private set; } = 1f;

    public static void SetDifficulty(Difficulty diff)
    {
        Current = diff;
        HasSelected = true;

        switch (diff)
        {
            case Difficulty.Easy:
                ZombieHealthMul = 0.7f;
                ZombieDamageMul = 0.6f;
                ZombieSpeedMul = 0.9f;
                ZombieCountMul = 0.7f;
                PlayerDamageMul = 1.3f;
                PointsMul = 1.5f;
                ChestSpawnMul = 1.5f;
                BossHealthMul = 0.6f;
                BossDamageMul = 0.6f;
                break;

            case Difficulty.Normal:
                ZombieHealthMul = 1f;
                ZombieDamageMul = 1f;
                ZombieSpeedMul = 1f;
                ZombieCountMul = 1f;
                PlayerDamageMul = 1f;
                PointsMul = 1f;
                ChestSpawnMul = 1f;
                BossHealthMul = 1f;
                BossDamageMul = 1f;
                break;

            case Difficulty.Hard:
                ZombieHealthMul = 1.5f;
                ZombieDamageMul = 1.5f;
                ZombieSpeedMul = 1.2f;
                ZombieCountMul = 1.4f;
                PlayerDamageMul = 0.8f;
                PointsMul = 2f;
                ChestSpawnMul = 0.7f;
                BossHealthMul = 1.8f;
                BossDamageMul = 1.5f;
                break;

            case Difficulty.Asian:
                ZombieHealthMul = 10f;
                ZombieDamageMul = 10f;
                ZombieSpeedMul = 3f;
                ZombieCountMul = 5f;
                PlayerDamageMul = 0.3f;
                PointsMul = 10f;
                ChestSpawnMul = 0.2f;
                BossHealthMul = 10f;
                BossDamageMul = 10f;
                break;
        }

        Debug.Log($"[Difficulty] Đã chọn: {diff} | ZombieHP x{ZombieHealthMul} | ZombieDMG x{ZombieDamageMul} | PlayerDMG x{PlayerDamageMul}");
    }

    public static string GetName()
    {
        switch (Current)
        {
            case Difficulty.Easy: return "DỄ";
            case Difficulty.Normal: return "BÌNH THƯỜNG";
            case Difficulty.Hard: return "KHÓ";
            case Difficulty.Asian: return "ASIAN";
            default: return "???";
        }
    }

    public static Color GetColor()
    {
        switch (Current)
        {
            case Difficulty.Easy: return new Color(0.3f, 0.85f, 0.4f);
            case Difficulty.Normal: return new Color(1f, 0.85f, 0.2f);
            case Difficulty.Hard: return new Color(1f, 0.2f, 0.15f);
            case Difficulty.Asian: return new Color(0.8f, 0.05f, 0.9f);
            default: return Color.white;
        }
    }
}
