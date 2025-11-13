using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using CharacterInteractionSystem;

namespace TouchDetection
{
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
        private Vector2[] lastTouchPositions = new Vector2[10]; // Track touch positions
        
        // Events
        public System.Action<Vector3> OnTouchDetected;
        public System.Action<int> OnTouchCountChanged;
        public System.Action<string> OnCharacterTouched;
        
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
        
        void SetupEventListeners()
        {
            if (webcamTouchDetector != null)
            {
                webcamTouchDetector.OnTouchesDetected += HandleWebcamTouches;
                webcamTouchDetector.OnTouchCountChanged += HandleTouchCountChanged;
            }
        }
        
        void Update()
        {
            // Process input methods based on availability
            if (inputMethodActive[2] && webcamTouchDetector != null) // Webcam input (highest priority)
            {
                ProcessWebcamTouches();
            }
            else if (inputMethodActive[1] && Input.touchCount > 0) // Mobile touch input
            {
                ProcessMobileTouches();
            }
            else if (inputMethodActive[0]) // Mouse input (fallback)
            {
                ProcessMouseInput();
            }
            
            // Clean up old touch indicators
            CleanupTouchIndicators();
        }
        
        void ProcessWebcamTouches()
        {
            // Webcam touches are handled by events, but we can process them here too
            if (webcamTouchDetector != null)
            {
                List<WebcamTouchDetector.TouchData> touchData = webcamTouchDetector.GetCurrentTouches();
                
                for (int i = 0; i < touchData.Count; i++)
                {
                    Vector2 webcamPos = touchData[i].position;
                    Vector3 unityWorldPos = new Vector3(webcamPos.x, webcamPos.y, 0);
                    
                    // Convert to screen space for raycasting
                    Vector3 screenPos = mainCamera.WorldToScreenPoint(unityWorldPos);
                    Vector3 raycastPosition = new Vector3(screenPos.x, screenPos.y, 0);
                    
                    ProcessTouchAtPosition(i + 100, raycastPosition); // Offset webcam touch IDs
                }
            }
        }
        
        void ProcessMobileTouches()
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                Vector3 screenPosition = touch.position;
                
                ProcessTouchAtPosition(touch.fingerId, screenPosition);
                
                // Store touch position for gesture detection
                if (touch.fingerId < lastTouchPositions.Length)
                {
                    lastTouchPositions[touch.fingerId] = touch.position;
                }
            }
        }
        
        void ProcessMouseInput()
        {
            if (Input.GetMouseButtonDown(0))
            {
                Vector3 mousePosition = Input.mousePosition;
                ProcessTouchAtPosition(999, mousePosition); // Use 999 as mouse ID
            }
            
            if (Input.GetMouseButton(0))
            {
                Vector3 mousePosition = Input.mousePosition;
                ProcessTouchMoved(999, mousePosition);
            }
            
            if (Input.GetMouseButtonUp(0))
            {
                Vector3 mousePosition = Input.mousePosition;
                ProcessTouchEnded(999, mousePosition);
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
                Ray ray = mainCamera.ScreenPointToRay(screenPosition);
                Vector3 touchPosition = GetWorldPositionFromScreen(ray);
                
                activeTouches[touchId] = touchPosition;
                
                if (showTouchDebug)
                {
                    Debug.Log($"Touch moved to: {touchPosition}");
                }
            }
        }
        
        void ProcessTouchEnded(int touchId, Vector3 screenPosition)
        {
            if (activeTouches.ContainsKey(touchId))
            {
                activeTouches.Remove(touchId);
                
                if (showTouchDebug)
                {
                    Debug.Log($"Touch ended at: {screenPosition}");
                }
            }
        }
        
        Vector3 GetWorldPositionFromScreen(Ray ray)
        {
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit, 100f, interactionLayer))
            {
                return hit.point;
            }
            
            // If no hit, project onto a virtual plane at z=0
            Plane plane = new Plane(Vector3.forward, Vector3.zero);
            float distance;
            if (plane.Raycast(ray, out distance))
            {
                return ray.GetPoint(distance);
            }
            
            return Vector3.zero;
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
            Vector3 worldPosition = mainCamera.ScreenToWorldPoint(screenPosition);
            worldPosition.z = 0; // Put indicator on screen plane
            
            GameObject indicator = Instantiate(touchIndicatorPrefab, worldPosition, Quaternion.identity);
            touchIndicators.Add(indicator);
            
            // Auto-destroy after a short time
            Destroy(indicator, 2f);
        }
        
        void HandleWebcamTouches(List<WebcamTouchDetector.TouchData> webcamTouches)
        {
            if (!useWebcamInput) return;
            
            // Convert webcam touches to Unity world coordinates and process
            foreach (WebcamTouchDetector.TouchData touch in webcamTouches)
            {
                Vector3 unityWorldPos = new Vector3(touch.position.x, touch.position.y, 0);
                ProcessTouchAtPosition(Random.Range(1000, 9999), unityWorldPos);
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
}
