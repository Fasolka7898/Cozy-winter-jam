using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class CampFire : MonoBehaviour
{
    [SerializeField] private Color colorWH;
    [SerializeField] private Color colorFR;

    [Header("Настройки горения")]
    [SerializeField] private float maxBurnTime = 60f;
    [SerializeField] private float remainingTime = 60f;
    [SerializeField] private int branchesPerAdd = 1;
    [SerializeField] private float timePerBranch = 10f;

    [Header("Настройки обогрева")]
    [SerializeField] private float warmthRadius = 5f;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool showWarmthRadius = true;

    [Header("Визуальные эффекты")]
    [SerializeField] private ParticleSystem fireParticles;
    [SerializeField] private Light fireLight;
    [SerializeField] private AnimationCurve lightIntensityCurve;
    [SerializeField] private float minLightIntensity = 0.5f;
    [SerializeField] private float maxLightIntensity = 3f;
    [SerializeField] private ParticleSystem warmthParticles;

    [Header("UI")]
    [SerializeField] private GameObject interactionPanel;
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private string timeFormat = "Осталось: {0:F0}с";
    [SerializeField] private string promptText = "Нажмите E, чтобы добавить ветку";
    [SerializeField] private TextMeshProUGUI warmthStatusText;
    [SerializeField] private string warmingUpText = "Вы греетесь у костра";
    [SerializeField] private string freezingText = "Вы замерзаете!";

    [Header("UI Слайдер")]
    [SerializeField] private Slider burnTimeSlider;
    [SerializeField] private Gradient sliderColorGradient;
    [SerializeField] private Image sliderFillImage;

    [Header("Звуки")]
    [SerializeField] private AudioClip burningSound;
    [SerializeField] private AudioClip addBranchSound;
    [SerializeField] private AudioClip fireExtinguishSound;
    [SerializeField] private AudioClip warmingSound;
    [SerializeField] private AudioClip freezingSound;

    [Header("=== МЕНЮ ПОДТВЕРЖДЕНИЯ ===")]
    [SerializeField] private ConfirmMenu confirmMenu;
    [SerializeField] private string extinctionMessage = "Костер потух. Вернуться в лагерь?";
    [SerializeField] private string campSceneName = "CampScene"; // Название сцены с лагерем
    [SerializeField] private float sceneLoadDelay = 1f;

    [Header("=== НАСТРОЙКИ ПЕРЕНОСА ===")]
    [SerializeField] private Transform campSpawnPoint; // Точка появления в лагере
    [SerializeField] private bool useSpawnPoint = true;

    private AudioSource audioSource;
    private bool isBurning = true;
    private bool playerInRange = false;
    private bool playerInWarmthRadius = false;

    private PlayerBranches playerBranches;
    private PlayerInput playerInput;
    private PlayerColdSystem playerColdSystem;
    private InputAction interactAction;
    private GameObject player;

    private float initialLightIntensity;

    private void Start()
    {
        SetupAudio();
        FindPlayer();
        SetupInputAction();
        InitializeSlider();
        SetupWarmthParticles();

        if (interactionPanel != null)
            interactionPanel.SetActive(false);

        if (fireLight != null)
            initialLightIntensity = fireLight.intensity;

        UpdateUI();
    }

    private void SetupWarmthParticles()
    {
        if (warmthParticles != null)
        {
            var shape = warmthParticles.shape;
            shape.radius = warmthRadius;
            shape.radiusThickness = 0.1f;
            if (isBurning)
                warmthParticles.Play();
            else
                warmthParticles.Stop();
        }
    }

    private void InitializeSlider()
    {
        if (burnTimeSlider == null)
        {
            burnTimeSlider = GetComponentInChildren<Slider>();
            if (burnTimeSlider == null)
                burnTimeSlider = FindObjectOfType<Slider>();
        }
        SetupSlider();
    }

    private void SetupSlider()
    {
        if (burnTimeSlider == null) return;

        RectTransform rect = burnTimeSlider.GetComponent<RectTransform>();
        if (rect != null) rect.sizeDelta = new Vector2(200f, 20f);

        burnTimeSlider.gameObject.SetActive(true);
        burnTimeSlider.minValue = 0f;
        burnTimeSlider.maxValue = maxBurnTime;
        burnTimeSlider.value = remainingTime;
        burnTimeSlider.direction = Slider.Direction.LeftToRight;

        Transform fillArea = burnTimeSlider.transform.Find("Fill Area");
        if (fillArea != null)
        {
            Transform fill = fillArea.Find("Fill");
            if (fill != null && sliderFillImage == null)
                sliderFillImage = fill.GetComponent<Image>();
        }

        Transform handleArea = burnTimeSlider.transform.Find("Handle Slide Area");
        if (handleArea != null)
            handleArea.gameObject.SetActive(false);

        if (sliderColorGradient == null)
            CreateDefaultGradient();

        UpdateSliderColor();
    }

    private void CreateDefaultGradient()
    {
        sliderColorGradient = new Gradient();
        GradientColorKey[] colors = new GradientColorKey[3];
        colors[0] = new GradientColorKey(Color.green, 1f);
        colors[1] = new GradientColorKey(Color.yellow, 0.5f);
        colors[2] = new GradientColorKey(Color.red, 0f);
        GradientAlphaKey[] alphas = new GradientAlphaKey[3];
        alphas[0] = new GradientAlphaKey(1f, 1f);
        alphas[1] = new GradientAlphaKey(1f, 0.5f);
        alphas[2] = new GradientAlphaKey(1f, 0f);
        sliderColorGradient.SetKeys(colors, alphas);
    }

    private void SetupAudio()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        if (burningSound != null && isBurning)
        {
            audioSource.clip = burningSound;
            audioSource.loop = true;
            audioSource.Play();
        }
    }

    private void FindPlayer()
    {
        player = GameObject.FindGameObjectWithTag(playerTag);
        if (player != null)
        {
            playerBranches = player.GetComponent<PlayerBranches>();
            playerInput = player.GetComponent<PlayerInput>();
            playerColdSystem = player.GetComponent<PlayerColdSystem>();
            if (playerColdSystem == null)
                Debug.LogWarning("На игроке нет компонента PlayerColdSystem!");
        }
    }

    private void SetupInputAction()
    {
        if (playerInput != null && playerInput.actions != null)
        {
            interactAction = playerInput.actions.FindAction("Interact") ?? playerInput.actions.FindAction("Use");
            if (interactAction != null)
            {
                interactAction.performed += OnInteractPerformed;
                interactAction.Enable();
            }
        }
    }

    private void OnInteractPerformed(InputAction.CallbackContext context)
    {
        if (playerInRange && isBurning)
            TryAddBranch();
    }

    private void Update()
    {
        if (isBurning)
        {
            remainingTime -= Time.deltaTime;
            UpdateFireEffects();
            CheckWarmthRadius();

            if (remainingTime <= 0)
                ExtinguishFire();

            UpdateUI();
        }

        CheckPlayerDistance();

        if (playerInRange && isBurning && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            TryAddBranch();
    }

    private void CheckWarmthRadius()
    {
        if (playerBranches == null || playerColdSystem == null) return;

        float dist = Vector3.Distance(transform.position, playerBranches.transform.position);
        bool wasInWarmth = playerInWarmthRadius;
        playerInWarmthRadius = dist <= warmthRadius && isBurning;

        if (wasInWarmth != playerInWarmthRadius)
        {
            playerColdSystem.SetInWarmth(playerInWarmthRadius);

            if (playerInWarmthRadius)
            {
                if (warmingSound != null) audioSource.PlayOneShot(warmingSound);
            }
            else
            {
                if (freezingSound != null) audioSource.PlayOneShot(freezingSound);
            }
        }
    }

    private void CheckPlayerDistance()
    {
        if (playerBranches == null) return;

        float dist = Vector3.Distance(transform.position, playerBranches.transform.position);
        bool wasInRange = playerInRange;
        playerInRange = dist <= 3f;

        if (wasInRange != playerInRange && interactionPanel != null)
        {
            interactionPanel.SetActive(playerInRange && isBurning);
        }
    }

    private void TryAddBranch()
    {
        if (playerBranches != null && playerBranches.HasEnoughBranches(branchesPerAdd))
        {
            if (playerBranches.UseBranches(branchesPerAdd))
            {
                AddTime(timePerBranch);
                if (addBranchSound != null) audioSource.PlayOneShot(addBranchSound);
            }
        }
    }

    public void AddTime(float seconds)
    {
        if (!isBurning) return; // Не разжигаем костер автоматически
        remainingTime = Mathf.Min(remainingTime + seconds, maxBurnTime);
    }

    private void UpdateFireEffects()
    {
        if (fireParticles != null)
        {
            float t = remainingTime / maxBurnTime;
            var emission = fireParticles.emission;
            emission.rateOverTime = Mathf.Lerp(5f, 20f, t);
            var main = fireParticles.main;
            main.startSize = Mathf.Lerp(0.5f, 1.5f, t);
        }

        if (fireLight != null)
        {
            float t = remainingTime / maxBurnTime;
            if (lightIntensityCurve != null && lightIntensityCurve.keys.Length > 0)
                fireLight.intensity = lightIntensityCurve.Evaluate(1 - t) * maxLightIntensity;
            else
                fireLight.intensity = Mathf.Lerp(minLightIntensity, maxLightIntensity, t);
            fireLight.intensity += Random.Range(-0.1f, 0.1f);
        }
    }

    private void UpdateSlider()
    {
        if (burnTimeSlider != null)
        {
            burnTimeSlider.value = remainingTime;
            UpdateSliderColor();
        }
    }

    private void UpdateSliderColor()
    {
        if (sliderFillImage != null)
        {
            float t = remainingTime / maxBurnTime;
            if (sliderColorGradient != null)
                sliderFillImage.color = sliderColorGradient.Evaluate(t);
            else if (t > 0.66f) sliderFillImage.color = Color.green;
            else if (t > 0.33f) sliderFillImage.color = Color.yellow;
            else sliderFillImage.color = Color.red;
        }
    }

    private void ExtinguishFire()
    {
        isBurning = false;
        remainingTime = 0;

        if (fireParticles != null) fireParticles.Stop();
        if (warmthParticles != null) warmthParticles.Stop();
        if (fireLight != null) fireLight.enabled = false;

        if (audioSource != null)
        {
            audioSource.Stop();
            if (fireExtinguishSound != null) audioSource.PlayOneShot(fireExtinguishSound);
        }

        if (interactionPanel != null)
            interactionPanel.SetActive(false);

        playerInWarmthRadius = false;
        if (playerColdSystem != null) playerColdSystem.SetInWarmth(false);

        UpdateUI();

        // Вызываем меню подтверждения при тушении костра
        if (confirmMenu != null)
        {
            // Показываем меню с кастомным сообщением
            confirmMenu.ShowMenu(extinctionMessage);

            // Настраиваем событие подтверждения в меню
            SetupConfirmMenuEvents();
        }
        else
        {
            Debug.LogWarning("ConfirmMenu не назначен в CampFire!");
        }
    }

    private void SetupConfirmMenuEvents()
    {
        if (confirmMenu == null) return;

        // Очищаем предыдущие события, чтобы не было накопления
        confirmMenu.RemoveConfirmListener(TeleportToCamp);
        confirmMenu.RemoveCancelListener(OnCancelExtinction);

        // Добавляем наш метод на подтверждение - телепортация в лагерь
        confirmMenu.AddConfirmListener(TeleportToCamp);

        // Добавляем обработку отмены - можно ничего не делать или добавить логику
        confirmMenu.AddCancelListener(OnCancelExtinction);
    }

    // Метод для телепортации в лагерь
    private void TeleportToCamp()
    {
        Debug.Log("Телепортация в лагерь...");

        // Разблокируем курсор перед загрузкой сцены
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Загружаем сцену лагеря
        if (!string.IsNullOrEmpty(campSceneName))
        {
            StartCoroutine(LoadCampScene());
        }
        else
        {
            Debug.LogError("Не указано название сцены с лагерем!");
        }
    }

    private System.Collections.IEnumerator LoadCampScene()
    {
        // Задержка перед загрузкой сцены
        yield return new WaitForSeconds(sceneLoadDelay);

        // Загружаем сцену
        SceneManager.LoadScene(campSceneName);
    }

    // Метод для отмены (если игрок нажал "Нет")
    private void OnCancelExtinction()
    {
        Debug.Log("Игрок отказался от телепортации в лагерь");

        // Можно добавить дополнительную логику, например:
        // - Показать подсказку
        // - Запустить таймер на автоматическое завершение
        // - Оставить костер потухшим
    }

    // Альтернативный метод: телепортация с сохранением позиции игрока (если сцена та же)
    private void TeleportToSpawnPoint()
    {
        if (player != null && campSpawnPoint != null && useSpawnPoint)
        {
            // Телепортируем игрока в указанную точку
            CharacterController controller = player.GetComponent<CharacterController>();
            if (controller != null)
            {
                controller.enabled = false;
                player.transform.position = campSpawnPoint.position;
                player.transform.rotation = campSpawnPoint.rotation;
                controller.enabled = true;
            }
            else
            {
                player.transform.position = campSpawnPoint.position;
                player.transform.rotation = campSpawnPoint.rotation;
            }

            Debug.Log("Игрок телепортирован к точке спавна в лагере");
        }
    }

    private void UpdateUI()
    {
        if (timeText != null)
        {
            if (isBurning)
            {
                timeText.text = string.Format(timeFormat, remainingTime);
                timeText.color = Color.yellow;
            }
            else
            {
                timeText.text = "Костер потух";
                timeText.color = Color.gray;
            }
        }

        if (warmthStatusText != null)
        {
            if (!isBurning)
            {
                warmthStatusText.text = "Костер потух";
                warmthStatusText.color = Color.gray;
            }
            else if (playerInWarmthRadius)
            {
                warmthStatusText.text = warmingUpText;
                warmthStatusText.color = colorWH;
            }
            else if (playerColdSystem != null && playerColdSystem.IsCold())
            {
                warmthStatusText.text = freezingText;
                warmthStatusText.color = colorFR;
            }
            else
            {
                warmthStatusText.text = "";
            }
        }

        UpdateSlider();
    }

    private void OnDestroy()
    {
        if (interactAction != null)
            interactAction.performed -= OnInteractPerformed;

        // Очищаем события при уничтожении
        if (confirmMenu != null)
        {
            confirmMenu.RemoveConfirmListener(TeleportToCamp);
            confirmMenu.RemoveCancelListener(OnCancelExtinction);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 3f);
        if (showWarmthRadius)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
            Gizmos.DrawSphere(transform.position, warmthRadius);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, warmthRadius);
        }
    }

    // Публичные методы для доступа к данным
    public float GetRemainingTime() => remainingTime;
    public float GetMaxTime() => maxBurnTime;
    public bool IsBurning() => isBurning;
    public bool IsPlayerInWarmthRadius() => playerInWarmthRadius;
    public float GetWarmthRadius() => warmthRadius;
}