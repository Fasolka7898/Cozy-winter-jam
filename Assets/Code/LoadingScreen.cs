using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class LoadingScreen : MonoBehaviour
{
    [Header("Настройки загрузки")]
    [SerializeField] private int targetSceneName;
    [SerializeField] private float minLoadingTime = 3f; // Минимальное время загрузки в секундах

    [Header("Прогресс-бар")]
    [SerializeField] private Slider progressBar;

    private float loadingTimer = 0f;
    private bool isSceneReady = false;

    private void Start()
    {
        StartCoroutine(LoadSceneAsync());
    }

    private IEnumerator LoadSceneAsync()
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(targetSceneName);
        asyncLoad.allowSceneActivation = false;

        // Ждем, пока загрузка дойдет до 90% (0.9)
        while (!asyncLoad.isDone || loadingTimer < minLoadingTime)
        {
            // Обновляем таймер
            loadingTimer += Time.deltaTime;

            // Прогресс загрузки (0..0.9)
            float loadProgress = Mathf.Clamp01(asyncLoad.progress / 0.9f);

            // Прогресс с учетом таймера (если загрузка быстрая, все равно ждем minLoadingTime)
            float displayProgress = Mathf.Min(loadProgress, loadingTimer / minLoadingTime);

            // Обновляем UI
            progressBar.value = displayProgress;

            // Если загрузка завершена, но таймер еще не истек
            if (asyncLoad.progress >= 0.9f)
            {
                isSceneReady = true;
            }

            // Если таймер истек и сцена готова — переходим
            if (loadingTimer >= minLoadingTime && isSceneReady)
            {
                asyncLoad.allowSceneActivation = true;
            }

            yield return null;
        }
    }
}