using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;

/// <summary>
/// Touch Calibration System - Complete webcam touch detection calibration
/// Handles calibration screen, coordinate mapping, sensitivity adjustment, camera position calibration,
/// saved settings, UI management, and integration with WebcamTouchDetector
/// 
/// Attach this to a GameObject and reference it with WebcamTouchDetector
/// </summary>
[System.Serializable]
public class TouchCalibrationData
{
    [Header("Coordinate Mapping")]
    public Vector2 webcamTopLeft = new Vector2(0, 1);      // Webcam coordinate of screen top-left
    public Vector2 webcamTopRight = new Vector2(1, 1);     // Webcam coordinate of screen top-right  
    public Vector2 webcamBottomLeft = new Vector2(0, 0);   // Webcam coordinate of screen bottom-left
    public Vector2 webcamBottomRight = new Vector2(1, 0);  // Webcam coordinate of screen bottom-right
    
    [Header("Camera Settings")]
    public Vector2 cameraOffset = Vector2.zero;            // Camera position offset
    public float cameraRotation = 0f;                      // Camera rotation adjustment
    public float cameraZoom = 1f;                          // Camera zoom adjustment
    
    [Header("Touch Sensitivity")]
    public float touchSensitivity = 1f;                    // Overall touch sensitivity
    public float minTouchArea = 10f;                       // Minimum area for touch detection
    public float maxTouchArea = 1000f;                     // Maximum area for touch detection
    public float noiseThreshold = 0.1f;                    // Noise filtering threshold
    
    [Header("Advanced Settings")]
    public int calibrationQuality = 9;                     // Calibration point quality (0-16)
    public bool flipHorizontal = false;                    // Flip webcam horizontally
    public bool flipVertical = false;                      // Flip webcam vertically
    public Color touchColor = Color.red;                   // Touch indicator color
    
    public TouchCalibrationData()
    {
        // Set default values
        ResetToDefaults();
    }
    
    public void ResetToDefaults()
    {
        webcamTopLeft = new Vector2(0, 1);
        webcamTopRight = new Vector2(1, 1);
        webcamBottomLeft = new Vector2(0, 0);
        webcamBottomRight = new Vector2(1, 0);
        cameraOffset = Vector2.zero;
        cameraRotation = 0f;
        cameraZoom = 1f;
        touchSensitivity = 1f;
        minTouchArea = 10f;
        maxTouchArea = 1000f;
        noiseThreshold = 0.1f;
        calibrationQuality = 9;
        flipHorizontal = false;
        flipVertical = false;
        touchColor = Color.red;
    }
}

public class TouchCalibrationSystem : MonoBehaviour
{
    [Header("System References")]
    public WebcamCapture webcamCapture;
    public WebcamTouchDetector webcamTouchDetector;
    // EnhancedTouchInputManager reference removed - using WebcamTouchDetector directly
    public Camera mainCamera;
    
    [Header("Calibration UI")]
    public Canvas calibrationCanvas;
    public GameObject calibrationPanel;
    public RawImage webcamPreview;
    public RawImage calibrationOverlay;
    public Button startCalibrationButton;
    public Button saveCalibrationButton;
    public Button loadCalibrationButton;
    public Button resetCalibrationButton;
    public Slider sensitivitySlider;
    public Text sensitivityValueText;
    public Text calibrationStatusText;
    public Text instructionsText;
    
    [Header("Calibration Markers")]
    public GameObject calibrationMarkerPrefab;
    public Transform[] calibrationPoints;
    public Color markerColor = Color.green;
    public float markerSize = 50f;
    
    [Header("Touch Indicators")]
    public GameObject touchIndicatorPrefab;
    public Color touchColor = Color.red;
    public float touchIndicatorLifetime = 2f;
    
    [Header("Settings")]
    public string calibrationFileName = "touch_calibration.json";
    public bool autoSaveOnExit = true;
    public bool showDebugInfo = true;
    public KeyCode calibrationToggleKey = KeyCode.C;
    
    // Core calibration data
    [SerializeField] private TouchCalibrationData calibrationData = new TouchCalibrationData();
    
    // State management
    private enum CalibrationState
    {
        Inactive,
        Calibration,
        Testing,
        Complete
    }
    
    private CalibrationState currentState = CalibrationState.Inactive;
    private int currentCalibrationPoint = 0;
    private bool isCalibrated = false;
    private List<GameObject> activeTouchIndicators = new List<GameObject>();
    private List<GameObject> activeCalibrationMarkers = new List<GameObject>();
    
    // Events
    public System.Action<TouchCalibrationData> OnCalibrationCompleted;
    public System.Action<string> OnCalibrationStatus;
    public System.Action OnCalibrationStarted;
    public System.Action OnCalibrationEnded;
    
    // Properties
    public TouchCalibrationData CalibrationData => calibrationData;
    public bool IsCalibrated => isCalibrated;
    public bool IsCalibrating => currentState == CalibrationState.Calibration;
    
    void Start()
    {
        InitializeComponents();
        SetupEventListeners();
        LoadCalibrationSettings();
        UpdateUI();
    }
    
    void InitializeComponents()
    {
        // Auto-find components if not assigned
        if (webcamCapture == null)
            webcamCapture = FindFirstObjectByType<WebcamCapture>();
        
        // Auto-find webcamTouchDetector if not assigned
        if (webcamTouchDetector == null)
            webcamTouchDetector = FindFirstObjectByType<WebcamTouchDetector>();
            
        if (mainCamera == null)
            mainCamera = Camera.main;
        
        // Setup calibration points if not assigned
        if (calibrationPoints == null || calibrationPoints.Length == 0)
        {
            CreateDefaultCalibrationPoints();
        }
        
        // Setup UI
        if (calibrationCanvas != null)
        {
            calibrationCanvas.enabled = false;
        }
        
        Debug.Log("Touch Calibration System initialized");
    }
    
    void SetupEventListeners()
    {
        // UI Button listeners
        if (startCalibrationButton != null)
            startCalibrationButton.onClick.AddListener(StartCalibration);
        
        if (saveCalibrationButton != null)
            saveCalibrationButton.onClick.AddListener(SaveCalibrationSettings);
        
        if (loadCalibrationButton != null)
            loadCalibrationButton.onClick.AddListener(LoadCalibrationSettings);
        
        if (resetCalibrationButton != null)
            resetCalibrationButton.onClick.AddListener(ResetCalibration);
        
        // Slider listeners
        if (sensitivitySlider != null)
        {
            sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
        }
        
        // Webcam events
        if (webcamCapture != null)
        {
            webcamCapture.OnCaptureStarted += OnWebcamStarted;
            webcamCapture.OnCaptureStopped += OnWebcamStopped;
        }
        
        // Touch detection events
        if (webcamTouchDetector != null)
        {
            webcamTouchDetector.OnTouchesDetected += ProcessWebcamTouches;
        }
    }
    
    void Update()
    {
        // Handle calibration toggle key
        if (Input.GetKeyDown(calibrationToggleKey))
        {
            ToggleCalibrationMode();
        }
        
        // Handle mouse clicks during calibration
        if (currentState == CalibrationState.Calibration)
        {
            HandleCalibrationInput();
        }
        
        // Clean up expired touch indicators
        CleanupTouchIndicators();
        
        // Update debug info
        if (showDebugInfo)
        {
            UpdateDebugInfo();
        }
    }
    
    void CreateDefaultCalibrationPoints()
    {
        // Create default 4-point calibration (corners)
        calibrationPoints = new Transform[4];
        
        if (mainCamera != null)
        {
            Vector3 center = mainCamera.transform.position;
            float distance = 5f;
            
            // Screen corners in world space
            Vector3 topLeft = mainCamera.ScreenToWorldPoint(new Vector3(0, Screen.height, distance));
            Vector3 topRight = mainCamera.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, distance));
            Vector3 bottomLeft = mainCamera.ScreenToWorldPoint(new Vector3(0, 0, distance));
            Vector3 bottomRight = mainCamera.ScreenToWorldPoint(new Vector3(Screen.width, 0, distance));
            
            // Create point objects
            calibrationPoints[0] = CreateCalibrationPoint("TopLeft", topLeft);
            calibrationPoints[1] = CreateCalibrationPoint("TopRight", topRight);
            calibrationPoints[2] = CreateCalibrationPoint("BottomLeft", bottomLeft);
            calibrationPoints[3] = CreateCalibrationPoint("BottomRight", bottomRight);
        }
    }
    
    Transform CreateCalibrationPoint(string name, Vector3 position)
    {
        GameObject pointObj = new GameObject($"CalibrationPoint_{name}");
        pointObj.transform.position = position;
        
        // Add visual marker if prefab is available
        if (calibrationMarkerPrefab != null)
        {
            GameObject marker = Instantiate(calibrationMarkerPrefab, position, Quaternion.identity);
            marker.transform.SetParent(pointObj.transform);
        }
        else
        {
            // Create simple visual marker
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.transform.position = position;
            marker.transform.localScale = Vector3.one * 0.1f;
            marker.GetComponent<Renderer>().material.color = markerColor;
        }
        
        return pointObj.transform;
    }
    
    public void StartCalibration()
    {
        if (currentState == CalibrationState.Calibration)
        {
            Debug.LogWarning("Calibration already in progress");
            return;
        }
        
        Debug.Log("Starting touch calibration");
        
        currentState = CalibrationState.Calibration;
        currentCalibrationPoint = 0;
        
        // Show calibration UI
        if (calibrationCanvas != null)
        {
            calibrationCanvas.enabled = true;
        }
        
        if (calibrationPanel != null)
        {
            calibrationPanel.SetActive(true);
        }
        
        // Reset calibration data
        calibrationData.ResetToDefaults();
        
        // Create calibration markers
        CreateCalibrationMarkers();
        
        // Update instructions
        UpdateInstructions($"Click on the green marker to set position {currentCalibrationPoint + 1} of {calibrationPoints.Length}");
        
        OnCalibrationStarted?.Invoke();
        UpdateCalibrationStatus("Calibration started");
    }
    
    void CreateCalibrationMarkers()
    {
        // Clear existing markers
        foreach (GameObject marker in activeCalibrationMarkers)
        {
            if (marker != null)
                Destroy(marker);
        }
        activeCalibrationMarkers.Clear();
        
        // Create markers for current calibration point
        if (currentCalibrationPoint < calibrationPoints.Length)
        {
            Vector3 position = calibrationPoints[currentCalibrationPoint].position;
            
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.transform.position = position;
            marker.transform.localScale = Vector3.one * markerSize * 0.01f;
            marker.GetComponent<Renderer>().material.color = markerColor;
            marker.name = "CalibrationMarker_Current";
            
            activeCalibrationMarkers.Add(marker);
        }
    }
    
    void HandleCalibrationInput()
    {
        // Handle mouse click for calibration point
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mousePos = Input.mousePosition;
            
            // Check if clicked on current calibration marker
            if (currentCalibrationPoint < calibrationPoints.Length)
            {
                Vector3 markerPos = mainCamera.WorldToScreenPoint(calibrationPoints[currentCalibrationPoint].position);
                float distance = Vector3.Distance(mousePos, markerPos);
                
                if (distance < markerSize * 2f) // Click tolerance
                {
                    SetCalibrationPoint(mousePos);
                }
            }
        }
    }
    
    void SetCalibrationPoint(Vector3 screenPosition)
    {
        if (currentCalibrationPoint >= calibrationPoints.Length)
            return;
        
        // Convert screen position to normalized webcam coordinates
        Vector2 webcamPos = ScreenToWebcamCoordinates(screenPosition);
        
        // Set calibration data based on point index
        switch (currentCalibrationPoint)
        {
            case 0: // Top-left
                calibrationData.webcamTopLeft = webcamPos;
                break;
            case 1: // Top-right  
                calibrationData.webcamTopRight = webcamPos;
                break;
            case 2: // Bottom-left
                calibrationData.webcamBottomLeft = webcamPos;
                break;
            case 3: // Bottom-right
                calibrationData.webcamBottomRight = webcamPos;
                break;
        }
        
        Debug.Log($"Calibration point {currentCalibrationPoint + 1} set: {webcamPos}");
        
        currentCalibrationPoint++;
        
        if (currentCalibrationPoint < calibrationPoints.Length)
        {
            // Continue to next point
            CreateCalibrationMarkers();
            UpdateInstructions($"Click on the green marker to set position {currentCalibrationPoint + 1} of {calibrationPoints.Length}");
        }
        else
        {
            // Calibration complete
            CompleteCalibration();
        }
    }
    
    Vector2 ScreenToWebcamCoordinates(Vector3 screenPosition)
    {
        // Normalize screen position to 0-1 range
        Vector2 normalizedPos = new Vector2(
            screenPosition.x / Screen.width,
            1f - (screenPosition.y / Screen.height) // Flip Y coordinate
        );
        
        // Apply camera adjustments
        Vector2 adjustedPos = normalizedPos + calibrationData.cameraOffset;
        adjustedPos = RotateVector2(adjustedPos - Vector2.one * 0.5f, calibrationData.cameraRotation) + Vector2.one * 0.5f;
        adjustedPos = adjustedPos * calibrationData.cameraZoom + (Vector2.one * 0.5f) * (1f - calibrationData.cameraZoom);
        
        // Apply flips
        if (calibrationData.flipHorizontal)
            adjustedPos.x = 1f - adjustedPos.x;
        if (calibrationData.flipVertical)
            adjustedPos.y = 1f - adjustedPos.y;
        
        return adjustedPos;
    }
    
    Vector2 WebcamToScreenCoordinates(Vector2 webcamPosition)
    {
        // Apply inverse camera adjustments
        Vector2 adjustedPos = (webcamPosition - Vector2.one * 0.5f) / calibrationData.cameraZoom + Vector2.one * 0.5f;
        adjustedPos = RotateVector2(adjustedPos - Vector2.one * 0.5f, -calibrationData.cameraRotation) + Vector2.one * 0.5f;
        adjustedPos -= calibrationData.cameraOffset;
        
        // Apply inverse flips
        if (calibrationData.flipHorizontal)
            adjustedPos.x = 1f - adjustedPos.x;
        if (calibrationData.flipVertical)
            adjustedPos.y = 1f - adjustedPos.y;
        
        // Convert to screen coordinates
        return new Vector2(
            adjustedPos.x * Screen.width,
            (1f - adjustedPos.y) * Screen.height // Flip Y coordinate
        );
    }
    
    Vector2 RotateVector2(Vector2 vector, float angleDegrees)
    {
        float angle = angleDegrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(angle);
        float sin = Mathf.Sin(angle);
        
        return new Vector2(
            vector.x * cos - vector.y * sin,
            vector.x * sin + vector.y * cos
        );
    }
    
    void CompleteCalibration()
    {
        Debug.Log("Touch calibration completed");
        
        currentState = CalibrationState.Complete;
        isCalibrated = true;
        
        // Remove calibration markers
        foreach (GameObject marker in activeCalibrationMarkers)
        {
            if (marker != null)
                Destroy(marker);
        }
        activeCalibrationMarkers.Clear();
        
        // Hide calibration UI or switch to testing mode
        if (calibrationPanel != null)
        {
            calibrationPanel.SetActive(false);
        }
        
        // Notify listeners
        OnCalibrationCompleted?.Invoke(calibrationData);
        UpdateCalibrationStatus("Calibration completed successfully");
        
        // Auto-save if enabled
        if (autoSaveOnExit)
        {
            SaveCalibrationSettings();
        }
    }
    
    public void ToggleCalibrationMode()
    {
        if (currentState == CalibrationState.Inactive)
        {
            StartCalibration();
        }
        else
        {
            EndCalibration();
        }
    }
    
    public void EndCalibration()
    {
        Debug.Log("Ending calibration");
        
        currentState = CalibrationState.Inactive;
        currentCalibrationPoint = 0;
        
        // Hide calibration UI
        if (calibrationCanvas != null)
        {
            calibrationCanvas.enabled = false;
        }
        
        if (calibrationPanel != null)
        {
            calibrationPanel.SetActive(false);
        }
        
        // Remove calibration markers
        foreach (GameObject marker in activeCalibrationMarkers)
        {
            if (marker != null)
                Destroy(marker);
        }
        activeCalibrationMarkers.Clear();
        
        OnCalibrationEnded?.Invoke();
        UpdateCalibrationStatus("Calibration ended");
    }
    
    void ProcessWebcamTouches(List<WebcamTouchDetector.TouchData> webcamTouches)
    {
        if (currentState == CalibrationState.Testing || currentState == CalibrationState.Complete)
        {
            foreach (WebcamTouchDetector.TouchData touch in webcamTouches)
            {
                if (touch.isActive)
                {
                    Vector2 screenPos = WebcamToScreenCoordinates(touch.position);
                    CreateTouchIndicator(screenPos);
                }
            }
        }
    }
    
    void CreateTouchIndicator(Vector3 screenPosition)
    {
        if (touchIndicatorPrefab == null) return;
        
        // Convert screen position to world position
        Vector3 worldPosition = mainCamera.ScreenToWorldPoint(screenPosition);
        worldPosition.z = 0f;
        
        GameObject indicator = Instantiate(touchIndicatorPrefab, worldPosition, Quaternion.identity);
        activeTouchIndicators.Add(indicator);
        
        // Set color
        Renderer renderer = indicator.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = calibrationData.touchColor;
        }
        
        // Auto-destroy after lifetime
        Destroy(indicator, touchIndicatorLifetime);
    }
    
    void CleanupTouchIndicators()
    {
        activeTouchIndicators.RemoveAll(indicator => indicator == null);
        
        // Limit number of indicators to prevent memory issues
        if (activeTouchIndicators.Count > 20)
        {
            GameObject oldest = activeTouchIndicators[0];
            activeTouchIndicators.RemoveAt(0);
            if (oldest != null)
            {
                Destroy(oldest);
            }
        }
    }
    
    void OnSensitivityChanged(float value)
    {
        calibrationData.touchSensitivity = value;
        
        if (sensitivityValueText != null)
        {
            sensitivityValueText.text = $"Sensitivity: {value:F2}";
        }
        
        UpdateCalibrationStatus($"Sensitivity: {value:F2}");
    }
    
    public void SaveCalibrationSettings()
    {
        try
        {
            string filePath = Path.Combine(Application.persistentDataPath, calibrationFileName);
            string jsonData = JsonUtility.ToJson(calibrationData, true);
            
            File.WriteAllText(filePath, jsonData);
            
            Debug.Log($"Touch calibration settings saved to: {filePath}");
            UpdateCalibrationStatus("Settings saved successfully");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save calibration settings: {e.Message}");
            UpdateCalibrationStatus("Failed to save settings");
        }
    }
    
    public void LoadCalibrationSettings()
    {
        try
        {
            string filePath = Path.Combine(Application.persistentDataPath, calibrationFileName);
            
            if (File.Exists(filePath))
            {
                string jsonData = File.ReadAllText(filePath);
                calibrationData = JsonUtility.FromJson<TouchCalibrationData>(jsonData);
                
                // Update UI to reflect loaded settings
                if (sensitivitySlider != null)
                {
                    sensitivitySlider.value = calibrationData.touchSensitivity;
                }
                
                isCalibrated = true;
                currentState = CalibrationState.Complete;
                
                Debug.Log($"Touch calibration settings loaded from: {filePath}");
                UpdateCalibrationStatus("Settings loaded successfully");
            }
            else
            {
                Debug.Log("No calibration file found, using defaults");
                UpdateCalibrationStatus("Using default settings");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load calibration settings: {e.Message}");
            UpdateCalibrationStatus("Failed to load settings");
            calibrationData.ResetToDefaults();
        }
        
        UpdateUI();
    }
    
    public void ResetCalibration()
    {
        Debug.Log("Resetting calibration to defaults");
        
        calibrationData.ResetToDefaults();
        isCalibrated = false;
        currentState = CalibrationState.Inactive;
        
        // Update UI
        if (sensitivitySlider != null)
        {
            sensitivitySlider.value = calibrationData.touchSensitivity;
        }
        
        UpdateCalibrationStatus("Calibration reset to defaults");
    }
    
    void UpdateUI()
    {
        // Update sensitivity slider
        if (sensitivitySlider != null)
        {
            sensitivitySlider.value = calibrationData.touchSensitivity;
        }
        
        if (sensitivityValueText != null)
        {
            sensitivityValueText.text = $"Sensitivity: {calibrationData.touchSensitivity:F2}";
        }
        
        // Update status
        UpdateCalibrationStatus(isCalibrated ? "Calibrated" : "Not calibrated");
    }
    
    void UpdateInstructions(string message)
    {
        if (instructionsText != null)
        {
            instructionsText.text = message;
        }
    }
    
    void UpdateCalibrationStatus(string message)
    {
        if (calibrationStatusText != null)
        {
            calibrationStatusText.text = message;
        }
        
        OnCalibrationStatus?.Invoke(message);
        
        if (showDebugInfo)
        {
            Debug.Log($"Calibration Status: {message}");
        }
    }
    
    void UpdateDebugInfo()
    {
        // This could be expanded to show real-time debug information
        // such as current touch positions, calibration accuracy, etc.
    }
    
    // Event handlers
    void OnWebcamStarted()
    {
        UpdateCalibrationStatus("Webcam connected");
    }
    
    void OnWebcamStopped()
    {
        UpdateCalibrationStatus("Webcam disconnected");
    }
    
    // Public API for external components
    
    public Vector2 GetScreenPositionFromWebcam(Vector2 webcamPosition)
    {
        return WebcamToScreenCoordinates(webcamPosition);
    }
    
    public Vector2 GetWebcamPositionFromScreen(Vector3 screenPosition)
    {
        return ScreenToWebcamCoordinates(screenPosition);
    }
    
    public void SetCameraOffset(Vector2 offset)
    {
        calibrationData.cameraOffset = offset;
    }
    
    public void SetCameraRotation(float rotation)
    {
        calibrationData.cameraRotation = rotation;
    }
    
    public void SetCameraZoom(float zoom)
    {
        calibrationData.cameraZoom = Mathf.Clamp(zoom, 0.1f, 2f);
    }
    
    public void SetFlipHorizontal(bool flip)
    {
        calibrationData.flipHorizontal = flip;
    }
    
    public void SetFlipVertical(bool flip)
    {
        calibrationData.flipVertical = flip;
    }
    
    public void SetTouchAreaRange(float min, float max)
    {
        calibrationData.minTouchArea = min;
        calibrationData.maxTouchArea = max;
    }
    
    public void SetNoiseThreshold(float threshold)
    {
        calibrationData.noiseThreshold = Mathf.Clamp01(threshold);
    }
    
    // Context menu methods for easy testing
    [ContextMenu("Start Quick Calibration")]
    public void StartQuickCalibration()
    {
        StartCalibration();
    }
    
    [ContextMenu("Test Touch Mapping")]
    public void TestTouchMapping()
    {
        // Test coordinate mapping with sample points
        Vector2[] testPoints = {
            new Vector2(0, 0),
            new Vector2(0.5f, 0.5f),
            new Vector2(1, 1)
        };
        
        foreach (Vector2 webcamPos in testPoints)
        {
            Vector2 screenPos = WebcamToScreenCoordinates(webcamPos);
            Vector2 backToWebcam = ScreenToWebcamCoordinates(screenPos);
            
            Debug.Log($"Webcam: {webcamPos} -> Screen: {screenPos} -> Webcam: {backToWebcam}");
        }
    }
    
    void OnDrawGizmos()
    {
        // Draw calibration points in editor
        if (calibrationPoints != null && calibrationPoints.Length > 0)
        {
            Gizmos.color = Color.green;
            
            foreach (Transform point in calibrationPoints)
            {
                if (point != null)
                {
                    Gizmos.DrawWireSphere(point.position, 0.1f);
                }
            }
        }
        
        // Draw current touch indicators in play mode
        if (Application.isPlaying && activeTouchIndicators.Count > 0)
        {
            Gizmos.color = Color.red;
            
            foreach (GameObject indicator in activeTouchIndicators)
            {
                if (indicator != null)
                {
                    Gizmos.DrawWireSphere(indicator.transform.position, 0.2f);
                }
            }
        }
    }
    
    void OnDestroy()
    {
        // Clean up
        foreach (GameObject marker in activeCalibrationMarkers)
        {
            if (marker != null)
                Destroy(marker);
        }
        
        foreach (GameObject indicator in activeTouchIndicators)
        {
            if (indicator != null)
                Destroy(indicator);
        }
        
        // Auto-save on exit
        if (autoSaveOnExit && isCalibrated)
        {
            SaveCalibrationSettings();
        }
    }
}
