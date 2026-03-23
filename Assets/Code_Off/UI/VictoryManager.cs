using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Hiện UI chiến thắng khi Boss chết.
/// Gắn vào Canvas hoặc GameManager.
/// </summary>
public class VictoryManager : MonoBehaviour
{
    [Header("--- UI ---")]
    public GameObject victoryPanel;

    [Header("--- ÂM THANH ---")]
    [Tooltip("Nhạc chiến thắng (kéo AudioClip vào đây)")]
    public AudioClip victoryMusic;
    [Range(0f, 1f)]
    public float victoryVolume = 0.8f;

    private static VictoryManager instance;

    void Awake()
    {
        instance = this;
        if (victoryPanel != null)
            victoryPanel.SetActive(false);
    }

    public static void ShowVictory()
    {
        if (instance == null)
        {
            Debug.LogWarning("[Victory] Không tìm thấy VictoryManager trong scene!");
            return;
        }

        instance.OnVictory();
    }

    void OnVictory()
    {
        Debug.Log("[Victory] CHIẾN THẮNG!");

        // Tắt nhạc nền
        WaveManager.StopBGM();

        if (victoryPanel != null)
            victoryPanel.SetActive(true);

        // Phát nhạc chiến thắng
        if (victoryMusic != null)
        {
            GameObject musicObj = new GameObject("VictoryMusic");
            AudioSource src = musicObj.AddComponent<AudioSource>();
            src.clip = victoryMusic;
            src.volume = victoryVolume;
            src.loop = false;
            src.Play();
        }

        // Dừng game
        Time.timeScale = 0f;

        // Mở khóa chuột
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void QuitGame()
    {
        Debug.Log("Thoát game!");
        Application.Quit();
    }
}
