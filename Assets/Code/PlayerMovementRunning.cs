using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovementRunning : MonoBehaviour
{
    [Header("Movement Settings")]
    public float speed = 5f;
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float laneChangeSpeed = 10f;
    [SerializeField] private float laneDistance = 2f;

    [Header("Lanes")]
    [SerializeField] private int currentLane = 1;
    [SerializeField] private int totalLanes = 3;

    [Header("Sound Settings")]
    [SerializeField] private AudioSource slidingAudioSource; // Отдельный источник для звука скольжения
    [SerializeField] private AudioSource jumpAudioSource;    // Отдельный источник для звука прыжка
    [SerializeField] private AudioClip slidingSound;
    [SerializeField] private AudioClip jumpSound;
    [SerializeField] private float slidingVolume = 0.5f;
    [SerializeField] private bool debugMode = true;

    [Header("Death Settings")]
    [SerializeField] private string deathTriggerTag = "Obstacle"; // Тег объектов, от которых умираем
    [SerializeField] private GameOverUI gameOverUI; // Ссылка на UI экрана смерти

    private PlayerInput playerInput;
    private InputAction jumpAction;
    private InputAction moveLeftAction;
    private InputAction moveRightAction;
    private CharacterController controller;
    private Vector3 velocity;
    private float targetX;
    private bool isGrounded;

    // Флаг смерти
    private bool isDead = false;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        playerInput = GetComponent<PlayerInput>();

        jumpAction = playerInput.actions["Jump"];
        moveLeftAction = playerInput.actions["MoveLeft"];
        moveRightAction = playerInput.actions["MoveRight"];

        targetX = CalculateLanePosition(currentLane);

        SetupAudioSources();
    }

    void Start()
    {
        // Находим GameOverUI, если не назначен в инспекторе
        if (gameOverUI == null)
        {
            gameOverUI = FindObjectOfType<GameOverUI>();
            if (debugMode) Debug.Log("GameOverUI found: " + (gameOverUI != null));
        }
    }

    void SetupAudioSources()
    {
        // СОЗДАЕМ ОТДЕЛЬНЫЙ AUDIOSOURCE ДЛЯ СКОЛЬЖЕНИЯ
        if (slidingAudioSource == null)
        {
            // Пробуем найти существующий
            slidingAudioSource = GetComponent<AudioSource>();

            // Если не нашли, создаем новый GameObject для звука
            if (slidingAudioSource == null)
            {
                GameObject soundObject = new GameObject("SlidingSound");
                soundObject.transform.parent = transform;
                soundObject.transform.localPosition = Vector3.zero;
                slidingAudioSource = soundObject.AddComponent<AudioSource>();
                if (debugMode) Debug.Log("Created dedicated SlidingSound object");
            }
        }

        // НАСТРАИВАЕМ AUDIOSOURCE ДЛЯ СКОЛЬЖЕНИЯ
        if (slidingAudioSource != null && slidingSound != null)
        {
            slidingAudioSource.clip = slidingSound;
            slidingAudioSource.loop = true;
            slidingAudioSource.volume = slidingVolume;
            slidingAudioSource.playOnAwake = true; // Включаем при старте
            slidingAudioSource.spatialBlend = 0; // 2D звук

            // ЗАПУСКАЕМ ЗВУК И НЕ ОСТАНАВЛИВАЕМ НИКОГДА
            slidingAudioSource.Play();

            if (debugMode) Debug.Log("🚀 Sliding sound started - WILL NEVER STOP");
        }
        else
        {
            Debug.LogError($"Cannot setup sliding sound: source={slidingAudioSource}, clip={slidingSound}");
        }

        // НАСТРАИВАЕМ AUDIOSOURCE ДЛЯ ПРЫЖКА (отдельный)
        if (jumpAudioSource == null)
        {
            jumpAudioSource = gameObject.AddComponent<AudioSource>();
        }

        if (jumpAudioSource != null)
        {
            jumpAudioSource.playOnAwake = false;
            jumpAudioSource.spatialBlend = 0;
        }
    }

    void OnEnable()
    {
        if (jumpAction != null) jumpAction.performed += OnJumpPerformed;
        if (moveLeftAction != null) moveLeftAction.performed += OnMoveLeftPerformed;
        if (moveRightAction != null) moveRightAction.performed += OnMoveRightPerformed;
    }

    void OnDisable()
    {
        if (jumpAction != null) jumpAction.performed -= OnJumpPerformed;
        if (moveLeftAction != null) moveLeftAction.performed -= OnMoveLeftPerformed;
        if (moveRightAction != null) moveRightAction.performed -= OnMoveRightPerformed;
    }

    void Update()
    {
        // Если игрок мертв - не обрабатываем движение
        if (isDead)
            return;

        isGrounded = controller.isGrounded;

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        // Движение к целевой полосе
        float step = laneChangeSpeed * Time.deltaTime;
        float newX = Mathf.MoveTowards(transform.position.x, targetX, step);
        float horizontalMove = newX - transform.position.x;

        // Движение вперед и гравитация
        Vector3 forwardMove = transform.forward * speed * Time.deltaTime;
        velocity.y += gravity * Time.deltaTime;
        Vector3 verticalMove = velocity * Time.deltaTime;

        Vector3 finalMove = forwardMove + new Vector3(horizontalMove, 0, 0) + verticalMove;
        controller.Move(finalMove);
    }

    private void OnMoveLeftPerformed(InputAction.CallbackContext context)
    {
        if (isDead) return;
        Debug.Log("← Move Left");
        ChangeLane(-1);
    }

    private void OnMoveRightPerformed(InputAction.CallbackContext context)
    {
        if (isDead) return;
        Debug.Log("→ Move Right");
        ChangeLane(1);
    }

    private void OnJumpPerformed(InputAction.CallbackContext context)
    {
        if (isDead) return;
        Debug.Log("↑ Jump");
        if (isGrounded)
        {
            velocity.y = jumpForce;

            // Воспроизводим звук прыжка на отдельном AudioSource
            if (jumpSound != null && jumpAudioSource != null)
            {
                jumpAudioSource.PlayOneShot(jumpSound, 0.8f);
                Debug.Log($"🔊 Playing jump sound");
            }
        }
    }

    private void ChangeLane(int direction)
    {
        int newLane = currentLane + direction;
        if (newLane >= 0 && newLane < totalLanes)
        {
            currentLane = newLane;
            targetX = CalculateLanePosition(currentLane);
            Debug.Log($"Lane changed to {currentLane}");
        }
    }

    private float CalculateLanePosition(int lane)
    {
        return (lane - (totalLanes - 1) / 2f) * laneDistance;
    }

    // МЕХАНИКА СМЕРТИ ЧЕРЕЗ ONTRIGGERENTER
    private void OnTriggerEnter(Collider other)
    {
        // Проверяем, не мертв ли уже игрок и есть ли нужный тег
        if (!isDead && other.CompareTag(deathTriggerTag))
        {
            Die();
        }
    }

    // Для 2D коллайдеров
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isDead && other.CompareTag(deathTriggerTag))
        {
            Die();
        }
    }

    private void Die()
    {
        if (isDead) return;

        isDead = true;

        Debug.Log("💀 Player died! Triggering Game Over screen...");

        // Останавливаем звук скольжения
        if (slidingAudioSource != null && slidingAudioSource.isPlaying)
        {
            slidingAudioSource.Stop();
        }

        // Отключаем управление через отключение компонента PlayerInput
        // или можно просто заблокировать через флаг isDead

        // Вызываем UI экрана смерти
        if (gameOverUI != null)
        {
            gameOverUI.ShowGameOverScreen();
            Debug.Log("📱 Game Over UI shown");
        }
        else
        {
            Debug.LogError("❌ GameOverUI reference is missing! Please assign it in the inspector or make sure it exists in the scene.");
        }

        // Дополнительно: можно отключить движение полностью
        // GetComponent<PlayerInput>().enabled = false;

        // Здесь можно добавить анимацию смерти, звук смерти и т.д.
    }

    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying)
        {
            Gizmos.color = Color.yellow;
            for (int i = 0; i < totalLanes; i++)
            {
                float x = (i - (totalLanes - 1) / 2f) * laneDistance;
                Vector3 lanePos = transform.position + new Vector3(x, 0, 5);
                Gizmos.DrawWireCube(lanePos, new Vector3(1, 0.1f, 10));
            }
        }
    }
}