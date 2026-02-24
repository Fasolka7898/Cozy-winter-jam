using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class PickupStick : MonoBehaviour
{
    [Header("Настройки")]
    [SerializeField] private int stickAmount = 1;
    [SerializeField] private float pickupRange = 2f;
    [SerializeField] private float rotationSpeed = 90f;

    [Header("UI Подсказка")]
    [SerializeField] private GameObject promptPrefab;
    [SerializeField] private string promptText = "Нажмите E, чтобы подобрать палку";

    [Header("Эффекты")]
    [SerializeField] private GameObject pickupEffect;
    [SerializeField] private AudioClip pickupSound;

    private Transform player;
    private PlayerInput playerInput;
    private PlayerBranches playerBranches;
    private GameObject activePrompt;
    private bool playerInRange = false;
    private AudioSource audioSource;
    private InputAction interactAction;

    private void Start()
    {
        FindPlayer();
        CreatePrompt();
        SetupAudio();
    }

    private void FindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerBranches = playerObj.GetComponent<PlayerBranches>();
            playerInput = playerObj.GetComponent<PlayerInput>();

            if (playerBranches == null)
            {
                Debug.LogError("На игроке нет компонента PlayerBranches!");
            }

            SetupInputAction();
        }
        else
        {
            Debug.LogError("Не найден объект с тегом 'Player'!");
        }
    }

    private void SetupInputAction()
    {
        if (playerInput != null && playerInput.actions != null)
        {
            interactAction = playerInput.actions.FindAction("Interact");

            if (interactAction == null)
                interactAction = playerInput.actions.FindAction("Use");

            if (interactAction == null)
                interactAction = playerInput.actions.FindAction("Action");

            if (interactAction != null)
            {
                interactAction.performed += OnInteractPerformed;
                interactAction.Enable();
            }
            else
            {
                Debug.LogError("Не найдено действие для взаимодействия!");
            }
        }
    }

    private void OnInteractPerformed(InputAction.CallbackContext context)
    {
        if (playerInRange)
        {
            TryPickup();
        }
    }

    private void SetupAudio()
    {
        if (pickupSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.clip = pickupSound;
        }
    }

    private void CreatePrompt()
    {
        if (promptPrefab != null)
        {
            activePrompt = Instantiate(promptPrefab, transform.position + Vector3.up * 2f, Quaternion.identity);
            activePrompt.transform.SetParent(transform);

            TextMeshPro tmp = activePrompt.GetComponentInChildren<TextMeshPro>();
            if (tmp != null)
            {
                tmp.text = promptText;
            }

            activePrompt.SetActive(false);
        }
    }

    private void Update()
    {
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        CheckPlayerDistance();

        // Альтернативный ввод через клавиатуру
        if (playerInRange && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            TryPickup();
        }
    }

    private void CheckPlayerDistance()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);
        bool wasInRange = playerInRange;
        playerInRange = distance <= pickupRange;

        if (wasInRange != playerInRange && activePrompt != null)
        {
            activePrompt.SetActive(playerInRange);
        }
    }

    private void TryPickup()
    {
        if (playerBranches != null)
        {
            playerBranches.AddBranches(stickAmount);

            // Эффекты
            if (pickupEffect != null)
            {
                Instantiate(pickupEffect, transform.position, Quaternion.identity);
            }

            if (pickupSound != null && audioSource != null)
            {
                audioSource.Play();
                Destroy(gameObject, pickupSound.length);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }

    private void OnDestroy()
    {
        if (interactAction != null)
        {
            interactAction.performed -= OnInteractPerformed;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, pickupRange);
    }
}