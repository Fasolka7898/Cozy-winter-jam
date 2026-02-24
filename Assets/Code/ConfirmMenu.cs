using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;
using UnityEngine.Events;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class ConfirmMenu : MonoBehaviour
{
    [Header("=== РЕЖИМ АКТИВАЦИИ ===")]
    [SerializeField] private ActivationMode activationMode = ActivationMode.Start;

    public enum ActivationMode
    {
        Start,          // Показывать в начале сцены
        Trigger,        // Показывать при входе в триггер
        Manual,         // Показывать вручную через вызов метода
        Event           // Показывать по событию из другого скрипта
    }

    [Header("=== БАЗОВЫЕ НАСТРОЙКИ ===")]
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private TextMeshProUGUI messageText;

    [Header("=== НАСТРОЙКИ ТЕКСТА ===")]
    [SerializeField] private string defaultMessage = "Начать игру?";
    [SerializeField] private bool freezeTime = true;
    [SerializeField] private bool lockCursor = true;
    [SerializeField] private string nextSceneName;

    [Header("=== НАСТРОЙКИ ДЛЯ РЕЖИМА START ===")]
    [SerializeField] private bool showOnStart = true;
    [SerializeField] private float startDelay = 0f; // Задержка перед показом в начале

    [Header("=== НАСТРОЙКИ ДЛЯ РЕЖИМА TRIGGER ===")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool destroyOnTrigger = false;
    [SerializeField] private bool showOnce = true;
    [SerializeField] private float triggerDelay = 0f;

    [Header("=== НАСТРОЙКИ ДЛЯ РЕЖИМА EVENT ===")]
    [SerializeField] private string eventListenerName; // Имя события для прослушивания
                                                       // Можно использовать UnityEvent или SendMessage

    [Header("=== НАСТРОЙКИ ДЛЯ РЕЖИМА MANUAL ===")]
    // Ручной вызов через публичные методы

    [Header("=== НАСТРОЙКИ UI ===")]
    [SerializeField] private bool hideOtherUI = true;
    [SerializeField] private string otherUITag = "GameUI";
    [SerializeField] private List<GameObject> additionalUIElements;
    [SerializeField] private bool hideCanvases = true;
    [SerializeField] private bool disableRaycasters = true;

    [Header("=== НАСТРОЙКИ ИГРОКА ===")]
    [SerializeField] private GameObject playerController;

    [Header("=== СОБЫТИЯ ===")]
    [SerializeField] private UnityEvent onMenuShown;
    [SerializeField] private UnityEvent onMenuHidden;
    public UnityEvent onConfirmEvent;  // Теперь это поле доступно через публичные методы
    [SerializeField] private UnityEvent onCancelEvent;

    // Приватные переменные
    private bool isMenuActive = false;
    private bool hasBeenShown = false;
    private MonoBehaviour[] playerScripts;

    private List<GameObject> disabledUIObjects = new List<GameObject>();
    private List<Behaviour> disabledUIComponents = new List<Behaviour>();
    private Dictionary<Canvas, int> canvasSortingOrders = new Dictionary<Canvas, int>();

    // Публичные свойства для доступа к событиям
    public UnityEvent OnConfirmEvent => onConfirmEvent;
    public UnityEvent OnCancelEvent => onCancelEvent;
    public UnityEvent OnMenuShown => onMenuShown;
    public UnityEvent OnMenuHidden => onMenuHidden;

    void Start()
    {
        // Проверка компонентов
        if (menuPanel == null)
        {
            Debug.LogError("Menu Panel не назначен!");
            return;
        }

        if (confirmButton == null || cancelButton == null)
        {
            Debug.LogError("Кнопки не назначены!");
            return;
        }

        // Устанавливаем текст
        if (messageText != null)
            messageText.text = defaultMessage;

        // Назначаем обработчики
        confirmButton.onClick.AddListener(OnConfirm);
        cancelButton.onClick.AddListener(OnCancel);

        // Скрываем панель в начале
        menuPanel.SetActive(false);

        // Активируем в зависимости от режима
        switch (activationMode)
        {
            case ActivationMode.Start:
                if (showOnStart)
                {
                    if (startDelay > 0)
                        Invoke(nameof(ShowMenu), startDelay);
                    else
                        ShowMenu();
                }
                break;

            case ActivationMode.Event:
                // Подписываемся на событие
                Debug.Log($"Режим Event активирован. Ожидание события: {eventListenerName}");
                break;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (activationMode != ActivationMode.Trigger) return;

        if (other.CompareTag(playerTag))
        {
            if (showOnce && hasBeenShown) return;

            if (triggerDelay > 0)
                Invoke(nameof(ShowMenu), triggerDelay);
            else
                ShowMenu();

            hasBeenShown = true;

            if (destroyOnTrigger)
                Destroy(gameObject, triggerDelay);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (activationMode != ActivationMode.Trigger) return;

        if (other.CompareTag(playerTag))
        {
            if (showOnce && hasBeenShown) return;

            if (triggerDelay > 0)
                Invoke(nameof(ShowMenu), triggerDelay);
            else
                ShowMenu();

            hasBeenShown = true;

            if (destroyOnTrigger)
                Destroy(gameObject, triggerDelay);
        }
    }

    // Публичный метод для ручного вызова
    public void ShowMenu()
    {
        if (isMenuActive) return;

        menuPanel.SetActive(true);
        isMenuActive = true;

        if (hideOtherUI)
            HideOtherUI();

        if (freezeTime)
            Time.timeScale = 0f;

        if (playerController != null)
        {
            playerScripts = playerController.GetComponents<MonoBehaviour>();
            foreach (var script in playerScripts)
            {
                if (script.enabled)
                    script.enabled = false;
            }
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        onMenuShown?.Invoke();
        Debug.Log($"Меню показано (режим: {activationMode})");
    }

    // Перегруженный метод с возможностью изменить текст
    public void ShowMenu(string customMessage)
    {
        if (messageText != null)
            messageText.text = customMessage;

        ShowMenu();
    }

    // Перегруженный метод с изменением текста и сцены
    public void ShowMenu(string customMessage, string sceneName)
    {
        if (messageText != null)
            messageText.text = customMessage;

        if (!string.IsNullOrEmpty(sceneName))
            nextSceneName = sceneName;

        ShowMenu();
    }

    // Публичный метод для скрытия
    public void HideMenu()
    {
        if (!isMenuActive) return;

        menuPanel.SetActive(false);
        isMenuActive = false;

        if (hideOtherUI)
            ShowOtherUI();

        if (freezeTime)
            Time.timeScale = 1f;

        if (playerController != null && playerScripts != null)
        {
            foreach (var script in playerScripts)
                script.enabled = true;
        }

        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        onMenuHidden?.Invoke();
        Debug.Log("Меню скрыто");
    }

    // Методы для настройки
    public void SetMessage(string message)
    {
        if (messageText != null)
            messageText.text = message;
    }

    public void SetNextScene(string sceneName)
    {
        nextSceneName = sceneName;
    }

    public void SetPlayerController(GameObject controller)
    {
        playerController = controller;
    }

    // Публичные методы для подписки на события
    public void AddConfirmListener(UnityAction action)
    {
        onConfirmEvent.AddListener(action);
    }

    public void RemoveConfirmListener(UnityAction action)
    {
        onConfirmEvent.RemoveListener(action);
    }

    public void AddCancelListener(UnityAction action)
    {
        onCancelEvent.AddListener(action);
    }

    public void RemoveCancelListener(UnityAction action)
    {
        onCancelEvent.RemoveListener(action);
    }

    public void AddShownListener(UnityAction action)
    {
        onMenuShown.AddListener(action);
    }

    public void RemoveShownListener(UnityAction action)
    {
        onMenuShown.RemoveListener(action);
    }

    public void AddHiddenListener(UnityAction action)
    {
        onMenuHidden.AddListener(action);
    }

    public void RemoveHiddenListener(UnityAction action)
    {
        onMenuHidden.RemoveListener(action);
    }

    // Получение события из других скриптов
    public void OnGameEvent(string eventName)
    {
        if (activationMode == ActivationMode.Event && eventName == eventListenerName)
        {
            ShowMenu();
        }
    }

    // Для отправки сообщений через SendMessage
    public void ReceiveEvent(string eventName)
    {
        OnGameEvent(eventName);
    }

    void HideOtherUI()
    {
        disabledUIObjects.Clear();
        disabledUIComponents.Clear();
        canvasSortingOrders.Clear();

        // По тегу
        if (!string.IsNullOrEmpty(otherUITag))
        {
            bool tagExists = false;
            try
            {
                GameObject.FindGameObjectsWithTag(otherUITag);
                tagExists = true;
            }
            catch (UnityException)
            {
                Debug.LogWarning($"Тег '{otherUITag}' не существует");
            }

            if (tagExists)
            {
                GameObject[] taggedUI = GameObject.FindGameObjectsWithTag(otherUITag);
                foreach (GameObject ui in taggedUI)
                {
                    if (ui != null && ui != menuPanel && ui.activeSelf && !ui.transform.IsChildOf(menuPanel.transform))
                    {
                        ui.SetActive(false);
                        disabledUIObjects.Add(ui);
                    }
                }
            }
        }

        // Дополнительные элементы
        if (additionalUIElements != null)
        {
            foreach (GameObject ui in additionalUIElements)
            {
                if (ui != null && ui != menuPanel && ui.activeSelf && !ui.transform.IsChildOf(menuPanel.transform))
                {
                    ui.SetActive(false);
                    disabledUIObjects.Add(ui);
                }
            }
        }

        // Canvas
        if (hideCanvases)
        {
            Canvas[] allCanvases = FindObjectsOfType<Canvas>();
            foreach (Canvas canvas in allCanvases)
            {
                if (canvas.gameObject != menuPanel && canvas.enabled && !canvas.transform.IsChildOf(menuPanel.transform))
                {
                    canvasSortingOrders[canvas] = canvas.sortingOrder;
                    canvas.enabled = false;
                    disabledUIComponents.Add(canvas);
                }
            }
        }

        // Raycaster
        if (disableRaycasters)
        {
            GraphicRaycaster[] raycasters = FindObjectsOfType<GraphicRaycaster>();
            foreach (GraphicRaycaster raycaster in raycasters)
            {
                if (raycaster.gameObject != menuPanel && raycaster.enabled && !raycaster.transform.IsChildOf(menuPanel.transform))
                {
                    raycaster.enabled = false;
                    disabledUIComponents.Add(raycaster);
                }
            }
        }
    }

    void ShowOtherUI()
    {
        foreach (GameObject ui in disabledUIObjects)
            if (ui != null) ui.SetActive(true);

        foreach (Behaviour component in disabledUIComponents)
        {
            if (component != null)
            {
                if (component is Selectable selectable)
                    selectable.interactable = true;
                else
                    component.enabled = true;
            }
        }

        foreach (var kvp in canvasSortingOrders)
            if (kvp.Key != null) kvp.Key.sortingOrder = kvp.Value;

        disabledUIObjects.Clear();
        disabledUIComponents.Clear();
        canvasSortingOrders.Clear();
    }

    void OnConfirm()
    {
        onConfirmEvent?.Invoke();
        HideMenu();

        if (!string.IsNullOrEmpty(nextSceneName))
            SceneManager.LoadScene(nextSceneName);
    }

    void OnCancel()
    {
        onCancelEvent?.Invoke();
        HideMenu();
    }

    void Update()
    {
        if (isMenuActive && IsEscapePressed())
            OnCancel();
    }

    private bool IsEscapePressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Escape);
#endif
    }

    void OnDestroy()
    {
        confirmButton.onClick.RemoveListener(OnConfirm);
        cancelButton.onClick.RemoveListener(OnCancel);
    }
}