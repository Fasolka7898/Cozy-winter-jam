using UnityEngine;
using UnityEngine.SceneManagement;

public class TeleportToScene : MonoBehaviour
{
    [Header("Настройки телепортации")]
    [SerializeField] private int sceneIndex = 1; // Индекс сцены для перехода
    [SerializeField] private string teleportTag = "Player"; // Тег игрока
    [SerializeField] private bool useIndex = true; // Использовать индекс или название сцены
    [SerializeField] private string sceneName = "SceneName"; // Название сцены

    private void OnTriggerEnter(Collider other)
    {
        // Проверяем, вошел ли игрок в коллайдер
        if (other.CompareTag(teleportTag))
        {
            TeleportToTargetScene();
        }
    }

    // Для 2D коллайдеров
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(teleportTag))
        {
            TeleportToTargetScene();
        }
    }

    private void TeleportToTargetScene()
    {
        if (useIndex)
        {
            // Переход по индексу сцены
            SceneManager.LoadScene(sceneIndex);
        }
        else
        {
            // Переход по названию сцены
            SceneManager.LoadScene(sceneName);
        }
    }
}