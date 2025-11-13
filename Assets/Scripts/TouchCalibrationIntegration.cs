using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Touch Calibration System Integration Example
/// Demonstrates how to integrate TouchCalibrationSystem with existing webcam components
/// Attach this to a GameObject to set up a complete touch detection system
/// </summary>
public class TouchCalibrationIntegration : MonoBehaviour
{
    [Header("Core Components")]
    public WebcamCapture webcamCapture;
    public WebcamTouchDetector touchDetector;
    public TouchCalibrationSystem calibrationSystem;

    
    [Header("UI References")]
    public Canvas mainCanvas;
    public Button startCalibrationButton;
    public Button testTouchButton;
    public Text statusText;
    public Slider sensitivitySlider;
    public Toggle webcamToggle;
    public Toggle calibrationToggle;
    
    [Header("Touch Testing")]
    public GameObject testTouchIndicatorPrefab;
    public Color testTouchColor = Color.yellow;
    
    [Header("System Settings")]
    public bool autoInitialize = true;
    public bool showIntegrationDebug = true;
    
    private bool systemInitialized = false;
    
    void Start()
    {
        if (autoInitialize)
        {
            InitializeTouchSystem();
        }
    }
    
    void InitializeTouchSystem()
    {
        Debug.Log("Initializing Touch Calibration System...");
        
        // Find and validate core components
        if (!FindAndValidateComponents())
        {
            Debug.LogError("Failed to initialize touch system - missing components");
            return;
        }
        
        // Set up component connections
        SetupComponentConnections();
        
        // Configure touch detector
        ConfigureTouchDetector();
        
        // Set up UI if available
        SetupUI();
        
        // Subscribe to events
        SetupEventListeners();
        
        systemInitialized = true;
        
        UpdateStatusText("Touch system initialized successfully");
        Debug.Log("✅ Touch Calibration System initialized");
    }
    
    bool FindAndValidateComponents()
    {
        bool allFound = true;
        
        // Find WebcamCapture
        if (webcamCapture == null)
        {
            webcamCapture = FindFirstObjectByType<WebcamCapture>();
            if (webcamCapture == null)
            {
                Debug.LogError("WebcamCapture not found!");
                allFound = false;
            }
            else
            {
                Debug.Log("Found WebcamCapture");
            }
        }
        
        // Find WebcamTouchDetector
        if (touchDetector == null)
        {
            touchDetector = FindFirstObjectByType<WebcamTouchDetector>();
            if (touchDetector == null)
            {
                Debug.LogError("WebcamTouchDetector not found!");
                allFound = false;
            }
            else
            {
                Debug.Log("Found WebcamTouchDetector");
            }
        }
        
        // Find TouchCalibrationSystem
        if (calibrationSystem == null)
        {
            calibrationSystem = FindFirstObjectByType<TouchCalibrationSystem>();
            if (calibrationSystem == null)
            {
                Debug.LogError("TouchCalibrationSystem not found!");
                allFound = false;
            }
            else
            {
                Debug.Log("Found TouchCalibrationSystem");
            }
        }
        

        
        return allFound;
    }
    
    void SetupComponentConnections()
    {
        // Connect TouchCalibrationSystem with WebcamTouchDetector
        if (calibrationSystem != null && touchDetector != null)
        {
            calibrationSystem.webcamTouchDetector = touchDetector;
        }
        
        // Connect TouchCalibrationSystem with WebcamCapture
        if (calibrationSystem != null && webcamCapture != null)
        {
            calibrationSystem.webcamCapture = webcamCapture;
        }
        
        // Direct component connections are established automatically
        // TouchCalibrationSystem and WebcamTouchDetector work together directly
        
        if (showIntegrationDebug)
        {
            Debug.Log("Component connections established");
        }
    }
    
    void ConfigureTouchDetector()
    {
        if (touchDetector == null) return;
        
        // Configure touch detection settings
        touchDetector.EnableTouchDetection = true;
        touchDetector.TouchSensitivity = 1f;
        touchDetector.MinTouchArea = 30; // Fixed: should be int, not float
        touchDetector.MaxTouchArea = 1500f;
        touchDetector.MaxTouchPoints = 5;
        
        // Set detection color to red (assuming red objects will be used for touch)
        touchDetector.DetectionColor = new Color32(255, 50, 50, 255);
        touchDetector.DetectionColorThreshold = 0.8f; // Fixed: should be 0-1 range, not 80f
        
        // Note: processingWidth and processingHeight are read-only properties
        // They reflect the actual webcam resolution and cannot be set directly
        
        if (showIntegrationDebug)
        {
            Debug.Log("Touch detector configured");
        }
    }
    
    void SetupUI()
    {
        // Set up UI event listeners
        if (startCalibrationButton != null)
        {
            startCalibrationButton.onClick.RemoveAllListeners();
            startCalibrationButton.onClick.AddListener(StartCalibration);
        }
        
        if (testTouchButton != null)
        {
            testTouchButton.onClick.RemoveAllListeners();
            testTouchButton.onClick.AddListener(TestTouchDetection);
        }
        
        if (sensitivitySlider != null)
        {
            sensitivitySlider.onValueChanged.RemoveAllListeners();
            sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
            
            // Set initial value
            if (touchDetector != null)
            {
                sensitivitySlider.value = touchDetector.TouchSensitivity;
            }
        }
        
        if (webcamToggle != null)
        {
            webcamToggle.onValueChanged.RemoveAllListeners();
            webcamToggle.onValueChanged.AddListener(OnWebcamToggleChanged);
            
            // Set initial state
            webcamToggle.isOn = webcamCapture != null && webcamCapture.IsInitialized;
        }
        
        if (calibrationToggle != null)
        {
            calibrationToggle.onValueChanged.RemoveAllListeners();
            calibrationToggle.onValueChanged.AddListener(OnCalibrationToggleChanged);
        }
        
        if (showIntegrationDebug)
        {
            Debug.Log("UI setup completed");
        }
    }
    
    void SetupEventListeners()
    {
        // Touch detection events
        if (touchDetector != null)
        {
            touchDetector.OnTouchesDetected += OnTouchesDetected;
            touchDetector.OnTouchCountChanged += OnTouchCountChanged;
            touchDetector.OnSingleTouchDetected += OnSingleTouchDetected;
            touchDetector.OnNoTouchesDetected += OnNoTouchesDetected;
        }
        
        // Calibration events
        if (calibrationSystem != null)
        {
            calibrationSystem.OnCalibrationCompleted += OnCalibrationCompleted;
            calibrationSystem.OnCalibrationStatus += OnCalibrationStatus;
            calibrationSystem.OnCalibrationStarted += OnCalibrationStarted;
            calibrationSystem.OnCalibrationEnded += OnCalibrationEnded;
        }
        
        // Webcam events
        if (webcamCapture != null)
        {
            webcamCapture.OnCaptureStarted += OnWebcamStarted;
            webcamCapture.OnCaptureStopped += OnWebcamStopped;
            webcamCapture.OnStatusUpdate += OnWebcamStatusUpdate;
        }
        
        if (showIntegrationDebug)
        {
            Debug.Log("Event listeners setup completed");
        }
    }
    
    // Event handlers
    
    void OnTouchesDetected(List<WebcamTouchDetector.TouchData> touches)
    {
        if (showIntegrationDebug)
        {
            Debug.Log($"Integration: Detected {touches.Count} touches");
        }
        
        UpdateStatusText($"Touches detected: {touches.Count}");
        
        // Create visual indicators for detected touches
        CreateTouchIndicatorsFromTouchData(touches);
    }
    
    void OnTouchCountChanged(int count)
    {
        if (showIntegrationDebug)
        {
            Debug.Log($"Integration: Touch count changed to {count}");
        }
    }
    
    void OnSingleTouchDetected(Vector2 touchPosition)
    {
        if (showIntegrationDebug)
        {
            Debug.Log($"Integration: Single touch at {touchPosition}");
        }
    }
    
    void OnNoTouchesDetected()
    {
        if (showIntegrationDebug)
        {
            Debug.Log("Integration: No touches detected");
        }
    }
    
    void OnCalibrationCompleted(TouchCalibrationData data)
    {
        Debug.Log("Integration: Calibration completed");
        UpdateStatusText("Calibration completed - touch system ready");
        
        // Calibration completed - system is ready for use
        // TouchCalibrationSystem will handle input processing directly
    }
    
    void OnCalibrationStatus(string status)
    {
        UpdateStatusText($"Calibration: {status}");
    }
    
    void OnCalibrationStarted()
    {
        Debug.Log("Integration: Calibration started");
        UpdateStatusText("Calibration started - follow on-screen instructions");
    }
    
    void OnCalibrationEnded()
    {
        Debug.Log("Integration: Calibration ended");
        UpdateStatusText("Calibration ended");
    }
    
    void OnWebcamStarted()
    {
        Debug.Log("Integration: Webcam started");
        UpdateStatusText("Webcam connected");
        
        if (webcamToggle != null)
        {
            webcamToggle.isOn = true;
        }
    }
    
    void OnWebcamStopped()
    {
        Debug.Log("Integration: Webcam stopped");
        UpdateStatusText("Webcam disconnected");
        
        if (webcamToggle != null)
        {
            webcamToggle.isOn = false;
        }
    }
    
    void OnWebcamStatusUpdate(string status)
    {
        if (showIntegrationDebug)
        {
            Debug.Log($"Webcam Status: {status}");
        }
    }
    
    // UI Event Handlers
    
    void StartCalibration()
    {
        if (calibrationSystem != null)
        {
            calibrationSystem.StartCalibration();
        }
        else
        {
            Debug.LogError("Calibration system not available");
        }
    }
    
    void TestTouchDetection()
    {
        if (touchDetector != null)
        {
            touchDetector.SimulateTouch(new Vector2(0.5f, 0.5f), true);
            UpdateStatusText("Touch test triggered");
        }
        else
        {
            Debug.LogError("Touch detector not available");
        }
    }
    
    void OnSensitivityChanged(float value)
    {
        if (touchDetector != null)
        {
            touchDetector.SetTouchSensitivity(value);
            UpdateStatusText($"Sensitivity: {value:F2}");
        }
    }
    
    void OnWebcamToggleChanged(bool isOn)
    {
        if (webcamCapture != null)
        {
            if (isOn && !webcamCapture.IsInitialized)
            {
                webcamCapture.InitializeWebcam();
            }
            else if (!isOn && webcamCapture.IsInitialized)
            {
                webcamCapture.StopWebcam();
            }
        }
    }
    
    void OnCalibrationToggleChanged(bool isOn)
    {
        if (isOn)
        {
            StartCalibration();
        }
        else
        {
            if (calibrationSystem != null)
            {
                calibrationSystem.EndCalibration();
            }
        }
    }
    
    // Helper methods
    
    void CreateTouchIndicatorsFromTouchData(List<WebcamTouchDetector.TouchData> touches)
    {
        if (testTouchIndicatorPrefab == null) return;
        
        foreach (WebcamTouchDetector.TouchData touch in touches)
        {
            // Convert normalized position to world position
            Vector3 worldPos = new Vector3(
                touch.position.x * 10f - 5f,  // Assuming 10-unit wide area centered at origin
                touch.position.y * 10f - 5f,
                0f
            );
            
            GameObject indicator = Instantiate(testTouchIndicatorPrefab, worldPos, Quaternion.identity);
            
            // Set color based on touch intensity
            Renderer renderer = indicator.GetComponent<Renderer>();
            if (renderer != null)
            {
                Color indicatorColor = Color.Lerp(Color.yellow, Color.red, touch.intensity);
                renderer.material.color = indicatorColor;
            }
            
            // Auto-destroy after a short time
            Destroy(indicator, 2f);
        }
    }
    
    // Legacy method for backward compatibility
    void CreateTouchIndicators(List<Vector2> touches)
    {
        if (testTouchIndicatorPrefab == null) return;
        
        foreach (Vector2 touchPos in touches)
        {
            // Convert normalized position to world position
            Vector3 worldPos = new Vector3(
                touchPos.x * 10f - 5f,  // Assuming 10-unit wide area centered at origin
                touchPos.y * 10f - 5f,
                0f
            );
            
            GameObject indicator = Instantiate(testTouchIndicatorPrefab, worldPos, Quaternion.identity);
            
            // Set color
            Renderer renderer = indicator.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = testTouchColor;
            }
            
            // Auto-destroy after a short time
            Destroy(indicator, 2f);
        }
    }
    
    void UpdateStatusText(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }
    
    // Public API for external control
    
    public void EnableTouchSystem(bool enable)
    {
        if (touchDetector != null)
        {
            touchDetector.SetTouchDetectionEnabled(enable);
        }
        
        if (enable && !systemInitialized)
        {
            InitializeTouchSystem();
        }
    }
    
    public void SetSystemSensitivity(float sensitivity)
    {
        if (touchDetector != null)
        {
            touchDetector.SetTouchSensitivity(sensitivity);
        }
        
        if (sensitivitySlider != null)
        {
            sensitivitySlider.value = sensitivity;
        }
    }
    
    public void ResetSystem()
    {
        if (touchDetector != null)
        {
            touchDetector.ClearAllTouches();
            touchDetector.ClearTouchHistory();
        }
        
        if (calibrationSystem != null)
        {
            calibrationSystem.ResetCalibration();
        }
        
        UpdateStatusText("System reset");
    }
    
    public void SaveSystemSettings()
    {
        if (calibrationSystem != null)
        {
            calibrationSystem.SaveCalibrationSettings();
        }
    }
    
    public void LoadSystemSettings()
    {
        if (calibrationSystem != null)
        {
            calibrationSystem.LoadCalibrationSettings();
        }
    }
    
    public string GetSystemStatus()
    {
        string status = "Touch System Status:\n";
        
        status += $"Initialized: {systemInitialized}\n";
        
        if (webcamCapture != null)
        {
            status += $"Webcam: {(webcamCapture.IsInitialized ? "Ready" : "Not Ready")}\n";
        }
        
        if (touchDetector != null)
        {
            status += $"Touch Detection: {(touchDetector.IsProcessing ? "Processing" : "Ready")}\n";
            status += $"Active Touches: {touchDetector.TouchCount}\n";
        }
        
        if (calibrationSystem != null)
        {
            status += $"Calibration: {(calibrationSystem.IsCalibrated ? "Complete" : "Not Calibrated")}\n";
        }
        
        return status;
    }
    
    // Testing and debugging
    
    [ContextMenu("Test System Integration")]
    public void TestSystemIntegration()
    {
        Debug.Log("Testing system integration...");
        
        string status = GetSystemStatus();
        Debug.Log(status);
        
        UpdateStatusText("System test completed - check console for details");
    }
    
    [ContextMenu("Force System Initialization")]
    public void ForceSystemInitialization()
    {
        InitializeTouchSystem();
    }
    
    void OnGUI()
    {
        if (!showIntegrationDebug) return;
        
        // Simple debug display
        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Label("Touch System Debug");
        GUILayout.Label($"Initialized: {systemInitialized}");
        
        if (webcamCapture != null)
        {
            GUILayout.Label($"Webcam: {webcamCapture.Status}");
        }
        
        if (touchDetector != null)
        {
            GUILayout.Label($"Touches: {touchDetector.TouchCount}");
            GUILayout.Label($"Processing: {touchDetector.IsProcessing}");
        }
        
        if (calibrationSystem != null)
        {
            GUILayout.Label($"Calibrated: {calibrationSystem.IsCalibrated}");
        }
        
        GUILayout.EndArea();
    }
}
