using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using CharacterInteractionSystem;

/// <summary>
/// MotionCharacterController - Main integration script for Webcam Character System
/// Connects WebcamTouchDetector, CharacterInteraction, and TouchIndicator components
/// Provides automatic component discovery, coordinate transformation, character response logic,
/// interaction filtering, real-time mapping, and debug visualization
/// 
/// Attach this to a GameObject for complete motion-character integration
/// </summary>
public class MotionCharacterController : MonoBehaviour
{
    [Header("Core Components")]
    [SerializeField] private WebcamTouchDetector webcamTouchDetector;
    [SerializeField] private TouchCalibrationSystem calibrationSystem;
    [SerializeField] private TouchIndicator touchIndicator;
    [SerializeField] private Camera mainCamera;
    
    [Header("Character System Integration")]
    [SerializeField] private List<CharacterInteraction> characterInteractions = new List<CharacterInteraction>();
    [SerializeField] private bool autoFindCharacters = true;
    [SerializeField] private LayerMask characterLayerMask = -1; // All layers by default
    
    [Header("Touch Processing")]
    [SerializeField] private bool enableTouchProcessing = true;
    [SerializeField] private float touchRangeMultiplier = 1.0f;
    [SerializeField] private int maxSimultaneousInteractions = 5;
    [SerializeField] private float interactionRadius = 1.0f;
    
    [Header("Character Response Settings")]
    [SerializeField] private bool enableCharacterFeedback = true;
    [SerializeField] private float characterResponseDelay = 0.1f;
    [SerializeField] private bool enableCooldownSystem = true;
    [SerializeField] private float globalInteractionCooldown = 0.5f;
    
    [Header("Coordinate Transformation")]
    [SerializeField] private bool useCalibrationSystem = true;
    [SerializeField] private Vector2 customWorldArea = new Vector2(10f, 10f);
    [SerializeField] private Vector3 worldCenter = Vector3.zero;
    [SerializeField] private bool enableCoordinateDebug = false;
    
    [Header("Interaction Filtering")]
    [SerializeField] private bool enableConflictPrevention = true;
    [SerializeField] private float conflictDistance = 2.0f;
    [SerializeField] private int maxInteractionsPerFrame = 3;
    
    [Header("Debug Visualization")]
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] private bool showCoordinateMapping = true;
    [SerializeField] private bool showCharacterAreas = true;
    [SerializeField] private Color debugColor = Color.cyan;
    
    [Header("System Settings")]
    [SerializeField] private bool autoInitialize = true;
    [SerializeField] private bool showSystemStatus = true;
    [SerializeField] private KeyCode debugToggleKey = KeyCode.F1;
    
    [Header("Character Movement")]
    [SerializeField] public float moveSpeed = 5.0f;
    [SerializeField] public float acceleration = 8.0f;
    [SerializeField] public bool enableDiagonalMovement = true;
    
    // Component references and state
    private Dictionary<int, CharacterInteraction> activeTouchToCharacterMap = new Dictionary<int, CharacterInteraction>();
    private Queue<CharacterInteraction> interactionQueue = new Queue<CharacterInteraction>();
    private List<Vector2> processedTouchPositions = new List<Vector2>();
    private float lastGlobalInteractionTime = 0f;
    private bool systemInitialized = false;
    private bool debugMode = false;
    
    // Character management
    private readonly Dictionary<CharacterInteraction, TouchPoint> characterTouchHistory = new Dictionary<CharacterInteraction, TouchPoint>();
    private readonly Dictionary<CharacterInteraction, float> characterLastInteraction = new Dictionary<CharacterInteraction, float>();
    
    // Events
    public System.Action<List<Vector2>> OnMotionDetected;
    public System.Action<CharacterInteraction, Vector2> OnCharacterInteracted;
    public System.Action<CharacterInteraction> OnCharacterReleased;
    public System.Action<string> OnSystemStatus;
    
    // Properties
    public bool IsInitialized => systemInitialized && webcamTouchDetector != null;
    public bool IsProcessing => webcamTouchDetector?.IsProcessing ?? false;
    public List<CharacterInteraction> ActiveCharacters => new List<CharacterInteraction>(characterInteractions.Where(c => c != null));
    public int CurrentTouchCount => webcamTouchDetector?.TouchCount ?? 0;
    public List<CharacterInteraction> CurrentlyInteracting => new List<CharacterInteraction>(activeTouchToCharacterMap.Values);

    void Start()
    {
        if (autoInitialize)
        {
            InitializeMotionCharacterSystem();
        }
    }

    void Update()
    {
        if (!systemInitialized) return;
        
        HandleDebugInput();
        
        if (enableTouchProcessing)
        {
            ProcessMotionAndReact();
        }
        
        ProcessInteractionQueue();
        UpdateCharacterStates();
    }

    /// <summary>
    /// Main method to detect motion and react to character interactions
    /// Should be called automatically by the system but can be called manually
    /// </summary>
    public void DetectMotionAndReact()
    {
        if (!systemInitialized || !enableTouchProcessing)
        {
            LogDebug("Motion detection disabled or system not initialized");
            return;
        }

        if (webcamTouchDetector == null)
        {
            LogDebug("No webcam touch detector available");
            return;
        }

        // Get current touch positions from detector
        List<Vector2> currentTouches = webcamTouchDetector.CurrentTouchPositions;
        
        if (currentTouches.Count == 0)
        {
            HandleNoTouchesDetected();
            return;
        }

        // Process touches and map to characters
        ProcessTouchesForCharacterInteraction(currentTouches);
        
        // Trigger events
        OnMotionDetected?.Invoke(new List<Vector2>(currentTouches));
        
        LogDebug($"Detected {currentTouches.Count} touch points for character interaction");
    }

    void ProcessMotionAndReact()
    {
        DetectMotionAndReact();
    }

    void ProcessTouchesForCharacterInteraction(List<Vector2> touchPositions)
    {
        processedTouchPositions.Clear();
        int interactionsProcessed = 0;
        
        foreach (Vector2 touchPos in touchPositions)
        {
            if (interactionsProcessed >= maxInteractionsPerFrame)
                break;
                
            // Transform webcam coordinates to world coordinates
            Vector3 worldPosition = TransformTouchToWorldPosition(touchPos);
            
            if (enableCoordinateDebug)
            {
                LogDebug($"Touch {touchPos} -> World {worldPosition}");
            }
            
            // Find closest character within interaction range
            CharacterInteraction targetCharacter = FindClosestCharacter(worldPosition, interactionRadius * touchRangeMultiplier);
            
            if (targetCharacter != null)
            {
                // Check for conflicts
                if (IsInteractionAvailable(targetCharacter))
                {
                    QueueCharacterInteraction(targetCharacter, worldPosition);
                    interactionsProcessed++;
                }
                else
                {
                    LogDebug($"Interaction blocked for character: {targetCharacter.CharacterName}");
                }
            }
            
            // Create visual indicator
            if (enableCharacterFeedback && touchIndicator != null)
            {
                touchIndicator.ShowTouchIndicator(worldPosition);
            }
            
            processedTouchPositions.Add(touchPos);
        }
    }

    CharacterInteraction FindClosestCharacter(Vector3 worldPosition, float maxRange)
    {
        CharacterInteraction closest = null;
        float closestDistance = maxRange;
        
        foreach (CharacterInteraction character in characterInteractions)
        {
            if (character == null) continue;
            
            float distance = Vector3.Distance(worldPosition, character.transform.position);
            
            if (distance < closestDistance)
            {
                // Check for conflicts with other active interactions
                if (!enableConflictPrevention || !HasConflict(character, distance))
                {
                    closest = character;
                    closestDistance = distance;
                }
            }
        }
        
        return closest;
    }

    bool HasConflict(CharacterInteraction character, float newInteractionDistance)
    {
        if (!enableConflictPrevention) return false;
        
        // Check if another character is too close
        foreach (var activeInteraction in activeTouchToCharacterMap.Values)
        {
            if (activeInteraction == null || activeInteraction == character) continue;
            
            float distance = Vector3.Distance(character.transform.position, activeInteraction.transform.position);
            if (distance < conflictDistance)
            {
                LogDebug($"Interaction conflict detected: {character.CharacterName} vs {activeInteraction.CharacterName} (distance: {distance})");
                return true;
            }
        }
        
        return false;
    }

    bool IsInteractionAvailable(CharacterInteraction character)
    {
        // Check global cooldown
        if (Time.time - lastGlobalInteractionTime < globalInteractionCooldown)
            return false;
            
        // Check character cooldown
        if (characterLastInteraction.ContainsKey(character))
        {
            if (Time.time - characterLastInteraction[character] < (character.IsOnCooldown ? 1f : 0.1f))
                return false;
        }
        
        // Check if already interacting
        return !activeTouchToCharacterMap.ContainsValue(character);
    }

    void QueueCharacterInteraction(CharacterInteraction character, Vector3 worldPosition)
    {
        // Create interaction data
        TouchPoint touchPoint = new TouchPoint
        {
            position = worldPosition,
            timestamp = Time.time,
            id = GetNextTouchId()
        };
        
        // Store in history
        characterTouchHistory[character] = touchPoint;
        
        // Queue for processing
        interactionQueue.Enqueue(character);
        
        // Track active interaction
        activeTouchToCharacterMap[touchPoint.id] = character;
        
        LogDebug($"Queued interaction with character: {character.CharacterName}");
    }

    void ProcessInteractionQueue()
    {
        if (interactionQueue.Count == 0) return;
        
        // Process interactions with delay for smoother feel
        if (Time.time - lastGlobalInteractionTime >= characterResponseDelay)
        {
            CharacterInteraction character = interactionQueue.Dequeue();
            
            if (character != null)
            {
                TriggerCharacterInteraction(character);
            }
        }
    }

    void TriggerCharacterInteraction(CharacterInteraction character)
    {
        if (character == null) return;
        
        try
        {
            // Handle character touch
            character.HandleCharacterTouched();
            
            // Trigger interaction after a brief delay
            StartCoroutine(DelayedCharacterInteraction(character));
            
            // Update timing
            lastGlobalInteractionTime = Time.time;
            characterLastInteraction[character] = Time.time;
            
            // Notify listeners
            Vector3 characterPosition = character.transform.position;
            OnCharacterInteracted?.Invoke(character, characterPosition);
            
            LogDebug($"Triggered interaction with: {character.CharacterName}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error triggering character interaction: {e.Message}");
        }
    }

    IEnumerator DelayedCharacterInteraction(CharacterInteraction character)
    {
        yield return new WaitForSeconds(0.1f);
        
        if (character != null)
        {
            character.TriggerInteraction();
        }
    }

    void UpdateCharacterStates()
    {
        // Clean up old touch history
        var keysToRemove = new List<CharacterInteraction>();
        
        foreach (var kvp in characterTouchHistory)
        {
            if (kvp.Value == null || Time.time - kvp.Value.timestamp > 5f)
            {
                keysToRemove.Add(kvp.Key);
            }
        }
        
        foreach (var key in keysToRemove)
        {
            characterTouchHistory.Remove(key);
        }
        
        // Clean up active touch mapping for expired interactions
        var touchIdsToRemove = new List<int>();
        foreach (var kvp in activeTouchToCharacterMap)
        {
            if (kvp.Value == null)
            {
                touchIdsToRemove.Add(kvp.Key);
            }
        }
        
        foreach (int touchId in touchIdsToRemove)
        {
            activeTouchToCharacterMap.Remove(touchId);
        }
    }

    void HandleNoTouchesDetected()
    {
        // Handle character release for all currently interacting characters
        foreach (var character in activeTouchToCharacterMap.Values)
        {
            if (character != null)
            {
                character.HandleCharacterReleased();
                OnCharacterReleased?.Invoke(character);
                LogDebug($"Released character: {character.CharacterName}");
            }
        }
        
        activeTouchToCharacterMap.Clear();
    }

    /// <summary>
    /// Transform touch position from webcam coordinates to Unity world coordinates
    /// </summary>
    public Vector3 TransformTouchToWorldPosition(Vector2 webcamPosition)
    {
        if (useCalibrationSystem && calibrationSystem != null && calibrationSystem.IsCalibrated)
        {
            // Use calibrated coordinate transformation
            Vector2 screenPosition = calibrationSystem.GetScreenPositionFromWebcam(webcamPosition);
            return ScreenToWorldPosition(screenPosition);
        }
        else
        {
            // Use simple coordinate mapping
            return SimpleCoordinateTransform(webcamPosition);
        }
    }

    /// <summary>
    /// Transform world position to webcam coordinates
    /// </summary>
    public Vector2 TransformWorldToWebcamPosition(Vector3 worldPosition)
    {
        if (useCalibrationSystem && calibrationSystem != null && calibrationSystem.IsCalibrated)
        {
            // Use calibrated coordinate transformation
            Vector3 screenPosition = WorldToScreenPosition(worldPosition);
            return calibrationSystem.GetWebcamPositionFromScreen(screenPosition);
        }
        else
        {
            // Use simple coordinate mapping
            return SimpleInverseCoordinateTransform(worldPosition);
        }
    }

    Vector3 SimpleCoordinateTransform(Vector2 normalizedPosition)
    {
        // Simple mapping from 0-1 normalized coordinates to world space
        float x = (normalizedPosition.x - 0.5f) * customWorldArea.x + worldCenter.x;
        float y = (normalizedPosition.y - 0.5f) * customWorldArea.y + worldCenter.y;
        float z = worldCenter.z;
        
        return new Vector3(x, y, z);
    }

    Vector2 SimpleInverseCoordinateTransform(Vector3 worldPosition)
    {
        // Inverse of simple coordinate transform
        float x = (worldPosition.x - worldCenter.x) / customWorldArea.x + 0.5f;
        float y = (worldPosition.y - worldCenter.y) / customWorldArea.y + 0.5f;
        
        return new Vector2(x, y);
    }

    Vector3 ScreenToWorldPosition(Vector2 screenPosition)
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
            
        if (mainCamera == null)
            return Vector3.zero;
            
        return mainCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, 10f));
    }

    Vector3 WorldToScreenPosition(Vector3 worldPosition)
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
            
        if (mainCamera == null)
            return Vector3.zero;
            
        return mainCamera.WorldToScreenPoint(worldPosition);
    }

    /// <summary>
    /// Initialize the complete motion character system
    /// </summary>
    public void InitializeMotionCharacterSystem()
    {
        Debug.Log("Initializing Motion Character Controller...");
        
        // Auto-find components
        AutoFindComponents();
        
        // Validate required components
        if (!ValidateRequiredComponents())
        {
            Debug.LogError("MotionCharacterController: Failed to initialize - missing required components");
            return;
        }
        
        // Set up component connections
        SetupComponentConnections();
        
        // Find and register character interactions
        if (autoFindCharacters)
        {
            FindAndRegisterCharacters();
        }
        
        // Set up event listeners
        SetupEventListeners();
        
        // Configure components
        ConfigureComponents();
        
        systemInitialized = true;
        
        OnSystemStatus?.Invoke("Motion Character System initialized successfully");
        Debug.Log("✅ Motion Character Controller initialized successfully");
        
        LogDebug("System ready for motion detection and character interaction");
    }

    void AutoFindComponents()
    {
        // Find webcam touch detector
        if (webcamTouchDetector == null)
        {
            webcamTouchDetector = FindFirstObjectByType<WebcamTouchDetector>();
            if (webcamTouchDetector != null)
                Debug.Log("Found WebcamTouchDetector");
        }
        
        // Find calibration system
        if (calibrationSystem == null)
        {
            calibrationSystem = FindFirstObjectByType<TouchCalibrationSystem>();
            if (calibrationSystem != null)
                Debug.Log("Found TouchCalibrationSystem");
        }
        
        // Find touch indicator
        if (touchIndicator == null)
        {
            touchIndicator = FindFirstObjectByType<TouchIndicator>();
            if (touchIndicator != null)
                Debug.Log("Found TouchIndicator");
        }
        
        // Find main camera
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera != null)
                Debug.Log("Found Main Camera");
        }
    }

    bool ValidateRequiredComponents()
    {
        bool allValid = true;
        
        if (webcamTouchDetector == null)
        {
            Debug.LogError("WebcamTouchDetector not found!");
            allValid = false;
        }
        
        return allValid;
    }

    void SetupComponentConnections()
    {
        if (calibrationSystem != null && webcamTouchDetector != null)
        {
            calibrationSystem.webcamTouchDetector = webcamTouchDetector;
        }
        
        if (touchIndicator != null && mainCamera != null)
        {
            touchIndicator.SetTargetCamera(mainCamera);
        }
    }

    void FindAndRegisterCharacters()
    {
        // Clear existing list
        characterInteractions.Clear();
        
        // Find all character interaction components
        CharacterInteraction[] foundCharacters = FindObjectsByType<CharacterInteraction>(FindObjectsSortMode.None);
        
        foreach (CharacterInteraction character in foundCharacters)
        {
            // Filter by layer mask if specified
            if (((1 << character.gameObject.layer) & characterLayerMask) != 0)
            {
                characterInteractions.Add(character);
                Debug.Log($"Registered character: {character.CharacterName}");
            }
        }
        
        // Also check for characters in children of this object
        CharacterInteraction[] childCharacters = GetComponentsInChildren<CharacterInteraction>(true);
        foreach (CharacterInteraction character in childCharacters)
        {
            if (!characterInteractions.Contains(character))
            {
                characterInteractions.Add(character);
                Debug.Log($"Registered child character: {character.CharacterName}");
            }
        }
        
        Debug.Log($"Found and registered {characterInteractions.Count} character interactions");
    }

    void SetupEventListeners()
    {
        if (webcamTouchDetector != null)
        {
            webcamTouchDetector.OnTouchesDetected += OnWebcamTouchesDetected;
            webcamTouchDetector.OnSingleTouchDetected += OnSingleWebcamTouchDetected;
            webcamTouchDetector.OnNoTouchesDetected += OnNoWebcamTouchesDetected;
        }
        
        if (calibrationSystem != null)
        {
            calibrationSystem.OnCalibrationCompleted += OnCalibrationCompleted;
            calibrationSystem.OnCalibrationStatus += OnCalibrationStatusChanged;
        }
        
        // Setup character interaction events
        foreach (CharacterInteraction character in characterInteractions)
        {
            if (character != null)
            {
                character.OnInteractionTriggered += OnCharacterInteractionTriggered;
            }
        }
    }

    void ConfigureComponents()
    {
        if (webcamTouchDetector != null)
        {
            webcamTouchDetector.SetTouchDetectionEnabled(true);
            // Note: SetTouchDetectionEnabled already sets EnableTouchDetection property
            
            LogDebug("WebcamTouchDetector configured");
        }
        
        if (calibrationSystem != null)
        {
            useCalibrationSystem = calibrationSystem.IsCalibrated;
            LogDebug("Calibration system configured");
        }
        
        LogDebug("Component configuration completed");
    }

    // Event handlers
    void OnWebcamTouchesDetected(List<WebcamTouchDetector.TouchData> touches)
    {
        LogDebug($"Webcam detected {touches.Count} touches");
    }

    void OnSingleWebcamTouchDetected(Vector2 touchPosition)
    {
        LogDebug($"Single touch detected at {touchPosition}");
    }

    void OnNoWebcamTouchesDetected()
    {
        HandleNoTouchesDetected();
    }

    void OnCalibrationCompleted(TouchCalibrationData data)
    {
        useCalibrationSystem = true;
        OnSystemStatus?.Invoke("Calibration completed - using calibrated coordinate mapping");
        LogDebug("Calibration completed, enabled calibrated mapping");
    }

    void OnCalibrationStatusChanged(string status)
    {
        LogDebug($"Calibration status: {status}");
    }

    void OnCharacterInteractionTriggered(CharacterInteraction character)
    {
        LogDebug($"Character interaction triggered: {character.CharacterName}");
    }

    void HandleDebugInput()
    {
        if (Input.GetKeyDown(debugToggleKey))
        {
            debugMode = !debugMode;
            showDebugInfo = debugMode;
            LogDebug($"Debug mode {(debugMode ? "enabled" : "disabled")}");
        }
    }

    void LogDebug(string message)
    {
        if (showDebugInfo)
        {
            Debug.Log($"[MotionCharacterController] {message}");
        }
    }

    // Public API methods
    
    public void AddCharacterInteraction(CharacterInteraction character)
    {
        if (character != null && !characterInteractions.Contains(character))
        {
            characterInteractions.Add(character);
            character.OnInteractionTriggered += OnCharacterInteractionTriggered;
            LogDebug($"Added character interaction: {character.CharacterName}");
        }
    }

    public void RemoveCharacterInteraction(CharacterInteraction character)
    {
        if (characterInteractions.Contains(character))
        {
            characterInteractions.Remove(character);
            LogDebug($"Removed character interaction: {character.CharacterName}");
        }
    }

    public void SetTouchProcessingEnabled(bool enabled)
    {
        enableTouchProcessing = enabled;
        LogDebug($"Touch processing {(enabled ? "enabled" : "disabled")}");
    }

    public void SetCharacterResponseDelay(float delay)
    {
        characterResponseDelay = Mathf.Max(0f, delay);
        LogDebug($"Character response delay set to {characterResponseDelay}s");
    }

    public void SetInteractionRadius(float radius)
    {
        interactionRadius = Mathf.Max(0.1f, radius);
        LogDebug($"Interaction radius set to {interactionRadius}");
    }

    public void SetConflictPreventionEnabled(bool enabled)
    {
        enableConflictPrevention = enabled;
        LogDebug($"Conflict prevention {(enabled ? "enabled" : "disabled")}");
    }

    public void SetGlobalCooldown(float cooldown)
    {
        globalInteractionCooldown = Mathf.Max(0f, cooldown);
        LogDebug($"Global interaction cooldown set to {globalInteractionCooldown}s");
    }

    public void ClearAllInteractions()
    {
        interactionQueue.Clear();
        activeTouchToCharacterMap.Clear();
        characterTouchHistory.Clear();
        processedTouchPositions.Clear();
        
        if (webcamTouchDetector != null)
        {
            webcamTouchDetector.ClearAllTouches();
        }
        
        LogDebug("All interactions cleared");
    }

    public string GetSystemStatus()
    {
        System.Text.StringBuilder status = new System.Text.StringBuilder();
        status.AppendLine("=== Motion Character Controller Status ===");
        status.AppendLine($"Initialized: {systemInitialized}");
        status.AppendLine($"Touch Processing: {enableTouchProcessing}");
        status.AppendLine($"Current Touches: {CurrentTouchCount}");
        status.AppendLine($"Active Characters: {characterInteractions.Count}");
        status.AppendLine($"Currently Interacting: {activeTouchToCharacterMap.Count}");
        status.AppendLine($"Queued Interactions: {interactionQueue.Count}");
        status.AppendLine($"Calibrated Mapping: {useCalibrationSystem && calibrationSystem?.IsCalibrated == true}");
        
        if (webcamTouchDetector != null)
        {
            status.AppendLine($"Webcam Processing: {webcamTouchDetector.IsProcessing}");
        }
        
        status.AppendLine($"Debug Mode: {debugMode}");
        
        return status.ToString();
    }

    public List<CharacterInteraction> GetCharactersInRange(Vector3 worldPosition, float range)
    {
        return characterInteractions
            .Where(c => c != null && Vector3.Distance(worldPosition, c.transform.position) <= range)
            .ToList();
    }

    public CharacterInteraction GetClosestCharacter(Vector3 worldPosition)
    {
        return FindClosestCharacter(worldPosition, float.MaxValue);
    }

    public void ForceInteractionWithCharacter(CharacterInteraction character)
    {
        if (character != null)
        {
            Vector3 characterPosition = character.transform.position;
            QueueCharacterInteraction(character, characterPosition);
            LogDebug($"Forced interaction with: {character.CharacterName}");
        }
    }

    // Context menu methods for easy testing
    [ContextMenu("Test Motion Detection")]
    public void TestMotionDetection()
    {
        if (webcamTouchDetector != null)
        {
            webcamTouchDetector.SimulateTouch(new Vector2(0.5f, 0.5f), true);
        }
    }

    [ContextMenu("Test All Characters")]
    public void TestAllCharacters()
    {
        foreach (CharacterInteraction character in characterInteractions)
        {
            if (character != null)
            {
                ForceInteractionWithCharacter(character);
            }
        }
    }

    [ContextMenu("Clear System")]
    public void ClearSystem()
    {
        ClearAllInteractions();
        
        foreach (var character in characterInteractions)
        {
            if (character != null)
            {
                character.ResetCooldown();
            }
        }
    }

    [ContextMenu("Print System Status")]
    public void PrintSystemStatus()
    {
        string status = GetSystemStatus();
        Debug.Log(status);
        OnSystemStatus?.Invoke(status);
    }

    // Utility methods
    private int GetNextTouchId()
    {
        return System.DateTime.Now.Ticks.GetHashCode() ^ UnityEngine.Random.Range(0, 10000);
    }

    void OnDrawGizmosSelected()
    {
        if (!showDebugInfo) return;
        
        Gizmos.color = debugColor;
        
        // Draw world mapping area
        Vector3 center = worldCenter;
        Vector3 size = new Vector3(customWorldArea.x, customWorldArea.y, 0.1f);
        Gizmos.DrawWireCube(center, size);
        
        // Draw character areas if enabled
        if (showCharacterAreas)
        {
            Gizmos.color = Color.yellow;
            foreach (CharacterInteraction character in characterInteractions)
            {
                if (character != null)
                {
                    Gizmos.DrawWireSphere(character.transform.position, interactionRadius);
                }
            }
        }
        
        // Draw coordinate debug if enabled
        if (enableCoordinateDebug && processedTouchPositions.Count > 0)
        {
            Gizmos.color = Color.green;
            foreach (Vector2 touchPos in processedTouchPositions)
            {
                Vector3 worldPos = TransformTouchToWorldPosition(touchPos);
                Gizmos.DrawWireCube(worldPos, Vector3.one * 0.2f);
            }
        }
    }

    void OnGUI()
    {
        if (!showSystemStatus || !debugMode) return;
        
        // Display system status
        GUILayout.BeginArea(new Rect(10, 10, 350, 200));
        GUILayout.Label("🎮 Motion Character Controller", EditorGUIUtility.boldLabel);
        GUILayout.Label($"Status: {(systemInitialized ? "✅ Ready" : "❌ Not Ready")}");
        GUILayout.Label($"Touches: {CurrentTouchCount}");
        GUILayout.Label($"Characters: {characterInteractions.Count}");
        GUILayout.Label($"Interactions: {activeTouchToCharacterMap.Count}");
        GUILayout.Label($"Queue: {interactionQueue.Count}");
        
        GUILayout.Space(5);
        GUILayout.Label($"F1 - Toggle Debug: {(debugMode ? "ON" : "OFF")}", EditorGUIUtility.miniBoldLabel);
        GUILayout.EndArea();
        
        // Display character information if interacting
        if (activeTouchToCharacterMap.Count > 0)
        {
            GUILayout.BeginArea(new Rect(10, 220, 350, 100));
            GUILayout.Label("🎭 Active Interactions:", EditorGUIUtility.boldLabel);
            
            foreach (var interaction in activeTouchToCharacterMap.Values)
            {
                if (interaction != null)
                {
                    GUILayout.Label($"• {interaction.CharacterName}");
                }
            }
            
            GUILayout.EndArea();
        }
    }

    void OnDestroy()
    {
        // Clean up event listeners
        if (webcamTouchDetector != null)
        {
            webcamTouchDetector.OnTouchesDetected -= OnWebcamTouchesDetected;
            webcamTouchDetector.OnSingleTouchDetected -= OnSingleWebcamTouchDetected;
            webcamTouchDetector.OnNoTouchesDetected -= OnNoWebcamTouchesDetected;
        }
        
        if (calibrationSystem != null)
        {
            calibrationSystem.OnCalibrationCompleted -= OnCalibrationCompleted;
            calibrationSystem.OnCalibrationStatus -= OnCalibrationStatusChanged;
        }
        
        // Clean up character event listeners
        foreach (CharacterInteraction character in characterInteractions)
        {
            if (character != null)
            {
                character.OnInteractionTriggered -= OnCharacterInteractionTriggered;
            }
        }
        
        LogDebug("MotionCharacterController destroyed, events cleaned up");
    }

    // Inner classes
    
    [System.Serializable]
    public class TouchPoint
    {
        public Vector3 position;
        public float timestamp;
        public float age;
        public int id;
        
        public TouchPoint()
        {
            timestamp = Time.time;
            age = 0f;
            id = 0;
        }
    }
}

// Editor GUI styles (for OnGUI display)
#if UNITY_EDITOR
public static class EditorGUIUtility
{
    public static GUIStyle boldLabel
    {
        get
        {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontStyle = FontStyle.Bold;
            return style;
        }
    }
    
    public static GUIStyle miniBoldLabel
    {
        get
        {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontStyle = FontStyle.Bold;
            style.fontSize = 10;
            return style;
        }
    }
}
#endif
