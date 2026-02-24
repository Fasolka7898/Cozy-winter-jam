using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class HouseBuilder : MonoBehaviour
{
    [Header("Настройки строительства")]
    [SerializeField] private int requiredBranches = 50;
    [SerializeField] private int branchesPerContribution = 1;
    [SerializeField] private float interactionRadius = 3f;

    [Header("Состояние дома")]
    [SerializeField] private GameObject houseModel;
    [SerializeField] private bool isHouseBuilt = false;

    [Header("UI")]
    [SerializeField] private GameObject interactionPanel;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private string promptText = "Нажмите E, чтобы отдать палки ({0}/{1})";
    [SerializeField] private string builtText = "Дом построен!";
    [SerializeField] private string notEnoughText = "Недостаточно палок!";

    [Header("Меню подтверждения")]
    [SerializeField] private ConfirmMenu confirmMenu; // Ссылка на меню подтверждения
    [SerializeField] private bool showMenuOnBuild = true;
    [SerializeField] private string builtMessage = "Дом построен! Начать новое задание?";
    [SerializeField] private string nextSceneAfterBuild;

    [Header("Визуальные эффекты")]
    [SerializeField] private ParticleSystem buildEffect;
    [SerializeField] private ParticleSystem completedEffect;
    [SerializeField] private Light buildLight;

    [Header("Звуки")]
    [SerializeField] private AudioClip addBranchSound;
    [SerializeField] private AudioClip buildCompleteSound;
    [SerializeField] private AudioClip notEnoughSound;

    private int currentBranches = 0;
    private bool playerInRange = false;
    private PlayerBranches playerBranches;
    private PlayerInput playerInput;
    private InputAction interactAction;
    private AudioSource audioSource;

    private void Start()
    {
        SetupAudio();
        FindPlayer();
        SetupInputAction();

        if (interactionPanel != null)
            interactionPanel.SetActive(false);

        if (houseModel != null && !isHouseBuilt)
            houseModel.SetActive(false);

        if (buildLight != null)
            buildLight.enabled = false;

        UpdateUI();

        // Ищем меню, если не назначено
        if (confirmMenu == null)
        {
            confirmMenu = FindObjectOfType<ConfirmMenu>();
        }
    }

    private void SetupAudio()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    private void FindPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerBranches = player.GetComponent<PlayerBranches>();
            playerInput = player.GetComponent<PlayerInput>();
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
        if (playerInRange && !isHouseBuilt)
            TryAddBranches();
    }

    private void Update()
    {
        CheckPlayerDistance();

        if (playerInRange && !isHouseBuilt && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            TryAddBranches();
    }

    private void CheckPlayerDistance()
    {
        if (playerBranches == null) return;

        float dist = Vector3.Distance(transform.position, playerBranches.transform.position);
        bool wasInRange = playerInRange;
        playerInRange = dist <= interactionRadius;

        if (wasInRange != playerInRange && interactionPanel != null)
        {
            interactionPanel.SetActive(playerInRange && !isHouseBuilt);

            if (playerInRange && !isHouseBuilt)
            {
                UpdatePanelText();
            }
        }
    }

    private void TryAddBranches()
    {
        if (playerBranches == null) return;

        if (!playerBranches.HasEnoughBranches(branchesPerContribution))
        {
            if (notEnoughSound != null)
                audioSource.PlayOneShot(notEnoughSound);

            if (statusText != null)
            {
                statusText.text = notEnoughText;
                Invoke(nameof(ClearStatusText), 2f);
            }
            return;
        }

        if (playerBranches.UseBranches(branchesPerContribution))
        {
            currentBranches += branchesPerContribution;

            if (buildEffect != null)
                buildEffect.Play();

            if (addBranchSound != null)
                audioSource.PlayOneShot(addBranchSound);

            if (buildLight != null)
            {
                buildLight.enabled = true;
                CancelInvoke(nameof(TurnOffLight));
                Invoke(nameof(TurnOffLight), 0.5f);
            }

            if (currentBranches >= requiredBranches)
            {
                BuildHouse();
            }

            UpdateUI();
            UpdatePanelText();
        }
    }

    private void TurnOffLight()
    {
        if (buildLight != null)
            buildLight.enabled = false;
    }

    private void BuildHouse()
    {
        isHouseBuilt = true;

        if (houseModel != null)
            houseModel.SetActive(true);

        if (completedEffect != null)
            completedEffect.Play();

        if (buildCompleteSound != null)
            audioSource.PlayOneShot(buildCompleteSound);

        if (buildLight != null)
            buildLight.enabled = true;

        if (interactionPanel != null)
            interactionPanel.SetActive(false);

        UpdateUI();

        Debug.Log($"Дом построен! Использовано {currentBranches} палок");

        // Показываем меню подтверждения после постройки
        if (showMenuOnBuild && confirmMenu != null)
        {
            ShowConfirmationMenu();
        }
    }

    private void ShowConfirmationMenu()
    {
        if (confirmMenu != null)
        {
            // Используем публичные методы ConfirmMenu
            confirmMenu.SetMessage(builtMessage);
            confirmMenu.SetNextScene(nextSceneAfterBuild);
            confirmMenu.ShowMenu();
        }
    }

    private void UpdateUI()
    {
        if (progressText != null)
        {
            if (isHouseBuilt)
            {
                progressText.text = builtText;
            }
            else
            {
                progressText.text = $"Прогресс: {currentBranches}/{requiredBranches}";
            }
        }
    }

    private void UpdatePanelText()
    {
        if (interactionPanel != null)
        {
            TextMeshProUGUI panelText = interactionPanel.GetComponentInChildren<TextMeshProUGUI>();
            if (panelText != null)
            {
                panelText.text = string.Format(promptText, currentBranches, requiredBranches);
            }
        }
    }

    private void ClearStatusText()
    {
        if (statusText != null)
            statusText.text = "";
    }

    private void OnDestroy()
    {
        if (interactAction != null)
            interactAction.performed -= OnInteractPerformed;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }

    // Публичные методы
    public int GetCurrentBranches() => currentBranches;
    public int GetRequiredBranches() => requiredBranches;
    public bool IsHouseBuilt() => isHouseBuilt;

    public void ForceBuildHouse()
    {
        if (!isHouseBuilt)
        {
            currentBranches = requiredBranches;
            BuildHouse();
        }
    }

    public void ResetHouse()
    {
        currentBranches = 0;
        isHouseBuilt = false;

        if (houseModel != null)
            houseModel.SetActive(false);

        UpdateUI();
    }
}