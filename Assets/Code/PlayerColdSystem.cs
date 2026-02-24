using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerColdSystem : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    private float currentHealth;

    [Header("Cold Settings")]
    [SerializeField] private float maxCold = 100f;
    [SerializeField] private float coldIncreaseRate = 10f;
    [SerializeField] private float coldDecreaseRate = 20f;
    [SerializeField] private float healthRegenRate = 5f;
    [SerializeField] private float coldDamageRate = 10f;

    private float currentCold;

    [Header("UI Sliders")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Slider coldSlider;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI coldText;
    [SerializeField] private string healthFormat = "{0:F0}/{1:F0}";
    [SerializeField] private string coldFormat = "{0:F0}%";

    [Header("=== МЕНЮ ПОДТВЕРЖДЕНИЯ ===")]
    [SerializeField] private ConfirmMenu confirmMenu;
    [SerializeField] private string deathMessage = "Вы замерзли насмерть! Начать заново?";
    [SerializeField] private string restartSceneName = "Level1";
    [SerializeField] private float deathMenuDelay = 1f;

    [Header("Status")]
    private bool isInWarmth = false;
    private bool isDead = false;

    // Используем переименованный класс
    private PlayerMovement.PlayerMovement playerMovement;

    private void Start()
    {
        currentHealth = maxHealth;
        currentCold = 0f;
        isDead = false;

        playerMovement = GetComponent<PlayerMovement.PlayerMovement>();

        UpdateUI();
    }

    private void Update()
    {
        if (isDead) return;

        float dt = Time.deltaTime;

        if (isInWarmth)
        {
            if (currentCold > 0)
            {
                currentCold = Mathf.Max(0, currentCold - coldDecreaseRate * dt);
            }
            else
            {
                currentHealth = Mathf.Min(maxHealth, currentHealth + healthRegenRate * dt);
            }
        }
        else
        {
            if (currentCold < maxCold)
            {
                currentCold = Mathf.Min(maxCold, currentCold + coldIncreaseRate * dt);
            }
            else
            {
                currentHealth = Mathf.Max(0, currentHealth - coldDamageRate * dt);
            }
        }

        if (currentHealth <= 0 && !isDead)
        {
            Die();
        }

        UpdateUI();
    }

    private void UpdateUI()
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }
        if (coldSlider != null)
        {
            coldSlider.maxValue = maxCold;
            coldSlider.value = currentCold;
        }
        if (healthText != null)
        {
            healthText.text = string.Format(healthFormat, currentHealth, maxHealth);
        }
        if (coldText != null)
        {
            float percent = (currentCold / maxCold) * 100f;
            coldText.text = string.Format(coldFormat, percent);
        }
    }

    public void SetInWarmth(bool inWarmth)
    {
        isInWarmth = inWarmth;
    }

    private void Die()
    {
        if (isDead) return;

        isDead = true;
        Debug.Log("Игрок замёрз насмерть!");

        if (playerMovement != null)
        {
            playerMovement.enabled = false;
        }

        PlayerInput playerInput = GetComponent<PlayerInput>();
        if (playerInput != null)
        {
            playerInput.enabled = false;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Invoke(nameof(ShowDeathMenu), deathMenuDelay);
    }

    private void ShowDeathMenu()
    {
        if (confirmMenu != null)
        {
            if (!string.IsNullOrEmpty(restartSceneName))
            {
                confirmMenu.ShowMenu(deathMessage, restartSceneName);
            }
            else
            {
                confirmMenu.ShowMenu(deathMessage);
            }

            SetupDeathMenuEvents();
        }
        else
        {
            Debug.LogWarning("ConfirmMenu не назначен в PlayerColdSystem! Загружаем сцену напрямую.");
            RestartGame();
        }
    }

    private void SetupDeathMenuEvents()
    {
        if (confirmMenu == null) return;

        confirmMenu.RemoveConfirmListener(RestartGame);
        confirmMenu.RemoveCancelListener(OnDeathCancel);

        confirmMenu.AddConfirmListener(RestartGame);
        confirmMenu.AddCancelListener(OnDeathCancel);
    }

    private void RestartGame()
    {
        Debug.Log("Рестарт игры...");

        if (confirmMenu != null)
        {
            confirmMenu.RemoveConfirmListener(RestartGame);
            confirmMenu.RemoveCancelListener(OnDeathCancel);
        }

        if (!string.IsNullOrEmpty(restartSceneName))
        {
            SceneManager.LoadScene(restartSceneName);
        }
        else
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    private void OnDeathCancel()
    {
        Debug.Log("Игрок отказался от рестарта");

        if (confirmMenu != null)
        {
            confirmMenu.RemoveConfirmListener(RestartGame);
            confirmMenu.RemoveCancelListener(OnDeathCancel);
        }
    }

    public void ManualRestart()
    {
        RestartGame();
    }

    public void ResetPlayer()
    {
        isDead = false;
        currentHealth = maxHealth;
        currentCold = 0f;

        if (playerMovement != null)
        {
            playerMovement.enabled = true;
        }

        PlayerInput playerInput = GetComponent<PlayerInput>();
        if (playerInput != null)
        {
            playerInput.enabled = true;
        }

        UpdateUI();

        Debug.Log("Игрок воскрешен!");
    }

    public float GetHealthNormalized() => currentHealth / maxHealth;
    public float GetColdNormalized() => currentCold / maxCold;
    public bool IsWarm() => currentCold <= 0;
    public bool IsCold() => currentCold >= maxCold;
    public bool IsDead() => isDead;
}