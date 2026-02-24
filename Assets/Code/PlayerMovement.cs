using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.Collections;

namespace PlayerMovement
{
    [RequireComponent(typeof(CharacterController), typeof(PlayerInput))]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float walkSpeed = 5f;
        [SerializeField] private float runSpeed = 10f;
        [SerializeField] private float jumpHeight = 1.25f;
        [SerializeField] private float gravity = -9.81f;

        [Header("Ground Check")]
        [SerializeField] private Transform groundCheckTransform;
        [SerializeField] private float groundCheckRadius = 0.28f;
        [SerializeField] private LayerMask groundLayers = ~0;

        [Header("Mouse Settings")]
        [SerializeField] private float mouseSensitivity = 2f;
        [SerializeField] private bool invertY = false;
        [SerializeField] private bool cursorLocked = true;

        [Header("Camera Bobbing")]
        [SerializeField] private bool enableCameraBobbing = true;
        [SerializeField] private float walkBobbingSpeed = 10f;
        [SerializeField] private float runBobbingSpeed = 15f;
        [SerializeField] private float walkBobbingAmount = 0.05f;
        [SerializeField] private float runBobbingAmount = 0.1f;
        [SerializeField] private float bobbingSmoothing = 10f;

        [Header("Camera Effects")]
        [SerializeField] private bool enableBreathingEffect = true;
        [SerializeField] private float breathingSpeed = 2f;
        [SerializeField] private float breathingAmount = 0.02f;

        [Header("Footstep Sounds")]
        [SerializeField] private AudioSource footstepAudioSource;
        [SerializeField] private AudioClip[] footstepClips;
        [SerializeField] private float walkStepInterval = 0.5f;
        [SerializeField] private float runStepInterval = 0.3f;
        [SerializeField][Range(0f, 1f)] private float footstepVolume = 0.5f;
        [SerializeField] private float pitchMin = 0.9f;
        [SerializeField] private float pitchMax = 1.1f;

        [Header("Snowball Settings")]
        [SerializeField] private GameObject snowballPrefab;
        [SerializeField] private Transform throwPoint;
        [SerializeField] private float snowballSpeed = 30f;
        [SerializeField] private float throwRate = 0.2f;
        [SerializeField] private float snowballLifetime = 3f;
        [SerializeField] private int maxSnowballs = 10;
        [SerializeField] private bool infiniteSnowballs = false;

        [Header("Snowball Effects")]
        [SerializeField] private AudioClip throwSound;
        [SerializeField][Range(0f, 1f)] private float throwVolume = 0.7f;
        [SerializeField] private ParticleSystem throwEffect;

        [Header("Score Settings")]
        [SerializeField] private int currentScore = 0;
        [SerializeField] private int totalScore = 27;

        [Header("Scene Loading")]
        [SerializeField] private int nextSceneIndex = 1;
        [SerializeField] private float sceneLoadDelay = 1f;

        [Header("=== МЕНЮ ПОДТВЕРЖДЕНИЯ ===")]
        [SerializeField] private ConfirmMenu confirmMenu;
        [SerializeField] private string victoryMessage = "Поздравляем! Вы уничтожили всех снеговиков! Хотите продолжить?";
        [SerializeField] private string victoryNextScene = "Level2"; // Или оставьте пустым, если не нужно
        [SerializeField] private bool showVictoryMenu = true;

        // Components
        private CharacterController _controller;
        private PlayerInput _playerInput;
        private Camera _playerCamera;

        // Movement variables
        private Vector2 _moveInput;
        private Vector2 _lookInput;
        private Vector3 _playerVelocity;
        private float _cameraPitch;
        private bool _isGrounded;
        private bool _isRunning;

        // Footstep variables
        private float _stepTimer;
        private int _lastFootstepIndex = -1;

        // Snowball variables
        private int _currentSnowballs;
        private float _nextThrowTime;
        private AudioSource _throwAudioSource;

        // Camera bobbing variables
        private float _bobbingTimer;
        private float _currentBobbingSpeed;
        private float _currentBobbingAmount;
        private Vector3 _initialCameraPosition;
        private Vector3 _cameraTargetPosition;
        private float _breathingTimer;

        // Victory flag
        private bool _victoryAchieved = false;

        // Events
        public delegate void ScoreChangedHandler(int newScore);
        public event ScoreChangedHandler OnScoreChanged;

        public delegate void VictoryAchievedHandler();
        public event VictoryAchievedHandler OnVictoryAchieved;

        // Public properties
        public int CurrentScore
        {
            get { return currentScore; }
            private set { currentScore = value; }
        }

        public int TotalScore
        {
            get { return totalScore; }
            private set { totalScore = value; }
        }

        private float CurrentSpeed
        {
            get { return _isRunning ? runSpeed : walkSpeed; }
        }

        private float CurrentStepInterval
        {
            get { return _isRunning ? runStepInterval : walkStepInterval; }
        }

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _playerInput = GetComponent<PlayerInput>();
            _playerCamera = GetComponentInChildren<Camera>();

            if (_playerCamera == null)
            {
                Debug.LogError("No camera found as child of player! Please add a camera.");
            }

            SetupFootstepAudio();
            SetupThrowAudio();
            InitializeSnowballs();

            SetupInputCallbacks();

            // Camera bobbing initialization
            if (_playerCamera != null)
            {
                _initialCameraPosition = _playerCamera.transform.localPosition;
            }
        }

        private void SetupFootstepAudio()
        {
            if (footstepAudioSource == null)
            {
                footstepAudioSource = GetComponent<AudioSource>();
                if (footstepAudioSource == null)
                {
                    footstepAudioSource = gameObject.AddComponent<AudioSource>();
                }
            }

            footstepAudioSource.playOnAwake = false;
            footstepAudioSource.loop = false;
            footstepAudioSource.spatialBlend = 1f;
            footstepAudioSource.volume = footstepVolume;
        }

        private void SetupThrowAudio()
        {
            _throwAudioSource = gameObject.AddComponent<AudioSource>();
            _throwAudioSource.playOnAwake = false;
            _throwAudioSource.loop = false;
            _throwAudioSource.spatialBlend = 1f;
            _throwAudioSource.volume = throwVolume;
        }

        private void InitializeSnowballs()
        {
            _currentSnowballs = maxSnowballs;
        }

        private void Start()
        {
            ApplyCursorState();

            if (snowballPrefab == null)
            {
                Debug.LogWarning("Snowball Prefab is not assigned! Throwing snowballs will not work.");
            }

            if (throwPoint == null)
            {
                if (_playerCamera != null)
                {
                    throwPoint = _playerCamera.transform;
                }
                else
                {
                    Debug.LogWarning("Throw Point is not assigned! Throwing snowballs will not work.");
                }
            }

            CountTotalSnowmen();
        }

        private void CountTotalSnowmen()
        {
            SnowballTarget[] allSnowmen = FindObjectsOfType<SnowballTarget>();
            totalScore = allSnowmen.Length;
            currentScore = 5;

            Debug.Log($"Total snowmen in level: {totalScore}");
            OnScoreChanged?.Invoke(currentScore);
        }

        private void SetupInputCallbacks()
        {
            _playerInput.actions["Move"].performed += ctx => _moveInput = ctx.ReadValue<Vector2>();
            _playerInput.actions["Move"].canceled += ctx => _moveInput = Vector2.zero;

            _playerInput.actions["Look"].performed += ctx => _lookInput = ctx.ReadValue<Vector2>();
            _playerInput.actions["Look"].canceled += ctx => _lookInput = Vector2.zero;

            _playerInput.actions["Jump"].performed += OnJumpPerformed;

            _playerInput.actions["Run"].performed += ctx => _isRunning = true;
            _playerInput.actions["Run"].canceled += ctx => _isRunning = false;

            _playerInput.actions["ToggleCursor"].performed += ctx => {
                cursorLocked = !cursorLocked;
                ApplyCursorState();
            };

            _playerInput.actions["Throw"].performed += OnThrowPerformed;
        }

        private void OnDestroy()
        {
            if (_playerInput != null)
            {
                _playerInput.actions["Move"].performed -= ctx => _moveInput = ctx.ReadValue<Vector2>();
                _playerInput.actions["Move"].canceled -= ctx => _moveInput = Vector2.zero;
                _playerInput.actions["Jump"].performed -= OnJumpPerformed;
                _playerInput.actions["Throw"].performed -= OnThrowPerformed;
            }

            // Очищаем события
            if (confirmMenu != null)
            {
                confirmMenu.RemoveConfirmListener(OnVictoryConfirm);
                confirmMenu.RemoveCancelListener(OnVictoryCancel);
            }
        }

        private void Update()
        {
            CheckGround();
            ApplyGravity();
            MovePlayer();
            HandleFootsteps();
            HandleMouseLook();
            HandleCameraBobbing();

            if (enableBreathingEffect)
            {
                HandleBreathingEffect();
            }
        }

        private void CheckGround()
        {
            _isGrounded = Physics.CheckSphere(
                groundCheckTransform.position,
                groundCheckRadius,
                groundLayers,
                QueryTriggerInteraction.Ignore
            );
        }

        private void ApplyGravity()
        {
            _playerVelocity.y += gravity * Time.deltaTime;

            if (_isGrounded && _playerVelocity.y < 0)
            {
                _playerVelocity.y = -2f;
            }

            _controller.Move(_playerVelocity * Time.deltaTime);
        }

        private void MovePlayer()
        {
            if (_moveInput == Vector2.zero) return;

            var moveDirection = new Vector3(_moveInput.x, 0, _moveInput.y);
            moveDirection = transform.TransformDirection(moveDirection);

            _controller.Move(moveDirection * (CurrentSpeed * Time.deltaTime));
        }

        private void HandleFootsteps()
        {
            bool isMoving = _moveInput != Vector2.zero && _isGrounded;

            if (isMoving)
            {
                _stepTimer -= Time.deltaTime;

                if (_stepTimer <= 0f)
                {
                    PlayFootstepSound();
                    _stepTimer = CurrentStepInterval;
                }
            }
            else
            {
                _stepTimer = 0f;
            }
        }

        private void PlayFootstepSound()
        {
            if (footstepAudioSource == null || footstepClips == null || footstepClips.Length == 0)
                return;

            int randomIndex;
            do
            {
                randomIndex = Random.Range(0, footstepClips.Length);
            } while (footstepClips.Length > 1 && randomIndex == _lastFootstepIndex);

            AudioClip clipToPlay = footstepClips[randomIndex];
            _lastFootstepIndex = randomIndex;

            footstepAudioSource.pitch = Random.Range(pitchMin, pitchMax);
            footstepAudioSource.volume = footstepVolume;
            footstepAudioSource.PlayOneShot(clipToPlay);
        }

        private void HandleMouseLook()
        {
            if (_playerCamera == null || _lookInput == Vector2.zero || !cursorLocked) return;

            transform.Rotate(Vector3.up, _lookInput.x * mouseSensitivity);

            float verticalDelta = _lookInput.y * mouseSensitivity * (invertY ? 1 : -1);
            _cameraPitch = Mathf.Clamp(_cameraPitch + verticalDelta, -90f, 90f);
            _playerCamera.transform.localRotation = Quaternion.Euler(_cameraPitch, 0, 0);
        }

        private void HandleCameraBobbing()
        {
            if (_playerCamera == null || !enableCameraBobbing) return;

            bool isMoving = _moveInput != Vector2.zero && _isGrounded;

            if (isMoving)
            {
                // Устанавливаем скорость и амплитуду в зависимости от состояния бега
                _currentBobbingSpeed = _isRunning ? runBobbingSpeed : walkBobbingSpeed;
                _currentBobbingAmount = _isRunning ? runBobbingAmount : walkBobbingAmount;

                // Увеличиваем таймер
                _bobbingTimer += Time.deltaTime * _currentBobbingSpeed;

                // Рассчитываем покачивание по синусоиде
                float sinWave = Mathf.Sin(_bobbingTimer);
                float cosWave = Mathf.Cos(_bobbingTimer * 0.5f);

                // Вертикальное покачивание (вверх-вниз)
                float verticalBob = sinWave * _currentBobbingAmount;

                // Горизонтальное покачивание (влево-вправо) - поменьше
                float horizontalBob = cosWave * (_currentBobbingAmount * 0.3f);

                // Применяем покачивание
                _cameraTargetPosition = _initialCameraPosition + new Vector3(horizontalBob, verticalBob, 0);
            }
            else
            {
                // Если стоим на месте - плавно возвращаем камеру в исходное положение
                _bobbingTimer = 0;
                _cameraTargetPosition = _initialCameraPosition;
            }

            // Плавно интерполируем к целевой позиции
            _playerCamera.transform.localPosition = Vector3.Lerp(
                _playerCamera.transform.localPosition,
                _cameraTargetPosition,
                Time.deltaTime * bobbingSmoothing
            );
        }

        private void HandleBreathingEffect()
        {
            if (_playerCamera == null || !enableBreathingEffect) return;

            // Эффект легкого покачивания даже когда игрок стоит
            _breathingTimer += Time.deltaTime * breathingSpeed;

            // Очень легкое синусоидальное движение
            float breathOffset = Mathf.Sin(_breathingTimer) * breathingAmount;

            // Применяем эффект дыхания только если нет активного покачивания от ходьбы
            if (_moveInput == Vector2.zero)
            {
                Vector3 breathPosition = _initialCameraPosition;
                breathPosition.y += breathOffset;

                _playerCamera.transform.localPosition = Vector3.Lerp(
                    _playerCamera.transform.localPosition,
                    breathPosition,
                    Time.deltaTime * bobbingSmoothing * 0.5f
                );
            }
        }

        private void OnJumpPerformed(InputAction.CallbackContext context)
        {
            if (_isGrounded)
            {
                _playerVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
        }

        private void OnThrowPerformed(InputAction.CallbackContext context)
        {
            if (!cursorLocked || Time.time < _nextThrowTime || _victoryAchieved) return;

            if (!infiniteSnowballs && _currentSnowballs <= 0)
            {
                Debug.Log("No snowballs left!");
                return;
            }

            ThrowSnowball();
            _nextThrowTime = Time.time + throwRate;

            if (!infiniteSnowballs)
            {
                _currentSnowballs--;
            }
        }

        private void ThrowSnowball()
        {
            if (snowballPrefab == null || throwPoint == null) return;

            PlayThrowSound();
            PlayThrowEffect();

            Vector3 throwDirection = GetThrowDirection();
            Vector3 throwOrigin = throwPoint.position;

            SpawnSnowball(throwOrigin, throwDirection);
        }

        private Vector3 GetThrowDirection()
        {
            if (_playerCamera != null)
            {
                return _playerCamera.transform.forward;
            }
            return transform.forward;
        }

        private void SpawnSnowball(Vector3 origin, Vector3 direction)
        {
            GameObject snowball = Instantiate(snowballPrefab, origin, Quaternion.LookRotation(direction));

            StraightSnowball snowballComponent = snowball.GetComponent<StraightSnowball>();
            if (snowballComponent == null)
            {
                snowballComponent = snowball.AddComponent<StraightSnowball>();
            }

            snowballComponent.Initialize(direction * snowballSpeed, snowballLifetime, this);

            Destroy(snowball, snowballLifetime);
        }

        private void PlayThrowSound()
        {
            if (_throwAudioSource != null && throwSound != null)
            {
                _throwAudioSource.pitch = Random.Range(0.9f, 1.1f);
                _throwAudioSource.PlayOneShot(throwSound);
            }
        }

        private void PlayThrowEffect()
        {
            if (throwEffect != null && throwPoint != null)
            {
                throwEffect.transform.position = throwPoint.position;
                throwEffect.Play();
            }
        }

        public void AddScore(int amount)
        {
            currentScore += amount;

            Debug.Log($"Snowman killed! Current: {currentScore}, Total: {totalScore}");

            OnScoreChanged?.Invoke(currentScore);

            CheckAllSnowmenDestroyed();
        }

        private void CheckAllSnowmenDestroyed()
        {
            if (currentScore >= totalScore && totalScore > 0 && !_victoryAchieved)
            {
                _victoryAchieved = true;
                Debug.Log("All snowmen destroyed! Showing victory menu...");

                OnVictoryAchieved?.Invoke();

                if (showVictoryMenu && confirmMenu != null)
                {
                    ShowVictoryMenu();
                }
                else
                {
                    // Если меню не назначено, просто загружаем следующую сцену
                    StartCoroutine(LoadSceneCoroutine());
                }
            }
        }

        private void ShowVictoryMenu()
        {
            if (confirmMenu == null)
            {
                Debug.LogWarning("ConfirmMenu not assigned! Loading next scene directly.");
                StartCoroutine(LoadSceneCoroutine());
                return;
            }

            // Настраиваем события меню
            SetupVictoryMenuEvents();

            // Разблокируем курсор для меню
            cursorLocked = false;
            ApplyCursorState();

            // Показываем меню с сообщением о победе
            if (!string.IsNullOrEmpty(victoryNextScene))
            {
                confirmMenu.ShowMenu(victoryMessage, victoryNextScene);
            }
            else
            {
                confirmMenu.ShowMenu(victoryMessage);
            }
        }

        private void SetupVictoryMenuEvents()
        {
            if (confirmMenu == null) return;

            // Очищаем предыдущие события
            confirmMenu.RemoveConfirmListener(OnVictoryConfirm);
            confirmMenu.RemoveCancelListener(OnVictoryCancel);

            // Добавляем новые обработчики
            confirmMenu.AddConfirmListener(OnVictoryConfirm);
            confirmMenu.AddCancelListener(OnVictoryCancel);
        }

        private void OnVictoryConfirm()
        {
            Debug.Log("Victory confirmed! Loading next scene...");

            // Очищаем события
            if (confirmMenu != null)
            {
                confirmMenu.RemoveConfirmListener(OnVictoryConfirm);
                confirmMenu.RemoveCancelListener(OnVictoryCancel);
            }

            // Загружаем следующую сцену
            if (!string.IsNullOrEmpty(victoryNextScene))
            {
                SceneManager.LoadScene(victoryNextScene);
            }
            else if (nextSceneIndex >= 0)
            {
                SceneManager.LoadScene(nextSceneIndex);
            }
        }

        private void OnVictoryCancel()
        {
            Debug.Log("Victory cancelled. Continuing current level...");

            // Возвращаем курсор в заблокированное состояние
            cursorLocked = true;
            ApplyCursorState();

            // Очищаем события
            if (confirmMenu != null)
            {
                confirmMenu.RemoveConfirmListener(OnVictoryConfirm);
                confirmMenu.RemoveCancelListener(OnVictoryCancel);
            }

            // Можно добавить логику для рестарта уровня или других действий
            // Например: SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private IEnumerator LoadSceneCoroutine()
        {
            yield return new WaitForSeconds(sceneLoadDelay);

            if (!string.IsNullOrEmpty(victoryNextScene))
            {
                SceneManager.LoadScene(victoryNextScene);
            }
            else
            {
                SceneManager.LoadScene(nextSceneIndex);
            }
        }

        public void AddSnowballs(int amount)
        {
            _currentSnowballs = Mathf.Min(_currentSnowballs + amount, maxSnowballs);
        }

        public int GetCurrentSnowballs()
        {
            return _currentSnowballs;
        }

        public int GetCurrentScore()
        {
            return currentScore;
        }

        public void SetNextSceneIndex(int index)
        {
            nextSceneIndex = index;
        }

        public void SetVictoryNextScene(string sceneName)
        {
            victoryNextScene = sceneName;
        }

        public void ResetScore()
        {
            currentScore = 0;
            totalScore = 0;
            _victoryAchieved = false;
            OnScoreChanged?.Invoke(currentScore);
        }

        private void ApplyCursorState()
        {
            Cursor.lockState = cursorLocked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !cursorLocked;
        }

        private void OnDrawGizmosSelected()
        {
            if (groundCheckTransform == null) return;

            Gizmos.color = _isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheckTransform.position, groundCheckRadius);

            if (_playerCamera != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawRay(_playerCamera.transform.position, _playerCamera.transform.forward * 5f);
            }
        }
    }

    public class StraightSnowball : MonoBehaviour
    {
        private Vector3 _velocity;
        private float _maxLifetime;
        private float _spawnTime;
        private bool _hasHit = false;
        private PlayerMovement _player;

        [Header("Snowball Effects")]
        [SerializeField] private GameObject hitEffect;
        [SerializeField] private AudioClip hitSound;
        [SerializeField] private float hitSoundVolume = 0.5f;

        private Rigidbody _rb;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
        }

        private void Start()
        {
            _spawnTime = Time.time;
            gameObject.tag = "Snowball";
        }

        public void Initialize(Vector3 initialVelocity, float lifetime, PlayerMovement player)
        {
            _velocity = initialVelocity;
            _maxLifetime = lifetime;
            _player = player;

            if (_rb != null)
            {
                _rb.linearVelocity = initialVelocity;
                _rb.useGravity = false;
            }
        }

        private void Update()
        {
            if (_hasHit) return;

            if (_rb == null)
            {
                transform.position += _velocity * Time.deltaTime;
            }

            if (Time.time - _spawnTime > _maxLifetime)
            {
                Destroy(gameObject);
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (_hasHit) return;

            SnowballTarget target = collision.collider.GetComponent<SnowballTarget>();
            if (target != null)
            {
                target.TakeDamage(1, _player);
            }

            _hasHit = true;

            if (hitEffect != null)
                Instantiate(hitEffect, collision.contacts[0].point, Quaternion.identity);

            if (hitSound != null)
                AudioSource.PlayClipAtPoint(hitSound, transform.position, hitSoundVolume);

            Destroy(gameObject);
        }
    }
}