using UnityEngine;
using System.Collections;

public class PlayerSkillManager : MonoBehaviour
{
    [Header("--- THAM CHIẾU ---")]
    public PlayerHealth pHealth;
    public PlayerArmor pArmor;
    [Header("--- PREFAB CẦN THIẾT ---")]
    public GameObject allyPrefab;    // Kéo Prefab nhân vật hỗ trợ vào

    [Header("--- ÂM THANH SKILL ---")]
    public AudioClip healSound;
    public AudioClip armorSound;
    public AudioClip summonSound;
    [Range(0f, 1f)] public float healVolume = 0.8f;
    [Range(0f, 1f)] public float armorVolume = 0.8f;
    [Range(0f, 1f)] public float summonVolume = 1f;

    [Header("--- DUCK NHẠC NỀN ---")]
    [Tooltip("Giảm nhạc nền xuống mức này khi skill phát (0.1 = 10%)")]
    [Range(0f, 1f)] public float bgmDuckLevel = 0.15f;
    [Tooltip("Thời gian fade nhạc nền xuống/lên (giây)")]
    public float duckFadeDuration = 0.3f;
    [Tooltip("Giữ nhạc nền nhỏ bao lâu sau khi skill sound kết thúc (giây)")]
    public float duckHoldExtra = 0.5f;

    private AudioSource audioSource;
    private Coroutine duckRoutine;

    void Start()
    {
        if (pHealth == null) pHealth = GetComponent<PlayerHealth>();
        if (pArmor == null) pArmor = GetComponent<PlayerArmor>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
    }

    // --- CÁC HÀM KÍCH HOẠT THẺ BÀI ---

    public void ActivateFullHealth()
    {
        pHealth.Heal(9999);
        PlaySound(healSound, healVolume);
        Debug.Log("Đã dùng thẻ: FULL MÁU");
    }

    public void ActivateFullArmor()
    {
        pArmor.AddArmor(9999);
        PlaySound(armorSound, armorVolume);
        Debug.Log("Đã dùng thẻ: FULL KHIÊN");
    }

    public void ActivateSummon()
    {
        if (allyPrefab != null)
        {
            Vector3 spawnPos = transform.position + transform.right * 2f;
            Instantiate(allyPrefab, spawnPos, Quaternion.identity);
            PlaySound(summonSound, summonVolume);
            Debug.Log("Đã dùng thẻ: TRIỆU HỒI");
        }
    }

    void PlaySound(AudioClip clip, float volume)
    {
        if (clip == null || audioSource == null) return;

        audioSource.PlayOneShot(clip, volume);

        // Duck nhạc nền
        if (duckRoutine != null) StopCoroutine(duckRoutine);
        duckRoutine = StartCoroutine(DuckBGM(clip.length));
    }

    IEnumerator DuckBGM(float soundDuration)
    {
        // Tìm BGM source từ WaveManager
        AudioSource bgm = FindBGMSource();
        if (bgm == null) yield break;

        float originalVol = bgm.volume;

        // Fade down
        float elapsed = 0f;
        while (elapsed < duckFadeDuration)
        {
            elapsed += Time.deltaTime;
            bgm.volume = Mathf.Lerp(originalVol, originalVol * bgmDuckLevel, elapsed / duckFadeDuration);
            yield return null;
        }
        bgm.volume = originalVol * bgmDuckLevel;

        // Giữ nhỏ trong lúc skill sound phát
        yield return new WaitForSeconds(soundDuration + duckHoldExtra);

        // Fade up
        elapsed = 0f;
        float currentVol = bgm.volume;
        while (elapsed < duckFadeDuration)
        {
            elapsed += Time.deltaTime;
            bgm.volume = Mathf.Lerp(currentVol, originalVol, elapsed / duckFadeDuration);
            yield return null;
        }
        bgm.volume = originalVol;

        duckRoutine = null;
    }

    AudioSource FindBGMSource()
    {
        WaveManager wm = FindFirstObjectByType<WaveManager>();
        if (wm != null && wm.bgmSource != null)
            return wm.bgmSource;
        return null;
    }
}