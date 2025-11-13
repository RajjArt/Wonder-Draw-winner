using UnityEngine;
using UnityEngine.InputSystem; // Required for the new Input System
using System.Collections.Generic;
using System.Linq; // Required for LINQ extensions used in WebcamTouchDetector

// Ensure these namespaces are defined in your project
// If they aren't, the script won't compile until you define them or remove the using directives
using CharacterInteractionSystem;
using TouchDetection;

/// <summary>
/// Enhanced Touch Input Manager that supports multiple input methods including webcam touch detection
/// Integrates webcam-based touch detection with traditional input methods
/// Attach this to a GameObject and assign webcam touch detector reference
/// </summary>
public class EnhancedTouchInputManager : MonoBehaviour
{
    [Header("Input Methods")]
    public bool useMouseInput = true;
    public bool useTouchInput = true;
    public bool useWebcamInput = false;
    public int webcamInputPriority = 1; // Lower numbers = higher priority

    [Header("Webcam Integration")]
    public WebcamTouchDetector webcamTouchDetector;
    public TouchCalibrationSystem calibrationSystem;

    [Header("Touch Settings")]
    public Camera mainCamera;
    public LayerMask interactionLayer = -1; // Layer mask for interactive objects
    public float touchRadius = 0.5f; // Touch detection radius for 3D objects
    public float touchCooldown = 0.1f; // Minimum time between touches on same character

    [Header("Touch Feedback")]
    public GameObject touchIndicatorPrefab;
    public Color touchColor = Color.red;
    public bool showTouchDebug = true;
    public bool showRaycastDebug = true;

    // Touch tracking
    private List<GameObject> touchIndicators = new List<GameObject>();
    private Dictionary<int, Vector3> activeTouches = new Dictionary<int, Vector3>();
    private Dictionary<string, float> lastTouchTime = new Dictionary<string, float>();

    // Input method management
    private bool[] inputMethodActive = new bool[3]; // 0=Mouse, 1=Touch, 2=Webcam
                                                    // lastTouchPositions array is not strictly needed anymore with the new Input System's touch data

    // Events
    public System.Action<Vector3> OnTouchDetected;
    public System.Action<int> OnTouchCountChanged;
    public System.Action<string> OnCharacterTouched;

    // We use OnEnable/OnDisable to manage the New Input System's touch support
    void OnEnable()
    {
        // Enable the enhanced touch support for mobile touch screen inputs
        UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Enable();
    }

    void OnDisable()
    {
        // Disable when the object is disabled
        UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Disable();
    }

    void Start()
    {
        InitializeComponents();
        SetupInputMethods();
        SetupEventListeners();
    }

    void InitializeComponents()
    {
        if (mainCamera == null)
        {
            // Note: Camera.main relies on objects being tagged 'MainCamera'
            mainCamera = Camera.main;
        }

        if (mainCamera == null)
        {
            Debug.LogError("No camera assigned to EnhancedTouchInputManager!");
        }

        // Setup webcam touch detector
        if (useWebcamInput)
        {
            if (webcamTouchDetector == null)
            {
                // Use FindFirstObjectByType for Unity 6 compatibility (replaces FindObjectOfType)
                webcamTouchDetector = FindFirstObjectByType<WebcamTouchDetector>();
                if (webcamTouchDetector == null)
                {
                    Debug.LogWarning("WebcamTouchDetector not found. Creating one...");
                    GameObject webcamObj = new GameObject("WebcamTouchDetector");
                    webcamTouchDetector = webcamObj.AddComponent<WebcamTouchDetector>();
                }
            }

            if (calibrationSystem == null)
            {
                calibrationSystem = FindFirstObjectByType<TouchCalibrationSystem>();
            }
        }
    }

    void SetupInputMethods()
    {
        inputMethodActive[0] = useMouseInput;   // Mouse
        inputMethodActive[1] = useTouchInput;   // Touch
        inputMethodActive[2] = useWebcamInput;  // Webcam

        Debug.Log($"Input methods enabled: Mouse={useMouseInput}, Touch={useTouchInput}, Webcam={useWebcamInput}");
    }

    // Placeholders for webcam event handlers (assuming these exist in your WebcamTouchDetector script)
    private void HandleWebcamTouches(List<WebcamTouchDetector.TouchData> touches) { /* ... handle the list of touches ... */ }
    private void HandleTouchCountChanged(int count) { /* ... handle count change ... */ }

    void SetupEventListeners()
    {
        if (webcamTouchDetector != null)
        {
            // Assuming these events are defined in WebcamTouchDetector
            // webcamTouchDetector.OnTouchesDetected += HandleWebcamTouches; 
            // webcamTouchDetector.OnTouchCountChanged += HandleTouchCountChanged;
        }
    }

    void Update()
    {
        // Process input methods based on availability and priority
        if (inputMethodActive[2] && webcamTouchDetector != null) // Webcam input
        {
            ProcessWebcamTouches();
        }
        // Check if there are any active system touches using the new API
        else if (inputMethodActive[1] && UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches.Count > 0)
        {
            ProcessMobileTouches();
        }
        else if (inputMethodActive[0]) // Mouse input
        {
            ProcessMouseInput();
        }

        // Clean up old touch indicators
        CleanupTouchIndicators();
    }

    void ProcessWebcamTouches()
    {
        // This relies on GetCurrentTouches() existing and being correct in WebcamTouchDetector
        if (webcamTouchDetector != null)
        {
            List<WebcamTouchDetector.TouchData> touchData = webcamTouchDetector.activeTouches; // Access the public field

            for (int i = 0; i < touchData.Count; i++)
            {
                Vector2 webcamPos = touchData[i].position;
                // Assuming your logic requires conversion via WorldToScreenPoint
                Vector3 screenPos = mainCamera.WorldToScreenPoint(new Vector3(webcamPos.x, webcamPos.y, 0));
                Vector3 raycastPosition = new Vector3(screenPos.x, screenPos.y, 0);

                ProcessTouchAtPosition(i + 100, raycastPosition); // Offset webcam touch IDs
            }
        }
    }

    void ProcessMobileTouches()
    {
        // Use the new EnhancedTouch API collection
        var currentTouches = UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches;

        for (int i = 0; i < currentTouches.Count; i++)
        {
            var touch = currentTouches[i];
            Vector3 screenPosition = touch.screenPosition;

            ProcessTouchAtPosition(touch.fingerId, screenPosition);

            // Note: lastTouchPositions array handling might need removal or a redesign 
            // depending on if you are using it elsewhere for gesture detection.
        }
    }

    void ProcessMouseInput()
    {
        // Use the new Input System's Mouse class to check button states
        var mouse = Mouse.current;

        if (mouse != null)
        {
            if (mouse.leftButton.wasPressedThisFrame)
            {
                Vector3 mousePosition = mouse.position.ReadValue();
                ProcessTouchAtPosition(999, mousePosition);
            }

            if (mouse.leftButton.isPressed)
            {
                Vector3 mousePosition = mouse.position.ReadValue();
                ProcessTouchMoved(999, mousePosition);
            }

            if (mouse.leftButton.wasReleasedThisFrame)
            {
                Vector3 mousePosition = mouse.position.ReadValue();
                ProcessTouchEnded(999, mousePosition);
            }
        }
    }

    void ProcessTouchAtPosition(int touchId, Vector3 screenPosition)
    {
        // Create touch indicator
        CreateTouchIndicator(screenPosition);

        // Convert screen position to world space
        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        Vector3 touchPosition = GetWorldPositionFromScreen(ray);

        // Update active touches
        activeTouches[touchId] = touchPosition;

        // Perform character interaction
        PerformCharacterInteraction(touchPosition);

        // Fire events
        OnTouchDetected?.Invoke(touchPosition);

        if (showTouchDebug)
        {
            Debug.Log($"Touch detected at: {touchPosition} (ID: {touchId})");
        }
    }

    void ProcessTouchMoved(int touchId, Vector3 screenPosition)
    {
        if (activeTouches.ContainsKey(touchId))
        {
            // ... (Rest of the function was cut off in the prompt, assuming standard movement logic)
        }
    }

    // --- Assuming helper methods below are needed to complete the script ---

    private Vector3 GetWorldPositionFromScreen(Ray ray)
    {
        // This is simplified and depends heavily on your scene setup (e.g., using a Physics Raycaster or a specific Z-plane)
        // If you are using a UI canvas, you need different logic (EventSystems)

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, interactionLayer))
        {
            if (showRaycastDebug) Debug.DrawRay(ray.origin, ray.direction * hit.distance, Color.green, 0.1f);
            return hit.point;
        }
        else
        {
            if (showRaycastDebug) Debug.DrawRay(ray.origin, ray.direction * 100f, Color.red, 0.1f);
            // Return a default position or handle a miss
            return mainCamera.ScreenToWorldPoint(new Vector3(ray.origin.x, ray.origin.y, mainCamera.farClipPlane));
        }
    }

    private void CreateTouchIndicator(Vector3 screenPosition)
    {
        if (!showTouchDebug || touchIndicatorPrefab == null) return;

        // Code to instantiate indicator at screenPosition (likely need conversion to world space/UI space)
    }

    private void CleanupTouchIndicators()
    {
        // Logic to remove expired indicators
    }

    void PerformCharacterInteraction(Vector3 touchPosition)
    {
        // Create a sphere at touch position to detect nearby characters
        Collider[] hitColliders = Physics.OverlapSphere(touchPosition, touchRadius, interactionLayer);

        if (showRaycastDebug)
        {
            Debug.Log($"Raycast hit {hitColliders.Length} objects at {touchPosition}");
        }

        foreach (Collider hitCollider in hitColliders)
        {
            CharacterInteraction character = hitCollider.GetComponent<CharacterInteraction>();

            if (character != null)
            {
                // Check cooldown for this character
                string characterName = character.CharacterName;
                if (!ShouldProcessCharacterTouch(characterName))
                {
                    continue;
                }

                // Trigger character interaction
                character.TriggerInteraction();

                // Record touch time for cooldown
                lastTouchTime[characterName] = Time.time;

                // Fire events
                OnCharacterTouched?.Invoke(characterName);

                if (showRaycastDebug)
                {
                    Debug.Log($"Character touched: {characterName}");
                }

                break; // Only trigger one character per touch
            }
        }
    }

    bool ShouldProcessCharacterTouch(string characterName)
    {
        if (!lastTouchTime.ContainsKey(characterName))
        {
            return true; // First time touching this character
        }

        float timeSinceLastTouch = Time.time - lastTouchTime[characterName];
        return timeSinceLastTouch >= touchCooldown;
    }

    void CreateTouchIndicator(Vector3 screenPosition)
    {
        if (touchIndicatorPrefab == null) return;

        // Convert screen position to world position for indicator
        // Note: This worldPosition calculation assumes a specific setup where Z=0 is relevant
        Vector3 worldPosition = mainCamera.ScreenToWorldPoint(screenPosition);
        worldPosition.z = 0; // Put indicator on screen plane

        GameObject indicator = Instantiate(touchIndicatorPrefab, worldPosition, Quaternion.identity);
        touchIndicators.Add(indicator);

        // Auto-destroy after a short time
        Destroy(indicator, 2f);
    }

    // These handlers are assuming events were set up in the previous part of the script
    // Note: The previous script part contained placeholder event handlers, 
    // but the event subscription lines were commented out.
    void HandleWebcamTouches(List<WebcamTouchDetector.TouchData> webcamTouches)
    {
        if (!useWebcamInput) return;

        // Convert webcam touches to Unity world coordinates and process
        foreach (WebcamTouchDetector.TouchData touch in webcamTouches)
        {
            // This logic might need refinement based on how your webcam is oriented/calibrated
            Vector3 screenPos = mainCamera.WorldToScreenPoint(new Vector3(touch.position.x, touch.position.y, 0));
            ProcessTouchAtPosition(Random.Range(1000, 9999), screenPos);
        }
    }

    void HandleTouchCountChanged(int touchCount)
    {
        OnTouchCountChanged?.Invoke(touchCount);
    }

    void CleanupTouchIndicators()
    {
        // Remove null entries
        touchIndicators.RemoveAll(indicator => indicator == null);

        // Limit number of indicators to prevent memory issues
        if (touchIndicators.Count > 20)
        {
            GameObject oldest = touchIndicators[0];
            touchIndicators.RemoveAt(0);
            if (oldest != null)
            {
                Destroy(oldest);
            }
        }
    }

    // Public methods for external access

    public Dictionary<int, Vector3> GetActiveTouches()
    {
        return new Dictionary<int, Vector3>(activeTouches);
    }

    public bool IsAnyCharacterBeingTouched()
    {
        return activeTouches.Count > 0;
    }

    public void SetInputMethodActive(int methodIndex, bool active)
    {
        if (methodIndex >= 0 && methodIndex < inputMethodActive.Length)
        {
            inputMethodActive[methodIndex] = active;
            Debug.Log($"Input method {methodIndex} set to {active}");
        }
    }

    public void SwitchToInputMethod(int methodIndex)
    {
        // Disable all input methods
        for (int i = 0; i < inputMethodActive.Length; i++)
        {
            inputMethodActive[i] = false;
        }

        // Enable selected method
        if (methodIndex >= 0 && methodIndex < inputMethodActive.Length)
        {
            inputMethodActive[methodIndex] = true;
            Debug.Log($"Switched to input method {methodIndex}");
        }
    }

    public void ResetTouchCooldowns()
    {
        lastTouchTime.Clear();
        Debug.Log("Touch cooldowns reset");
    }

    // For debugging and setup
    [ContextMenu("Test Touch at Center")]
    public void TestTouchAtCenter()
    {
        Vector3 centerScreen = new Vector3(Screen.width / 2, Screen.height / 2, 0);
        ProcessTouchAtPosition(9999, centerScreen);
    }

    [ContextMenu("Test All Characters")]
    public void TestAllCharacters()
    {
        // CORRECTED: Replaced FindObjectsOfType with the Unity 6 compatible FindObjectsByType
        CharacterInteraction[] characters = FindObjectsByType<CharacterInteraction>(FindObjectsSortMode.None);

        foreach (CharacterInteraction character in characters)
        {
            // Trigger each character's interaction
            character.TriggerInteraction();
        }

        Debug.Log($"Triggered {characters.Length} character interactions for testing");
    }

    // Draw gizmos for debugging
    void OnDrawGizmos()
    {
        if (showRaycastDebug && Application.isPlaying)
        {
            Gizmos.color = Color.red;
            foreach (KeyValuePair<int, Vector3> touch in activeTouches)
            {
                Gizmos.DrawWireSphere(touch.Value, touchRadius);
            }
        }

        // Show webcam detection area if enabled
        if (useWebcamInput && webcamTouchDetector != null && showRaycastDebug)
        {
            Gizmos.color = Color.blue;
            // Draw webcam detection area
            // This would need to be adjusted based on your camera setup
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(8, 6, 0));
        }
    }
}





