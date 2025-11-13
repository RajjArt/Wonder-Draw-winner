using UnityEngine;
using System.Collections;

/// <summary>
/// Enhanced Polished Character Controller - Complete System: Multi-character support with runtime integration
/// Advanced character controller designed for multiple webcam-generated characters with optimized performance
/// Integrates with MultipleCharacterManager and CharacterAreaPositioning systems
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class EnhancedPolishedCharacterController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float acceleration = 8f;
    [SerializeField] private float deceleration = 10f;
    [SerializeField] private bool enableDiagonalMovement = true;
    [SerializeField] private bool normalizeDiagonalSpeed = true;
    
    [Header("Physics Settings")]
    [SerializeField] private bool autoSetupPhysics = true;
    [SerializeField] private float gravityScale = 0f;
    [SerializeField] private bool freezeRotation = true;
    [SerializeField] private RigidbodyInterpolation2D interpolation = RigidbodyInterpolation2D.Interpolate;
    [SerializeField] private CollisionDetectionMode2D collisionMode = CollisionDetectionMode2D.Continuous;
    
    [Header("Character Identity")]
    [SerializeField] private string characterId = "";
    [SerializeField] private string characterName = "";
    [SerializeField] private bool allowInput = true;
    [SerializeField] private bool isPlayerControlled = false;
    
    [Header("Performance Settings")]
    [SerializeField] private bool enableLOD = true;
    [SerializeField] private float lodDistance = 15f;
    [SerializeField] private bool enableMovementSmoothing = true;
    [SerializeField] private float smoothTime = 0.1f;
    
    [Header("Visual Effects")]
    [SerializeField] private bool enableVisualFeedback = true;
    [SerializeField] private Color movementColor = Color.green;
    [SerializeField] private Color idleColor = Color.white;
    [SerializeField] private float colorLerpSpeed = 5f;
    
    [Header("Area Constraints")]
    [SerializeField] private bool enableAreaConstraints = true;
    [SerializeField] private CharacterAreaPositioning areaPositioning;
    
    // Core components
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Vector2 currentVelocity;
    private Vector2 targetVelocity;
    
    // Movement tracking
    private Vector2 lastPosition;
    private float lastMovementTime;
    private bool isMoving = false;
    
    // LOD and performance
    private float distanceToCamera;
    private bool isLODActive = false;
    private Vector2 smoothVelocity;
    
    // Input handling
    private float horizontalInput;
    private float verticalInput;
    
    // Event system
    public System.Action<string> OnCharacterMoved;
    public System.Action<string> OnCharacterStopped;
    public System.Action<float> OnLODChanged;
    public System.Action<string> OnStatusUpdate;
    
    // Properties
    public string CharacterId => characterId;
    public string CharacterName => characterName;
    public bool IsMoving => isMoving;
    public float CurrentSpeed => rb.linearVelocity.magnitude;
    public Vector2 CurrentVelocity => rb.linearVelocity;
    public bool IsLODActive => isLODActive;
    public float DistanceToCamera => distanceToCamera;
    
    private void Awake()
    {
        InitializeComponents();
        SetupCharacterIdentity();
    }
    
    private void Start()
    {
        SetupPhysics();
        InitializeMovement();
        FindAreaPositioning();
        
        Debug.Log($"🎮 Enhanced character controller initialized: {characterName} ({characterId})");
    }
    
    private void Update()
    {
        if (areaPositioning != null)
        {
            UpdateDistanceToCamera();
            CheckLODStatus();
        }
        
        if (enableVisualFeedback)
        {
            UpdateVisualFeedback();
        }
        
        UpdateMovementState();
    }
    
    private void FixedUpdate()
    {
        if (!allowInput || isLODActive)
        {
            ApplyDeceleration();
            return;
        }
        
        HandleInput();
        ApplyMovement();
        ApplyAreaConstraints();
    }
    
    /// <summary>
    /// Initialize core components
    /// </summary>
    private void InitializeComponents()
    {
        // Get or add required components
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }
        
        // Initialize movement tracking
        lastPosition = transform.position;
        lastMovementTime = Time.time;
    }
    
    /// <summary>
    /// Setup character identity and metadata
    /// </summary>
    private void SetupCharacterIdentity()
    {
        if (string.IsNullOrEmpty(characterId))
        {
            characterId = $"Character_{System.DateTime.Now.Ticks}";
        }
        
        if (string.IsNullOrEmpty(characterName))
        {
            characterName = $"Character_{gameObject.GetInstanceID()}";
        }
        
        gameObject.name = characterName;
    }
    
    /// <summary>
    /// Setup physics components
    /// </summary>
    public void SetupPhysics()
    {
        if (!autoSetupPhysics) return;
        
        // Configure Rigidbody2D
        rb.gravityScale = gravityScale;
        rb.freezeRotation = freezeRotation;
        rb.interpolation = interpolation;
        rb.collisionDetectionMode = collisionMode;
        
        // Add components if missing
        if (GetComponent<CircleCollider2D>() == null)
        {
            CircleCollider2D collider = gameObject.AddComponent<CircleCollider2D>();
            collider.radius = 0.5f;
        }
        
        Debug.Log($"⚖️ Physics setup completed for {characterName}");
    }
    
    /// <summary>
    /// Initialize movement system
    /// </summary>
    private void InitializeMovement()
    {
        currentVelocity = Vector2.zero;
        targetVelocity = Vector2.zero;
        smoothVelocity = Vector2.zero;
        
        // Set initial color
        if (spriteRenderer != null)
        {
            spriteRenderer.color = idleColor;
        }
    }
    
    /// <summary>
    /// Find area positioning system
    /// </summary>
    private void FindAreaPositioning()
    {
        if (areaPositioning == null)
        {
            areaPositioning = FindFirstObjectByType<CharacterAreaPositioning>();
        }
    }
    
    /// <summary>
    /// Handle input for character movement
    /// </summary>
    private void HandleInput()
    {
        // Get input axes
        horizontalInput = Input.GetAxis("Horizontal");
        verticalInput = Input.GetAxis("Vertical");
        
        // Alternative input methods
        if (isPlayerControlled)
        {
            // WASD keys
            if (Input.GetKey(KeyCode.W)) verticalInput = Mathf.Max(verticalInput, 1f);
            if (Input.GetKey(KeyCode.S)) verticalInput = Mathf.Min(verticalInput, -1f);
            if (Input.GetKey(KeyCode.A)) horizontalInput = Mathf.Min(horizontalInput, -1f);
            if (Input.GetKey(KeyCode.D)) horizontalInput = Mathf.Max(horizontalInput, 1f);
        }
        else
        {
            // Arrow keys for non-player characters
            if (Input.GetKey(KeyCode.UpArrow)) verticalInput = Mathf.Max(verticalInput, 1f);
            if (Input.GetKey(KeyCode.DownArrow)) verticalInput = Mathf.Min(verticalInput, -1f);
            if (Input.GetKey(KeyCode.LeftArrow)) horizontalInput = Mathf.Min(horizontalInput, -1f);
            if (Input.GetKey(KeyCode.RightArrow)) horizontalInput = Mathf.Max(horizontalInput, 1f);
        }
        
        // Create movement vector
        targetVelocity = new Vector2(horizontalInput, verticalInput);
        
        // Normalize diagonal movement
        if (enableDiagonalMovement && normalizeDiagonalSpeed && targetVelocity.magnitude > 1f)
        {
            targetVelocity.Normalize();
        }
        
        // Apply speed
        targetVelocity *= moveSpeed;
    }
    
    /// <summary>
    /// Apply movement to rigidbody
    /// </summary>
    private void ApplyMovement()
    {
        if (enableMovementSmoothing)
        {
            // Smooth velocity changes
            currentVelocity = Vector2.SmoothDamp(currentVelocity, targetVelocity, ref smoothVelocity, smoothTime);
            rb.linearVelocity = currentVelocity;
        }
        else
        {
            // Direct velocity setting
            currentVelocity = targetVelocity;
            rb.linearVelocity = currentVelocity;
        }
    }
    
    /// <summary>
    /// Apply deceleration when no input
    /// </summary>
    private void ApplyDeceleration()
    {
        if (currentVelocity.magnitude > 0.1f)
        {
            currentVelocity = Vector2.Lerp(currentVelocity, Vector2.zero, deceleration * Time.fixedDeltaTime);
            rb.linearVelocity = currentVelocity;
        }
        else
        {
            currentVelocity = Vector2.zero;
            rb.linearVelocity = Vector2.zero;
        }
    }
    
    /// <summary>
    /// Apply area constraints
    /// </summary>
    private void ApplyAreaConstraints()
    {
        if (!enableAreaConstraints || areaPositioning == null) return;
        
        // Get current position
        Vector2 position = rb.position;
        
        // Simple boundary constraints (can be enhanced with actual area bounds)
        if (Mathf.Abs(position.x) > 50f || Mathf.Abs(position.y) > 50f)
        {
            // Constrain to reasonable bounds
            position.x = Mathf.Clamp(position.x, -50f, 50f);
            position.y = Mathf.Clamp(position.y, -50f, 50f);
            rb.position = position;
        }
    }
    
    /// <summary>
    /// Update distance to camera for LOD
    /// </summary>
    private void UpdateDistanceToCamera()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            distanceToCamera = Vector3.Distance(transform.position, mainCamera.transform.position);
        }
    }
    
    /// <summary>
    /// Check and update LOD status
    /// </summary>
    private void CheckLODStatus()
    {
        if (!enableLOD) return;
        
        bool shouldBeLOD = distanceToCamera > lodDistance;
        
        if (shouldBeLOD != isLODActive)
        {
            isLODActive = shouldBeLOD;
            
            if (isLODActive)
            {
                // Reduce movement processing
                allowInput = false;
                OnLODChanged?.Invoke(lodDistance);
            }
            else
            {
                // Restore full functionality
                allowInput = true;
                OnLODChanged?.Invoke(0f);
            }
        }
    }
    
    /// <summary>
    /// Update visual feedback based on movement state
    /// </summary>
    private void UpdateVisualFeedback()
    {
        if (spriteRenderer == null) return;
        
        Color targetColor = isMoving ? movementColor : idleColor;
        spriteRenderer.color = Color.Lerp(spriteRenderer.color, targetColor, colorLerpSpeed * Time.deltaTime);
    }
    
    /// <summary>
    /// Update movement state tracking
    /// </summary>
    private void UpdateMovementState()
    {
        Vector2 currentPosition = transform.position;
        float positionDelta = Vector2.Distance(currentPosition, lastPosition);
        
        // Update movement state
        bool wasMoving = isMoving;
        isMoving = positionDelta > 0.01f; // Small threshold to avoid floating point issues
        
        if (isMoving != wasMoving)
        {
            if (isMoving)
            {
                OnCharacterMoved?.Invoke(characterId);
            }
            else
            {
                OnCharacterStopped?.Invoke(characterId);
            }
        }
        
        lastPosition = currentPosition;
        
        if (isMoving)
        {
            lastMovementTime = Time.time;
        }
    }
    
    /// <summary>
    /// Set character ID for multi-character support
    /// </summary>
    public void SetCharacterId(string id)
    {
        characterId = id;
        UpdateStatusUI($"Character ID set: {id}");
    }
    
    /// <summary>
    /// Set character name
    /// </summary>
    public void SetCharacterName(string name)
    {
        characterName = name;
        gameObject.name = name;
        UpdateStatusUI($"Character name set: {name}");
    }
    
    /// <summary>
    /// Enable or disable input for this character
    /// </summary>
    public void SetAllowInput(bool allow)
    {
        allowInput = allow;
        
        if (!allow && isMoving)
        {
            ApplyDeceleration();
        }
        
        UpdateStatusUI($"Input {(allow ? "enabled" : "disabled")} for {characterName}");
    }
    
    /// <summary>
    /// Set this character as player controlled
    /// </summary>
    public void SetPlayerControlled(bool player)
    {
        isPlayerControlled = player;
        UpdateStatusUI($"{characterName} is now {(player ? "player" : "AI")} controlled");
    }
    
    /// <summary>
    /// Force move character to a specific position
    /// </summary>
    public void ForceMoveTo(Vector3 targetPosition)
    {
        if (rb != null)
        {
            rb.position = new Vector2(targetPosition.x, targetPosition.y);
        }
        else
        {
            transform.position = targetPosition;
        }
        
        UpdateStatusUI($"Forced move to: {targetPosition}");
    }
    
    /// <summary>
    /// Apply impulse force to character
    /// </summary>
    public void ApplyImpulse(Vector2 force)
    {
        if (rb != null)
        {
            rb.AddForce(force, ForceMode2D.Impulse);
        }
        
        UpdateStatusUI($"Impulse applied: {force}");
    }
    
    /// <summary>
    /// Copy settings from another character controller
    /// </summary>
    public void CopySettingsFrom(EnhancedPolishedCharacterController other)
    {
        if (other == null) return;
        
        moveSpeed = other.moveSpeed;
        acceleration = other.acceleration;
        deceleration = other.deceleration;
        enableDiagonalMovement = other.enableDiagonalMovement;
        normalizeDiagonalSpeed = other.normalizeDiagonalSpeed;
        
        gravityScale = other.gravityScale;
        freezeRotation = other.freezeRotation;
        interpolation = other.interpolation;
        collisionMode = other.collisionMode;
        
        enableLOD = other.enableLOD;
        lodDistance = other.lodDistance;
        enableMovementSmoothing = other.enableMovementSmoothing;
        smoothTime = other.smoothTime;
        
        enableVisualFeedback = other.enableVisualFeedback;
        movementColor = other.movementColor;
        idleColor = other.idleColor;
        colorLerpSpeed = other.colorLerpSpeed;
        
        Debug.Log($"⚙️ Settings copied from {other.characterName} to {characterName}");
    }
    
    /// <summary>
    /// Auto setup physics components
    /// </summary>
    public void AutoSetupPhysics()
    {
        autoSetupPhysics = true;
        SetupPhysics();
    }
    
    /// <summary>
    /// Get character statistics
    /// </summary>
    public string GetCharacterStatistics()
    {
        return $"Name: {characterName}\n" +
               $"ID: {characterId}\n" +
               $"Position: {transform.position}\n" +
               $"Velocity: {CurrentVelocity}\n" +
               $"Speed: {CurrentSpeed:F2}\n" +
               $"Moving: {isMoving}\n" +
               $"LOD Active: {isLODActive}\n" +
               $"Distance to Camera: {distanceToCamera:F1}\n" +
               $"Input Enabled: {allowInput}\n" +
               $"Player Controlled: {isPlayerControlled}";
    }
    
    /// <summary>
    /// Reset character to initial state
    /// </summary>
    public void ResetCharacter()
    {
        rb.linearVelocity = Vector2.zero;
        currentVelocity = Vector2.zero;
        targetVelocity = Vector2.zero;
        smoothVelocity = Vector2.zero;
        
        isMoving = false;
        isLODActive = false;
        allowInput = true;
        
        if (spriteRenderer != null)
        {
            spriteRenderer.color = idleColor;
        }
        
        UpdateStatusUI($"Character {characterName} reset");
    }
    
    private void UpdateStatusUI(string message)
    {
        OnStatusUpdate?.Invoke(message);
        Debug.Log($"🎮 {characterName}: {message}");
    }
    
    private void OnDrawGizmosSelected()
    {
        // Draw LOD distance
        if (enableLOD)
        {
            Gizmos.color = isLODActive ? Color.red : Color.yellow;
            Gizmos.DrawWireSphere(transform.position, lodDistance);
        }
        
        // Draw movement direction
        if (isMoving && rb != null)
        {
            Gizmos.color = Color.green;
            Vector3 direction = rb.linearVelocity.normalized * 1f;
            Gizmos.DrawRay(transform.position, direction);
        }
    }
}
