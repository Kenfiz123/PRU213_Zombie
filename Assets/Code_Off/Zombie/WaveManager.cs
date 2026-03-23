using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class WaveManager : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════════════
    // PHASE CONFIG
    // ═══════════════════════════════════════════════════════════════
    [System.Serializable]
    public class PhaseConfig
    {
        [Header("--- THÔNG TIN PHASE ---")]
        public string phaseName = "Phase 1";
        public int totalWaves = 3;

        [Header("--- ZOMBIE ---")]
        public GameObject[] zombiePrefabs;
        public int startZombieCount = 3;
        public int increasePerWave = 2;

        [Header("--- BUFF ZOMBIE (nhân với giá trị gốc) ---")]
        public float speedMultiplier = 1f;
        public float healthMultiplier = 1f;
        public float damageMultiplier = 1f;
        [Tooltip("Thấp hơn = đánh nhanh hơn")]
        public float attackSpeedMultiplier = 1f;

        [Header("--- SPAWN ---")]
        public Transform[] spawnPoints;
        public float timeBetweenSpawns = 1f;
        public float timeBetweenWaves = 5f;

        [Header("--- RƯƠNG PHASE NÀY ---")]
        [Tooltip("Tổng số rương được spawn trong suốt phase này")]
        public int maxChestsInPhase = 10;
    }

    // ═══════════════════════════════════════════════════════════════
    // INSPECTOR
    // ═══════════════════════════════════════════════════════════════
    [Header("═══ 3 PHASE CẤU HÌNH ═══")]
    public PhaseConfig[] phases = new PhaseConfig[3];

    [Header("═══ BOSS ═══")]
    public GameObject bossPrefab;
    public Transform bossSpawnPoint;
    public float bossSpawnDelay = 3f;

    [Header("═══ RƯƠNG SPAWN ═══")]
    public GameObject chestPrefab;
    public Transform[] chestSpawnPoints;
    [Tooltip("Thời gian giữa mỗi lần spawn rương (giây)")]
    public float chestSpawnInterval = 30f;
    [Tooltip("Số rương mỗi lần spawn")]
    public int chestsPerSpawn = 2;
    [Tooltip("Số rương tối đa trên map cùng lúc")]
    public int maxChestsOnMap = 5;

    [Header("═══ NGÀY ĐÊM ═══")]
    [Tooltip("Kéo Directional Light (có DayNightCycle) vào đây")]
    public DayNightCycle dayNightCycle;

    [Header("═══ NHẠC NỀN ═══")]
    [Tooltip("Nhạc nền bình thường (Phase 1-3)")]
    public AudioClip normalBGM;
    [Tooltip("Nhạc nền khi Boss xuất hiện")]
    public AudioClip bossBGM;
    [Tooltip("Tiếng gầm/roar khi Boss spawn")]
    public AudioClip bossRoarSFX;
    [Range(0f, 1f)]
    public float roarVolume = 1f;
    [Tooltip("AudioSource phát nhạc nền (tự tạo nếu bỏ trống)")]
    public AudioSource bgmSource;
    [Range(0f, 1f)]
    public float bgmVolume = 0.5f;
    [Tooltip("Thời gian fade chuyển nhạc (giây)")]
    public float bgmFadeDuration = 2f;

    [Header("═══ UI ═══")]
    public Text waveText;
    public Text phaseText;

    // ═══════════════════════════════════════════════════════════════
    // STATE
    // ═══════════════════════════════════════════════════════════════
    private int currentPhase = 0;
    private int currentWave = 0;
    private bool isWaveActive = false;
    private bool isSpawning = false;
    private bool isBossPhase = false;
    private Coroutine chestSpawnCoroutine;

    // Đếm số rương đã spawn trong phase hiện tại
    private int chestsSpawnedInPhase = 0;

    /// <summary>Các script khác kiểm tra biến này để biết có đang boss phase không</summary>
    public static bool IsBossPhase { get; private set; }

    /// <summary>Tắt nhạc nền (gọi khi thắng/thua)</summary>
    public static void StopBGM()
    {
        if (instance != null && instance.bgmSource != null)
        {
            instance.StartCoroutine(instance.FadeOutBGM());
        }
    }

    private static WaveManager instance;

    // ═══════════════════════════════════════════════════════════════
    // UNITY CALLBACKS
    // ═══════════════════════════════════════════════════════════════
    void Start()
    {
        instance = this;
        IsBossPhase = false;

        // Tự tạo AudioSource nếu chưa gán
        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
            bgmSource.volume = bgmVolume;
        }

        StartCoroutine(WaitForDifficultyThenStart());
    }

    IEnumerator WaitForDifficultyThenStart()
    {
        // Đợi người chơi chọn độ khó
        while (!DifficultyManager.HasSelected)
            yield return null;

        // Phát nhạc nền bình thường
        if (normalBGM != null)
        {
            bgmSource.clip = normalBGM;
            bgmSource.volume = bgmVolume;
            bgmSource.Play();
        }

        chestSpawnCoroutine = StartCoroutine(ChestSpawnLoop());
        StartCoroutine(StartNextWave());
    }

    void Update()
    {
        if (isBossPhase || !isWaveActive || isSpawning) return;

        // Dùng TargetRegistry cached thay vì FindGameObjectsWithTag mỗi frame
        int count;
        if (TargetRegistry.Instance != null)
        {
            count = TargetRegistry.EnemyCount;
        }
        else
        {
            // Fallback: chỉ check mỗi 30 frame thay vì mỗi frame
            if (Time.frameCount % 30 != 0) return;
            count = GameObject.FindGameObjectsWithTag("Enemy").Length;
        }

        if (count == 0)
        {
            isWaveActive = false;
            StartCoroutine(StartNextWave());
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // WAVE LOGIC
    // ═══════════════════════════════════════════════════════════════
    IEnumerator StartNextWave()
    {
        if (currentPhase >= phases.Length)
        {
            yield return StartCoroutine(SpawnBoss());
            yield break;
        }

        PhaseConfig phase = phases[currentPhase];

        if (currentWave >= phase.totalWaves)
        {
            currentPhase++;
            currentWave = 0;
            chestsSpawnedInPhase = 0; // Reset đếm rương cho phase mới

            if (currentPhase >= phases.Length)
            {
                yield return StartCoroutine(SpawnBoss());
                yield break;
            }

            phase = phases[currentPhase];
        }

        UpdateUI(phase);

        // Tối dần theo phase
        if (dayNightCycle != null)
            dayNightCycle.OnPhaseChanged(currentPhase);

        Debug.Log($"[WaveManager] {phase.phaseName} - Wave {currentWave + 1}/{phase.totalWaves}");

        yield return new WaitForSeconds(phase.timeBetweenWaves);

        if (phaseText != null) phaseText.gameObject.SetActive(false);
        if (waveText != null) waveText.gameObject.SetActive(false);

        int zombieCount = Mathf.RoundToInt((phase.startZombieCount + currentWave * phase.increasePerWave) * DifficultyManager.ZombieCountMul);

        Transform[] spawns = (phase.spawnPoints != null && phase.spawnPoints.Length > 0)
            ? phase.spawnPoints
            : phases[0].spawnPoints;

        currentWave++;
        isSpawning = true;

        for (int i = 0; i < zombieCount; i++)
        {
            SpawnZombie(phase, spawns);
            yield return new WaitForSeconds(phase.timeBetweenSpawns);
        }

        isSpawning = false;
        isWaveActive = true;
    }

    void SpawnZombie(PhaseConfig phase, Transform[] spawns)
    {
        if (spawns == null || spawns.Length == 0) return;
        if (phase.zombiePrefabs == null || phase.zombiePrefabs.Length == 0) return;

        GameObject prefab = phase.zombiePrefabs[Random.Range(0, phase.zombiePrefabs.Length)];
        Transform point = spawns[Random.Range(0, spawns.Length)];

        GameObject zombie = Instantiate(prefab, point.position, point.rotation);

        ApplyZombieBuff(zombie, phase);
    }

    void ApplyZombieBuff(GameObject zombie, PhaseConfig phase)
    {
        // Lấy multiplier từ Difficulty
        float diffHP = DifficultyManager.ZombieHealthMul;
        float diffDMG = DifficultyManager.ZombieDamageMul;
        float diffSPD = DifficultyManager.ZombieSpeedMul;

        // Buff máu
        ZombieHealth hp = zombie.GetComponent<ZombieHealth>();
        if (hp != null)
        {
            hp.maxHealth = hp.maxHealth * phase.healthMultiplier * diffHP;
            hp.currentHealth = hp.maxHealth;
        }

        // Buff tốc độ + damage + tốc đánh + BẬT AGGRESSIVE MODE
        ZombieAI ai = zombie.GetComponent<ZombieAI>();
        if (ai != null)
        {
            ai.aggressiveMode = true;
            ai.chaseSpeed *= phase.speedMultiplier * diffSPD;
            ai.patrolSpeed *= phase.speedMultiplier * diffSPD;
            ai.damage = Mathf.RoundToInt(ai.damage * phase.damageMultiplier * diffDMG);
            ai.attackCooldown *= phase.attackSpeedMultiplier;
        }

        // Buff cho RangedZombieAI nếu có
        RangedZombieAI ranged = zombie.GetComponent<RangedZombieAI>();
        if (ranged != null)
        {
        }

        // Buff cho ExplodingZombieAI nếu có
        ExplodingZombieAI exploding = zombie.GetComponent<ExplodingZombieAI>();
        if (exploding != null)
        {
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // BOSS
    // ═══════════════════════════════════════════════════════════════
    IEnumerator SpawnBoss()
    {
        isBossPhase = true;
        IsBossPhase = true;

        // Boss = tối hoàn toàn
        if (dayNightCycle != null)
            dayNightCycle.OnPhaseChanged(4);

        // Fog chuyển boss (đỏ tối, đặc)
        if (FogManager.instance != null)
            FogManager.instance.SetBossPhase(true);

        if (phaseText != null)
        {
            phaseText.text = "BOSS INCOMING!";
            phaseText.gameObject.SetActive(true);
        }
        if (waveText != null) waveText.gameObject.SetActive(false);

        Debug.Log("[WaveManager] BOSS ĐANG ĐẾN!");

        // Fade out nhạc cũ → chuyển sang nhạc boss
        if (bossBGM != null && bgmSource != null)
        {
            StartCoroutine(CrossfadeBGM(bossBGM));
        }

        yield return new WaitForSeconds(bossSpawnDelay);

        if (phaseText != null) phaseText.gameObject.SetActive(false);

        if (bossPrefab != null && bossSpawnPoint != null)
        {
            GameObject bossObj = Instantiate(bossPrefab, bossSpawnPoint.position, bossSpawnPoint.rotation);

            // Buff boss theo difficulty
            ZombieHealth bossHP = bossObj.GetComponent<ZombieHealth>();
            if (bossHP != null)
            {
                bossHP.maxHealth *= DifficultyManager.BossHealthMul;
                bossHP.currentHealth = bossHP.maxHealth;

                // Hiện thanh máu Boss
                if (BossHealthBar.Instance != null)
                    BossHealthBar.Instance.Show(bossHP, "BOSS");
            }

            // Tiếng gầm boss
            if (bossRoarSFX != null)
                AudioSource.PlayClipAtPoint(bossRoarSFX, bossSpawnPoint.position, roarVolume);

            Debug.Log("[WaveManager] BOSS ĐÃ XUẤT HIỆN!");
        }
        else
        {
            Debug.LogWarning("[WaveManager] Thiếu bossPrefab hoặc bossSpawnPoint!");
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // CHEST SPAWN
    // ═══════════════════════════════════════════════════════════════
    IEnumerator ChestSpawnLoop()
    {
        // Difficulty ảnh hưởng tốc độ spawn rương (ChestSpawnMul > 1 = spawn nhanh hơn)
        float interval = chestSpawnInterval / Mathf.Max(0.1f, DifficultyManager.ChestSpawnMul);
        yield return new WaitForSeconds(interval);

        while (true)
        {
            SpawnChests();
            yield return new WaitForSeconds(interval);
        }
    }

    void SpawnChests()
    {
        if (chestPrefab == null || chestSpawnPoints == null || chestSpawnPoints.Length == 0)
            return;

        // Boss phase: không giới hạn số rương
        // Phase thường: giới hạn theo maxChestsInPhase
        if (!isBossPhase)
        {
            if (currentPhase >= phases.Length) return;
            PhaseConfig phase = phases[currentPhase];
            int remaining = phase.maxChestsInPhase - chestsSpawnedInPhase;
            if (remaining <= 0) return;
        }

        // Lấy danh sách rương hiện có trên map
        LootChest[] existingChests = FindObjectsByType<LootChest>(FindObjectsSortMode.None);
        int currentChests = existingChests.Length;

        int canSpawn = Mathf.Min(chestsPerSpawn, maxChestsOnMap - currentChests);

        // Giới hạn theo phase (trừ boss phase)
        if (!isBossPhase && currentPhase < phases.Length)
        {
            int remaining = phases[currentPhase].maxChestsInPhase - chestsSpawnedInPhase;
            canSpawn = Mathf.Min(canSpawn, remaining);
        }

        if (canSpawn <= 0) return;

        // Lọc ra các spawn point KHÔNG có rương gần đó (trong 2m)
        List<int> availableIndices = new List<int>();
        for (int i = 0; i < chestSpawnPoints.Length; i++)
        {
            bool occupied = false;
            foreach (var chest in existingChests)
            {
                if (Vector3.Distance(chestSpawnPoints[i].position, chest.transform.position) < 2f)
                {
                    occupied = true;
                    break;
                }
            }
            if (!occupied) availableIndices.Add(i);
        }

        for (int i = 0; i < canSpawn && availableIndices.Count > 0; i++)
        {
            int randIdx = Random.Range(0, availableIndices.Count);
            Transform point = chestSpawnPoints[availableIndices[randIdx]];
            availableIndices.RemoveAt(randIdx);

            Instantiate(chestPrefab, point.position, point.rotation);
            chestsSpawnedInPhase++;
        }

        Debug.Log($"[WaveManager] Spawn {canSpawn} rương (phase: {chestsSpawnedInPhase}/{(isBossPhase ? "∞" : phases[currentPhase].maxChestsInPhase.ToString())})");
    }

    // ═══════════════════════════════════════════════════════════════
    // UI
    // ═══════════════════════════════════════════════════════════════
    void UpdateUI(PhaseConfig phase)
    {
        if (phaseText != null)
        {
            phaseText.text = phase.phaseName;
            phaseText.gameObject.SetActive(true);
        }

        if (waveText != null)
        {
            waveText.text = $"Wave {currentWave + 1}/{phase.totalWaves}";
            waveText.gameObject.SetActive(true);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // BGM CROSSFADE
    // ═══════════════════════════════════════════════════════════════
    IEnumerator FadeOutBGM()
    {
        if (bgmSource == null || !bgmSource.isPlaying) yield break;

        float startVol = bgmSource.volume;
        float elapsed = 0f;
        while (elapsed < bgmFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime; // unscaled vì TimeScale=0 khi thắng/thua
            bgmSource.volume = Mathf.Lerp(startVol, 0f, elapsed / bgmFadeDuration);
            yield return null;
        }

        bgmSource.Stop();
        bgmSource.volume = 0f;
    }

    IEnumerator CrossfadeBGM(AudioClip newClip)
    {
        float startVol = bgmSource.volume;

        // Fade out nhạc cũ
        float elapsed = 0f;
        while (elapsed < bgmFadeDuration)
        {
            elapsed += Time.deltaTime;
            bgmSource.volume = Mathf.Lerp(startVol, 0f, elapsed / bgmFadeDuration);
            yield return null;
        }

        // Đổi clip & fade in nhạc mới
        bgmSource.Stop();
        bgmSource.clip = newClip;
        bgmSource.Play();

        elapsed = 0f;
        while (elapsed < bgmFadeDuration)
        {
            elapsed += Time.deltaTime;
            bgmSource.volume = Mathf.Lerp(0f, bgmVolume, elapsed / bgmFadeDuration);
            yield return null;
        }

        bgmSource.volume = bgmVolume;
    }
}
