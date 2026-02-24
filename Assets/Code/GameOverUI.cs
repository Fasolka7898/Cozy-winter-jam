using UnityEngine;
using UnityEngine.UI;

public class GameOverUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject gameOverPanel; // Панель с экраном смерти
    [SerializeField] private Button restartButton; // Кнопка рестарта
    [SerializeField] private Button menuButton; // Кнопка выхода в меню
    [SerializeField] private Text scoreText; // Текст с результатом (опционально)
    [SerializeField] private Text highScoreText; // Текст с рекордом (опционально)

    void Start()
    {
        // Скрываем панель при старте игры
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        // Привязываем кнопки
        if (restartButton != null)
            restartButton.onClick.AddListener(RestartGame);

        if (menuButton != null)
            menuButton.onClick.AddListener(GoToMainMenu);
    }

    public void ShowGameOverScreen()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);

            // Опционально: обновляем текст с результатом
            if (scoreText != null)
            {
                // Здесь можно получить текущий счет из менеджера игры
                // scoreText.text = "Score: " + GameManager.Instance.GetCurrentScore();
            }

            if (highScoreText != null)
            {
                // Здесь можно получить рекорд
                // highScoreText.text = "Best: " + GameManager.Instance.GetHighScore();
            }

            // Останавливаем время (опционально)
            Time.timeScale = 0f;
        }
    }

    public void RestartGame()
    {
        Debug.Log("🔄 Restarting game...");
        Time.timeScale = 1f; // Возвращаем время
        // Здесь логика перезапуска сцены или игры
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        );
    }

    public void GoToMainMenu()
    {
        Debug.Log("🏠 Going to main menu...");
        Time.timeScale = 1f;
        // Загрузка главного меню
        UnityEngine.SceneManagement.SceneManager.LoadScene("L_MG_1_E");
    }
}