using UnityEngine;
using TMPro;

public class PlayerBranches : MonoBehaviour
{
    [Header("Настройки")]
    [SerializeField] private int startBranches = 0;
    [SerializeField] private int maxBranches = 99;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI branchesText;
    [SerializeField] private string textPrefix = "Палки: ";
    [SerializeField] private Color textColor = Color.white; // Добавил поле для цвета

    // Статическое поле для хранения палок (общее для всех игроков)
    private static int totalBranches = 0;

    // Свойство для доступа к количеству палок
    public int BranchCount => totalBranches;

    // Событие для оповещения об изменении количества палок
    public delegate void BranchCountChanged(int newCount);
    public static event BranchCountChanged OnBranchCountChanged;

    private void Awake()
    {
        // Инициализация при создании игрока
        if (totalBranches == 0)
        {
            totalBranches = startBranches;
        }

        UpdateUI();
    }

    private void Start()
    {
        // Поиск UI, если не назначен в инспекторе
        if (branchesText == null)
        {
            GameObject uiObject = GameObject.FindGameObjectWithTag("BranchesUI");
            if (uiObject != null)
            {
                branchesText = uiObject.GetComponent<TextMeshProUGUI>();
            }
        }

        UpdateUI();
    }

    private void OnEnable()
    {
        // Подписываемся на событие изменения количества палок
        OnBranchCountChanged += HandleBranchCountChanged;
    }

    private void OnDisable()
    {
        // Отписываемся от события
        OnBranchCountChanged -= HandleBranchCountChanged;
    }

    // Метод для добавления палок
    public void AddBranches(int amount)
    {
        if (amount <= 0) return;

        int newCount = totalBranches + amount;
        if (newCount > maxBranches)
        {
            newCount = maxBranches;
        }

        if (newCount != totalBranches)
        {
            totalBranches = newCount;
            OnBranchCountChanged?.Invoke(totalBranches);
            UpdateUI();

            Debug.Log($"Добавлено {amount} палок. Всего: {totalBranches}");
        }
    }

    // Метод для использования палок
    public bool UseBranches(int amount)
    {
        if (amount <= 0) return true;
        if (totalBranches < amount) return false;

        totalBranches -= amount;
        OnBranchCountChanged?.Invoke(totalBranches);
        UpdateUI();

        Debug.Log($"Использовано {amount} палок. Осталось: {totalBranches}");
        return true;
    }

    // Проверка наличия достаточного количества палок
    public bool HasEnoughBranches(int amount)
    {
        return totalBranches >= amount;
    }

    // Сброс количества палок
    public void ResetBranches(int newAmount = 0)
    {
        totalBranches = Mathf.Clamp(newAmount, 0, maxBranches);
        OnBranchCountChanged?.Invoke(totalBranches);
        UpdateUI();
    }

    private void HandleBranchCountChanged(int newCount)
    {
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (branchesText != null)
        {
            branchesText.text = textPrefix + totalBranches.ToString();
            branchesText.color = textColor; // Применяем цвет из инспектора
        }
    }

    // Публичный метод для изменения цвета через код
    public void SetTextColor(Color newColor)
    {
        textColor = newColor;
        UpdateUI();
    }

    // Публичный метод для получения текущего цвета
    public Color GetTextColor()
    {
        return textColor;
    }

    // Для отладки в редакторе
    private void OnValidate()
    {
        if (maxBranches < 1) maxBranches = 1;
    }
}