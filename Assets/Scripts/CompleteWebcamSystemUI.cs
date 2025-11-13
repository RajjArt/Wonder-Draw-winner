using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Complete Webcam Character System UI - Full System Integration
/// Comprehensive UI controller that manages all aspects of the webcam character system
/// Provides real-time monitoring, character management, and system controls for runtime operation
/// </summary>
public class CompleteWebcamSystemUI : MonoBehaviour
{
    [Header("Main UI Panels")]
    [SerializeField] private GameObject mainDashboard;
    [SerializeField] private GameObject cameraPanel;
    [SerializeField] private GameObject characterPanel;
    [SerializeField] private GameObject positioningPanel;
    [SerializeField] private GameObject systemPanel;
    [SerializeField] private GameObject demoPanel;
    
    [Header("Control Buttons")]
    [SerializeField] private Button initializeCameraButton;
    [SerializeField] private Button startCaptureButton;
    [SerializeField] private Button stopCaptureButton;
    [SerializeField] private Button createCharacterButton;
    [SerializeField] private Button clearAllButton;
    [SerializeField] private Button runDemoButton;
    [SerializeField] private Button showStatisticsButton;
    [SerializeField] private Button resetSystemButton;
    
    [Header("Navigation Buttons")]
    [SerializeField] private Button cameraTabButton;
    [SerializeField] private Button characterTabButton;
    [SerializeField] private Button positioningTabButton;
    [SerializeField] private Button systemTabButton;
    [SerializeField] private Button demoTabButton;
    
    [Header("Display Elements - Dashboard")]
    [SerializeField] private Text systemStatusText;
    [SerializeField] private Text cameraStatusText;
    [SerializeField] private Text characterCountText;
    [SerializeField] private Text performanceText;
    [SerializeField] private Text fpsText;
    [SerializeField] private Image systemHealthBar;
    
    [Header("Display Elements - Camera")]
    [SerializeField] private RawImage cameraFeed;
    [SerializeField] private Text cameraResolutionText;
    [SerializeField] private Text cameraFPSText;
    [SerializeField] private Slider colorToleranceSlider;
    [SerializeField] private Text colorToleranceValue;
    [SerializeField] private Slider backgroundThresholdSlider;
    [SerializeField] private Text backgroundThresholdValue;
    [SerializeField] private Toggle edgeSmoothingToggle;
    [SerializeField] private Toggle multiPassToggle;
    
    [Header("Display Elements - Characters")]
    [SerializeField] private Transform characterListContent;
    [SerializeField] private GameObject characterListItemPrefab;
    [SerializeField] private Text totalCharactersText;
    [SerializeField] private Text activeCharactersText;
    [SerializeField] private Text memoryUsageText;
    [SerializeField] private ScrollRect characterScrollRect;
    
    [Header("Display Elements - Positioning")]
    [SerializeField] private RawImage positioningOverlay;
    [SerializeField] private Text areaUtilizationText;
    [SerializeField] private Text expansionLevelText;
    [SerializeField] private Text gridSizeText;
    [SerializeField] private Toggle showAreaBoundsToggle;
    [SerializeField] private Toggle showGridToggle;
    [SerializeField] private Slider characterSpacingSlider;
    [SerializeField] private Text spacingValueText;
    
    [Header("Display Elements - System")]
    [SerializeField] private Text systemStatsText;
    [SerializeField] private Text performanceMetricsText;
    [SerializeField] private Text errorLogText;
    [SerializeField] private ScrollRect errorLogScroll;
    [SerializeField] private Toggle autoPerformanceMonitoringToggle;
    [SerializeField] private Slider lodDistanceSlider;
    [SerializeField] private Text lodValueText;
    
    [Header("Integration")]
    [SerializeField] private WebcamCapture webcamCapture;
    [SerializeField] private BackgroundRemover backgroundRemover;
    [SerializeField] private WebcamCharacterPipeline pipeline;
    [SerializeField] private MultipleCharacterManager characterManager;
    [SerializeField] private CharacterAreaPositioning positioning;
    
    // UI State Management
    private bool isDemoRunning = false;
    private string currentTab = "dashboard";
    private List<UICharacterItem> characterListItems = new List<UICharacterItem>();
    
    // Performance monitoring
    private float lastFpsUpdate = 0f;
    private float currentFPS = 0f;
    private List<float> performanceHistory = new List<float>();
    private float lastPerformanceCheck = 0f;
    
    // Error logging
    private List<string> errorLog = new List<string>();
    private int maxErrorLogEntries = 50;
    
    // Auto-refresh settings
    private float uiRefreshRate = 0.5f;
    private float lastUiUpdate = 0f;
    
    // Character list item class
    private class UICharacterItem
    {
        public string characterId;
        public string characterName;
        public GameObject gameObject;
        public Text nameText;
        public Text statusText;
        public Text positionText;
        public Button activateButton;
        public Button deactivateButton;
        public Button removeButton;
        public Image statusImage;
    }
    
    private void Awake()
    {
        // Auto-find components if not assigned
        FindAllComponents();
    }
    
    private void Start()
    {
        InitializeUI();
        SetupEventSubscriptions();
        ShowDashboard();
        
        UnityEngine.Debug.Log("🎮 Complete Webcam System UI initialized");
    }
    
    private void Update()
    {
        // Auto-refresh UI
        if (Time.time - lastUiUpdate > uiRefreshRate)
        {
            UpdateDashboard();
            UpdatePerformanceMetrics();
            lastUiUpdate = Time.time;
        }
        
        // Update FPS counter
        UpdateFPS();
        
        // Auto-performance monitoring
        if (autoPerformanceMonitoringToggle != null && autoPerformanceMonitoringToggle.isOn)
        {
            CheckSystemHealth();
        }
    }
    
    /// <summary>
    /// Find all required components
    /// </summary>
    private void FindAllComponents()
    {
        if (webcamCapture == null) webcamCapture = FindFirstObjectByType<WebcamCapture>();
        if (backgroundRemover == null) backgroundRemover = FindFirstObjectByType<BackgroundRemover>();
        if (pipeline == null) pipeline = FindFirstObjectByType<WebcamCharacterPipeline>();
        if (characterManager == null) characterManager = FindFirstObjectByType<MultipleCharacterManager>();
        if (positioning == null) positioning = FindFirstObjectByType<CharacterAreaPositioning>();
    }
    
    /// <summary>
    /// Initialize UI components and event listeners
    /// </summary>
    private void InitializeUI()
    {
        // Setup control button listeners
        SetupButtonListeners();
        
        // Setup tab navigation
        SetupTabNavigation();
        
        // Setup slider listeners
        SetupSliderListeners();
        
        // Initialize display values
        InitializeDisplayValues();
        
        // Hide all panels except dashboard
        HideAllPanels();
        ShowPanel(mainDashboard);
    }
    
    /// <summary>
    /// Setup button event listeners
    /// </summary>
    private void SetupButtonListeners()
    {
        if (initializeCameraButton != null)
            initializeCameraButton.onClick.AddListener(InitializeCamera);
        
        if (startCaptureButton != null)
            startCaptureButton.onClick.AddListener(StartCapture);
        
        if (stopCaptureButton != null)
            stopCaptureButton.onClick.AddListener(StopCapture);
        
        if (createCharacterButton != null)
            createCharacterButton.onClick.AddListener(CreateCharacter);
        
        if (clearAllButton != null)
            clearAllButton.onClick.AddListener(ClearAllCharacters);
        
        if (runDemoButton != null)
            runDemoButton.onClick.AddListener(RunDemo);
        
        if (showStatisticsButton != null)
            showStatisticsButton.onClick.AddListener(ShowStatistics);
        
        if (resetSystemButton != null)
            resetSystemButton.onClick.AddListener(ResetSystem);
    }
    
    /// <summary>
    /// Setup tab navigation
    /// </summary>
    private void SetupTabNavigation()
    {
        if (cameraTabButton != null)
            cameraTabButton.onClick.AddListener(() => ShowTab("camera"));
        
        if (characterTabButton != null)
            characterTabButton.onClick.AddListener(() => ShowTab("characters"));
        
        if (positioningTabButton != null)
            positioningTabButton.onClick.AddListener(() => ShowTab("positioning"));
        
        if (systemTabButton != null)
            systemTabButton.onClick.AddListener(() => ShowTab("system"));
        
        if (demoTabButton != null)
            demoTabButton.onClick.AddListener(() => ShowTab("demo"));
    }
    
    /// <summary>
    /// Setup slider event listeners
    /// </summary>
    private void SetupSliderListeners()
    {
        if (colorToleranceSlider != null)
            colorToleranceSlider.onValueChanged.AddListener(OnColorToleranceChanged);
        
        if (backgroundThresholdSlider != null)
            backgroundThresholdSlider.onValueChanged.AddListener(OnBackgroundThresholdChanged);
        
        if (characterSpacingSlider != null)
            characterSpacingSlider.onValueChanged.AddListener(OnCharacterSpacingChanged);
        
        if (lodDistanceSlider != null)
            lodDistanceSlider.onValueChanged.AddListener(OnLODDistanceChanged);
    }
    
    /// <summary>
    /// Initialize display values
    /// </summary>
    private void InitializeDisplayValues()
    {
        // Camera settings
        if (colorToleranceSlider != null) colorToleranceSlider.value = 0.15f;
        if (backgroundThresholdSlider != null) backgroundThresholdSlider.value = 0.8f;
        if (characterSpacingSlider != null) characterSpacingSlider.value = 3f;
        if (lodDistanceSlider != null) lodDistanceSlider.value = 15f;
        
        // Toggle states
        if (edgeSmoothingToggle != null) edgeSmoothingToggle.isOn = true;
        if (multiPassToggle != null) multiPassToggle.isOn = true;
        if (showAreaBoundsToggle != null) showAreaBoundsToggle.isOn = true;
        if (showGridToggle != null) showGridToggle.isOn = true;
        if (autoPerformanceMonitoringToggle != null) autoPerformanceMonitoringToggle.isOn = true;
    }
    
    /// <summary>
    /// Setup event subscriptions with other components
    /// </summary>
    private void SetupEventSubscriptions()
    {
        if (webcamCapture != null)
        {
            webcamCapture.OnStatusUpdate += OnCameraStatusUpdate;
            webcamCapture.OnFrameCaptured += OnFrameCaptured;
            webcamCapture.OnCaptureStarted += OnCaptureStarted;
            webcamCapture.OnCaptureStopped += OnCaptureStopped;
        }
        
        if (backgroundRemover != null)
        {
            backgroundRemover.OnStatusUpdate += OnBackgroundRemoverStatusUpdate;
            backgroundRemover.OnBackgroundRemoved += OnBackgroundRemoved;
            backgroundRemover.OnProcessingProgress += OnProcessingProgress;
        }
        
        if (pipeline != null)
        {
            pipeline.OnStatusUpdate += OnPipelineStatusUpdate;
            pipeline.OnCharacterCreated += OnCharacterCreated;
            pipeline.OnProcessingProgress += OnPipelineProcessingProgress;
        }
        
        if (characterManager != null)
        {
            characterManager.OnCharacterAdded += OnCharacterAdded;
            characterManager.OnCharacterRemoved += OnCharacterRemoved;
            characterManager.OnCharacterActivated += OnCharacterActivated;
            characterManager.OnCharacterDeactivated += OnCharacterDeactivated;
            characterManager.OnStatusUpdate += OnManagerStatusUpdate;
            characterManager.OnPerformanceUpdate += OnPerformanceUpdate;
        }
        
        if (positioning != null)
        {
            positioning.OnStatusUpdate += OnPositioningStatusUpdate;
            positioning.OnSpawnPositionAssigned += OnSpawnPositionAssigned;
            positioning.OnAreaExpanded += OnAreaExpanded;
        }
    }
    
    // Event Handlers
    private void OnCameraStatusUpdate(string status)
    {
        UpdateCameraStatus(status);
    }
    
    private void OnFrameCaptured(Texture2D texture)
    {
        if (cameraFeed != null && texture != null)
        {
            cameraFeed.texture = texture;
        }
        
        if (cameraResolutionText != null)
        {
            cameraResolutionText.text = $"{texture.width}x{texture.height}";
        }
    }
    
    private void OnCaptureStarted()
    {
        UpdateCameraStatus("Camera started");
    }
    
    private void OnCaptureStopped()
    {
        UpdateCameraStatus("Camera stopped");
    }
    
    private void OnBackgroundRemoverStatusUpdate(string status)
    {
        UpdateSystemStatus($"Background: {status}");
    }
    
    private void OnBackgroundRemoved(Texture2D processedTexture)
    {
        // Could show processed preview here
    }
    
    private void OnProcessingProgress(float progress)
    {
        // Update processing progress bar if available
    }
    
    private void OnPipelineStatusUpdate(string status)
    {
        UpdateSystemStatus($"Pipeline: {status}");
    }
    
    private void OnCharacterCreated(WebcamCharacterPipeline.GeneratedCharacter character)
    {
        UpdateCharacterList();
        LogMessage($"Character created: {character.characterName}");
    }
    
    private void OnCharacterAdded(MultipleCharacterManager.ManagedCharacter character)
    {
        UpdateCharacterList();
        LogMessage($"Character added: {character.characterName}");
    }
    
    private void OnCharacterRemoved(string characterId)
    {
        UpdateCharacterList();
        LogMessage($"Character removed: {characterId}");
    }
    
    private void OnCharacterActivated(string characterId)
    {
        UpdateCharacterList();
    }
    
    private void OnCharacterDeactivated(string characterId)
    {
        UpdateCharacterList();
    }
    
    private void OnManagerStatusUpdate(string status)
    {
        UpdateSystemStatus($"Manager: {status}");
    }
    
    private void OnPerformanceUpdate(float frameTime)
    {
        performanceHistory.Add(frameTime);
        if (performanceHistory.Count > 100)
        {
            performanceHistory.RemoveAt(0);
        }
    }
    
    private void OnPositioningStatusUpdate(string status)
    {
        UpdateSystemStatus($"Positioning: {status}");
    }
    
    private void OnSpawnPositionAssigned(Vector3 position)
    {
        // Could update positioning overlay
    }
    
    private void OnAreaExpanded()
    {
        UpdatePositioningDisplay();
        LogMessage("Character area expanded");
    }
    
    private void OnPipelineProcessingProgress(float progress)
    {
        // Update pipeline progress
    }
    
    // Button Actions
    private void InitializeCamera()
    {
        if (webcamCapture != null)
        {
            webcamCapture.InitializeWebcam();
        }
    }
    
    private void StartCapture()
    {
        if (webcamCapture != null && !webcamCapture.IsCapturing)
        {
            webcamCapture.InitializeWebcam();
        }
    }
    
    private void StopCapture()
    {
        if (webcamCapture != null)
        {
            webcamCapture.StopWebcam();
        }
    }
    
    private void CreateCharacter()
    {
        if (webcamCapture != null && webcamCapture.IsCapturing)
        {
            Texture2D capturedFrame = webcamCapture.CaptureFrame();
            if (capturedFrame != null && pipeline != null)
            {
                pipeline.CreateCharacterManually(capturedFrame);
            }
        }
    }
    
    private void ClearAllCharacters()
    {
        if (characterManager != null)
        {
            characterManager.ClearAllCharacters();
            UpdateCharacterList();
            LogMessage("All characters cleared");
        }
    }
    
    private void RunDemo()
    {
        if (!isDemoRunning)
        {
            StartCoroutine(DemoCoroutine());
        }
    }
    
    private void ShowStatistics()
    {
        // Show detailed statistics
        string stats = GetDetailedStatistics();
        if (systemStatsText != null)
        {
            systemStatsText.text = stats;
        }
    }
    
    private void ResetSystem()
    {
        // Reset all components
        if (webcamCapture != null) webcamCapture.StopWebcam();
        if (characterManager != null) characterManager.ClearAllCharacters();
        if (positioning != null) positioning.ResetPositioningSystem();
        
        UpdateAllDisplays();
        LogMessage("System reset completed");
    }
    
    // Tab Navigation
    private void ShowTab(string tabName)
    {
        currentTab = tabName;
        HideAllPanels();
        
        switch (tabName.ToLower())
        {
            case "camera":
                ShowPanel(cameraPanel);
                UpdateCameraDisplay();
                break;
            case "characters":
                ShowPanel(characterPanel);
                UpdateCharacterList();
                break;
            case "positioning":
                ShowPanel(positioningPanel);
                UpdatePositioningDisplay();
                break;
            case "system":
                ShowPanel(systemPanel);
                UpdateSystemDisplay();
                break;
            case "demo":
                ShowPanel(demoPanel);
                break;
            default:
                ShowDashboard();
                break;
        }
    }
    
    private void ShowDashboard()
    {
        currentTab = "dashboard";
        HideAllPanels();
        ShowPanel(mainDashboard);
    }
    
    // Display Updates
    private void UpdateDashboard()
    {
        UpdateSystemStatus();
        UpdateCharacterCount();
        UpdatePerformanceDisplay();
        UpdateFPSDisplay();
        UpdateSystemHealth();
    }
    
    private void UpdateSystemStatus(string status = null)
    {
        if (systemStatusText != null)
        {
            string currentStatus = status ?? GetSystemStatus();
            systemStatusText.text = $"System: {currentStatus}";
        }
    }
    
    private void UpdateCameraStatus(string status)
    {
        if (cameraStatusText != null)
        {
            cameraStatusText.text = status;
        }
    }
    
    private void UpdateCharacterCount()
    {
        if (characterCountText != null && characterManager != null)
        {
            characterCountText.text = $"Characters: {characterManager.ActiveCharacterCount}/{characterManager.TotalCharacterCount}";
        }
    }
    
    private void UpdatePerformanceDisplay()
    {
        if (performanceText != null && characterManager != null)
        {
            performanceText.text = $"Frame Time: {characterManager.AverageFrameTime:F3}s";
        }
    }
    
    private void UpdateFPSDisplay()
    {
        if (fpsText != null)
        {
            fpsText.text = $"FPS: {currentFPS:F1}";
        }
    }
    
    private void UpdateSystemHealth()
    {
        if (systemHealthBar != null && characterManager != null)
        {
            float health = Mathf.Clamp01(1f - (characterManager.AverageFrameTime / 0.1f));
            systemHealthBar.fillAmount = health;
            
            // Update color based on health
            if (health > 0.7f)
            {
                systemHealthBar.color = Color.green;
            }
            else if (health > 0.3f)
            {
                systemHealthBar.color = Color.yellow;
            }
            else
            {
                systemHealthBar.color = Color.red;
            }
        }
    }
    
    private void UpdateCameraDisplay()
    {
        if (webcamCapture != null)
        {
            if (cameraFPSText != null)
            {
                cameraFPSText.text = webcamCapture.GetWebcamStats();
            }
        }
    }
    
    private void UpdateCharacterList()
    {
        if (characterListContent == null || characterManager == null) return;
        
        // Clear existing items
        foreach (Transform child in characterListContent)
        {
            Destroy(child.gameObject);
        }
        characterListItems.Clear();
        
        // Get active characters
        var activeCharacters = characterManager.GetActiveCharacters();
        
        foreach (var kvp in activeCharacters)
        {
            CreateCharacterListItem(kvp.Value);
        }
        
        // Update summary
        if (totalCharactersText != null)
        {
            totalCharactersText.text = $"Total: {characterManager.TotalCharacterCount}";
        }
        
        if (activeCharactersText != null)
        {
            activeCharactersText.text = $"Active: {characterManager.ActiveCharacterCount}";
        }
    }
    
    private void CreateCharacterListItem(MultipleCharacterManager.ManagedCharacter character)
    {
        if (characterListItemPrefab == null) return;
        
        GameObject item = Instantiate(characterListItemPrefab, characterListContent);
        UICharacterItem uiItem = new UICharacterItem();
        
        // Get UI components
        uiItem.nameText = item.transform.Find("NameText").GetComponent<Text>();
        uiItem.statusText = item.transform.Find("StatusText").GetComponent<Text>();
        uiItem.positionText = item.transform.Find("PositionText").GetComponent<Text>();
        uiItem.activateButton = item.transform.Find("ActivateButton").GetComponent<Button>();
        uiItem.deactivateButton = item.transform.Find("DeactivateButton").GetComponent<Button>();
        uiItem.removeButton = item.transform.Find("RemoveButton").GetComponent<Button>();
        uiItem.statusImage = item.transform.Find("StatusImage").GetComponent<Image>();
        
        // Set up item
        uiItem.characterId = character.characterId;
        uiItem.characterName = character.characterName;
        uiItem.gameObject = item;
        
        uiItem.nameText.text = character.characterName;
        uiItem.statusText.text = character.isActive ? "Active" : "Inactive";
        uiItem.positionText.text = $"({character.spawnPosition.x:F1}, {character.spawnPosition.y:F1})";
        
        uiItem.statusImage.color = character.isActive ? Color.green : Color.gray;
        
        // Set up button listeners
        uiItem.activateButton.onClick.AddListener(() => ActivateCharacter(character.characterId));
        uiItem.deactivateButton.onClick.AddListener(() => DeactivateCharacter(character.characterId));
        uiItem.removeButton.onClick.AddListener(() => RemoveCharacter(character.characterId));
        
        characterListItems.Add(uiItem);
    }
    
    private void UpdatePositioningDisplay()
    {
        if (positioning != null)
        {
            if (areaUtilizationText != null)
            {
                areaUtilizationText.text = $"Utilization: {positioning.AreaUtilization:P1}";
            }
            
            if (expansionLevelText != null)
            {
                expansionLevelText.text = $"Expansion: Level 0";
            }
            
            if (gridSizeText != null)
            {
                gridSizeText.text = $"Grid: Available slots";
            }
        }
    }
    
    private void UpdateSystemDisplay()
    {
        if (systemStatsText != null)
        {
            systemStatsText.text = GetSystemStatistics();
        }
        
        if (performanceMetricsText != null)
        {
            performanceMetricsText.text = GetPerformanceMetrics();
        }
    }
    
    // Slider Handlers
    private void OnColorToleranceChanged(float value)
    {
        if (colorToleranceValue != null)
        {
            colorToleranceValue.text = value.ToString("F2");
        }
        
        if (backgroundRemover != null)
        {
            backgroundRemover.UpdateProcessingSettings(value, backgroundThresholdSlider.value);
        }
    }
    
    private void OnBackgroundThresholdChanged(float value)
    {
        if (backgroundThresholdValue != null)
        {
            backgroundThresholdValue.text = value.ToString("F2");
        }
        
        if (backgroundRemover != null)
        {
            backgroundRemover.UpdateProcessingSettings(colorToleranceSlider.value, value);
        }
    }
    
    private void OnCharacterSpacingChanged(float value)
    {
        if (spacingValueText != null)
        {
            spacingValueText.text = value.ToString("F1");
        }
        
        if (positioning != null)
        {
            // Update positioning system
        }
    }
    
    private void OnLODDistanceChanged(float value)
    {
        if (lodValueText != null)
        {
            lodValueText.text = value.ToString("F0");
        }
        
        // Update LOD system
    }
    
    // Character Management
    private void ActivateCharacter(string characterId)
    {
        if (characterManager != null)
        {
            characterManager.ActivateCharacter(characterId);
        }
    }
    
    private void DeactivateCharacter(string characterId)
    {
        if (characterManager != null)
        {
            characterManager.DeactivateCharacter(characterId);
        }
    }
    
    private void RemoveCharacter(string characterId)
    {
        if (characterManager != null)
        {
            characterManager.RemoveCharacter(characterId);
        }
    }
    
    // Performance and System Health
    private void UpdateFPS()
    {
        if (Time.time - lastFpsUpdate > 1f)
        {
            currentFPS = 1f / Time.deltaTime;
            lastFpsUpdate = Time.time;
        }
    }
    
    private void UpdatePerformanceMetrics()
    {
        if (characterManager != null)
        {
            float avgFrameTime = characterManager.AverageFrameTime;
            if (performanceHistory.Count > 0)
            {
                avgFrameTime = performanceHistory.Average();
            }
            
            if (avgFrameTime > 0.05f) // 50ms
            {
                LogWarning($"Performance degradation detected: {avgFrameTime:F3}s frame time");
            }
        }
    }
    
    private void CheckSystemHealth()
    {
        if (Time.time - lastPerformanceCheck > 10f)
        {
            lastPerformanceCheck = Time.time;
            
            if (characterManager != null && characterManager.AverageFrameTime > 0.1f)
            {
                LogWarning("System health check: Performance below optimal level");
            }
        }
    }
    
    // Utility Methods
    private void HideAllPanels()
    {
        if (mainDashboard != null) mainDashboard.SetActive(false);
        if (cameraPanel != null) cameraPanel.SetActive(false);
        if (characterPanel != null) characterPanel.SetActive(false);
        if (positioningPanel != null) positioningPanel.SetActive(false);
        if (systemPanel != null) systemPanel.SetActive(false);
        if (demoPanel != null) demoPanel.SetActive(false);
    }
    
    private void ShowPanel(GameObject panel)
    {
        if (panel != null)
        {
            panel.SetActive(true);
        }
    }
    
    private void UpdateAllDisplays()
    {
        UpdateDashboard();
        UpdateCameraDisplay();
        UpdateCharacterList();
        UpdatePositioningDisplay();
        UpdateSystemDisplay();
    }
    
    private string GetSystemStatus()
    {
        if (characterManager == null) return "Not Initialized";
        
        return characterManager.ManagerStatus;
    }
    
    private string GetSystemStatistics()
    {
        string stats = "Webcam System Statistics:\n\n";
        
        if (webcamCapture != null)
        {
            stats += "Camera:\n";
            stats += webcamCapture.GetWebcamStats() + "\n\n";
        }
        
        if (characterManager != null)
        {
            stats += "Characters:\n";
            stats += characterManager.GetCharacterStatistics() + "\n\n";
        }
        
        if (positioning != null)
        {
            stats += "Positioning:\n";
            stats += positioning.GetPositioningStatistics() + "\n\n";
        }
        
        if (backgroundRemover != null)
        {
            stats += "Background Removal:\n";
            stats += backgroundRemover.GetProcessingStats() + "\n\n";
        }
        
        return stats;
    }
    
    private string GetPerformanceMetrics()
    {
        string metrics = "Performance Metrics:\n\n";
        metrics += $"Current FPS: {currentFPS:F1}\n";
        
        if (characterManager != null)
        {
            metrics += $"Average Frame Time: {characterManager.AverageFrameTime:F3}s\n";
        }
        
        if (performanceHistory.Count > 0)
        {
            float min = performanceHistory.Min();
            float max = performanceHistory.Max();
            float avg = performanceHistory.Average();
            
            metrics += $"Frame Time Range: {min:F3}s - {max:F3}s\n";
            metrics += $"Average (Last 100): {avg:F3}s\n";
        }
        
        return metrics;
    }
    
    private string GetDetailedStatistics()
    {
        return GetSystemStatistics() + "\n" + GetPerformanceMetrics() + GetErrorLog();
    }
    
    private void LogMessage(string message)
    {
        string timestamp = System.DateTime.Now.ToString("HH:mm:ss");
        string logEntry = $"[{timestamp}] {message}";
        errorLog.Add(logEntry);
        
        if (errorLog.Count > maxErrorLogEntries)
        {
            errorLog.RemoveAt(0);
        }
        
        UpdateErrorLogDisplay();
    }
    
    private void LogWarning(string message)
    {
        LogMessage($"WARNING: {message}");
    }
    
    private void LogError(string message)
    {
        LogMessage($"ERROR: {message}");
    }
    
    private string GetErrorLog()
    {
        if (errorLog.Count == 0) return "\nNo errors logged.";
        
        return "\nRecent Activity:\n" + string.Join("\n", errorLog.TakeLast(10));
    }
    
    private void UpdateErrorLogDisplay()
    {
        if (errorLogText != null)
        {
            errorLogText.text = GetErrorLog();
        }
        
        // Auto-scroll to bottom
        if (errorLogScroll != null)
        {
            StartCoroutine(ScrollToBottom());
        }
    }
    
    private IEnumerator ScrollToBottom()
    {
        yield return new WaitForEndOfFrame();
        if (errorLogScroll != null)
        {
            errorLogScroll.verticalNormalizedPosition = 0f;
        }
    }
    
    // Demo System
    private IEnumerator DemoCoroutine()
    {
        isDemoRunning = true;
        LogMessage("Starting demo sequence...");
        
        ShowTab("demo");
        
        // Step 1: Initialize camera
        UpdateSystemStatus("Demo: Initializing camera...");
        if (webcamCapture != null)
        {
            webcamCapture.InitializeWebcam();
        }
        yield return new WaitForSeconds(3f);
        
        // Step 2: Capture and create character
        UpdateSystemStatus("Demo: Capturing character...");
        if (webcamCapture != null && webcamCapture.IsCapturing)
        {
            Texture2D frame = webcamCapture.CaptureFrame();
            if (frame != null && pipeline != null)
            {
                pipeline.CreateCharacterManually(frame, "Demo_Character");
            }
        }
        yield return new WaitForSeconds(2f);
        
        // Step 3: Show character controls
        UpdateSystemStatus("Demo: Character created");
        yield return new WaitForSeconds(2f);
        
        // Step 4: Show system statistics
        ShowStatistics();
        yield return new WaitForSeconds(3f);
        
        // Step 5: Complete
        UpdateSystemStatus("Demo completed");
        LogMessage("Demo sequence completed");
        isDemoRunning = false;
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from all events
        if (webcamCapture != null)
        {
            webcamCapture.OnStatusUpdate -= OnCameraStatusUpdate;
            webcamCapture.OnFrameCaptured -= OnFrameCaptured;
            webcamCapture.OnCaptureStarted -= OnCaptureStarted;
            webcamCapture.OnCaptureStopped -= OnCaptureStopped;
        }
        
        if (backgroundRemover != null)
        {
            backgroundRemover.OnStatusUpdate -= OnBackgroundRemoverStatusUpdate;
            backgroundRemover.OnBackgroundRemoved -= OnBackgroundRemoved;
            backgroundRemover.OnProcessingProgress -= OnProcessingProgress;
        }
        
        if (pipeline != null)
        {
            pipeline.OnStatusUpdate -= OnPipelineStatusUpdate;
            pipeline.OnCharacterCreated -= OnCharacterCreated;
            pipeline.OnProcessingProgress -= OnPipelineProcessingProgress;
        }
        
        if (characterManager != null)
        {
            characterManager.OnCharacterAdded -= OnCharacterAdded;
            characterManager.OnCharacterRemoved -= OnCharacterRemoved;
            characterManager.OnCharacterActivated -= OnCharacterActivated;
            characterManager.OnCharacterDeactivated -= OnCharacterDeactivated;
            characterManager.OnStatusUpdate -= OnManagerStatusUpdate;
            characterManager.OnPerformanceUpdate -= OnPerformanceUpdate;
        }
        
        if (positioning != null)
        {
            positioning.OnStatusUpdate -= OnPositioningStatusUpdate;
            positioning.OnSpawnPositionAssigned -= OnSpawnPositionAssigned;
            positioning.OnAreaExpanded -= OnAreaExpanded;
        }
    }
}
