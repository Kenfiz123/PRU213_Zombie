using UnityEngine;
using UnityEngine.SceneManagement; // Để load lại màn chơi

public class GameOverManager : MonoBehaviour
{
    // Hàm này sẽ gắn vào nút "Chơi Lại"
    public void RestartGame()
    {
        // Trả lại thời gian bình thường (vì lúc chết mình đã set TimeScale = 0)
        Time.timeScale = 1;

        // Load lại Scene hiện tại
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // Hàm này sẽ gắn vào nút "Thoát"
    public void QuitGame()
    {
        Debug.Log("Đã thoát game!");
        Application.Quit(); // Lệnh này chỉ chạy khi Build ra file .exe, trong Editor sẽ không thấy gì
    }
}