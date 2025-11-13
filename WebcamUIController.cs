using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Webcam UI Controller - Phase 1: User interface for webcam character creation
/// Provides controls for capture, background removal, and character management
/// Integrates with WebcamCapture, BackgroundRemover, and WebcamCharacterPipeline
/// </summary>
public class WebcamUIController : MonoBehaviour
{
    [Header("UI Panel References")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject statusPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject previewPanel;
    
    [Header("Control Buttons")]
    [SerializeField] private Button initializeButton;
    [SerializeField] private Button captureButton;
    [SerializeField] private Button stopCameraButton;
    [SerializeField] private Button createCharacterButton;
    [SerializeField] private Button clearCharactersButton;
    [SerializeField] private Button showSettingsButton;
    [SerializeField] private Button hideSettingsButton;
    
    [Header("Display Elements")]
    [SerializeField] private Text statusText;
    [SerializeField] private Text statsText;
    [SerializeField] private Text processingText;
    [SerializeField] private Image processingBar;
    [SerializeField] private RawImage originalPreview;
    [SerializeField] private RawImage processedPreview;
    
    [Header("Settings Controls")]
    [SerializeField] private Slider colorToleranceSlider;
    [SerializeField] private Text colorToleranceValue;
    [SerializeField] private Slider backgroundThresholdSlider;
    [SerializeField] private Text backgroundThresholdValue;
    [SerializeField] private Toggle edgeSmoothingToggle;
    [SerializeField] private Toggle multiPassToggle;
    
    [Header("Integration")]
    [SerializeField] private WebcamCapture webcamCapture;
    [SerializeField] private BackgroundRemover backgroundRemover;
    [SerializeField] private WebcamCharacterPipeline pipeline;
    
    // UI state management
    private bool isInitialized = false;
    private Texture2D lastOriginalTexture;
    private Texture2D lastProcessedTexture;
    
    // Auto-refresh timer
    private float uiRefreshRate = 0.5f;
    private float lastUiUpdate = 0f;
    
    private void Awake()
    {
        // Auto-find components if not assigned
        if (webcamCapture == null)
            webcamCapture = FindFirstObjectByType<WebcamCapture>();
        if (backgroundRemover == null)
            backgroundRemover = FindFirstObjectByType<BackgroundRemover>();
        if (pipeline == null)
            pipeline = FindFirstObjectByType<WebcamCharacterPipeline>();
    }
    
    private void Start()
    {
        InitializeUI();
        SetupEventSubscriptions();
        UpdateUIState();
    }
    
    private void Update()
    {
        // Auto-refresh UI stats
        if (Time.time - lastUiUpdate > uiRefreshRate)
        {
            UpdateStatsDisplay();
            lastUiUpdate = Time.time;
        }
    }
    
    /// <summary>
    /// Initialize UI components and event listeners
    /// </summary>
    private void InitializeUI()
    {
        // Setup button listeners
        if (initializeButton != null)
            initializeButton.onClick.AddListener(InitializeWebcam);
        
        if (captureButton != null)
            captureButton.onClick.AddListener(CaptureFrame);
        
        if (stopCameraButton != null)
            stopCameraButton.onClick.AddListener(StopWebcam);
        
        if (createCharacterButton != null)
            createCharacterButton.onClick.AddListener(CreateCharacterFromPreview);
        
        if (clearCharactersButton != null)
            clearCharactersButton.onClick.AddListener(ClearAllCharacters);
        
        if (showSettingsButton != null)
            showSettingsButton.onClick.AddListener(ShowSettings);
        
        if (hideSettingsButton != null)
            hideSettingsButton.onClick.AddListener(HideSettings);
        
        // Setup settings controls
        if (colorToleranceSlider != null)
            colorToleranceSlider.onValueChanged.AddListener(OnColorToleranceChanged);
        
        if (backgroundThresholdSlider != null)
            backgroundThresholdSlider.onValueChanged.AddListener(OnBackgroundThresholdChanged);
        
        // Initialize settings values
        if (backgroundRemover != null)
        {
            colorToleranceSlider.value = backgroundRemover != null ? 0.15f : 0.5f;
            backgroundThresholdSlider.value = 0.8f;
        }
        
        // Hide settings panel initially
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
        
        Debug.Log("🎮 Webcam UI Controller initialized");
    }
    
    /// <summary>
    /// Setup event subscriptions with other components
    /// </summary>
    private void SetupEventSubscriptions()
    {
        if (webcamCapture != null)
        {
            webcamCapture.OnStatusUpdate += OnWebcamStatusUpdate;
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
    }
    
    /// <summary>
    /// Initialize webcam
    /// </summary>
    private void InitializeWebcam()
    {
        if (webcamCapture != null)
        {
            UpdateStatus("Initializing webcam...");
            webcamCapture.InitializeWebcam();
        }
        else
        {
            UpdateStatus("Webcam component not found");
        }
    }
    
    /// <summary>
    /// Capture current frame
    /// </summary>
    private void CaptureFrame()
    {
        if (webcamCapture != null && webcamCapture.IsCapturing)
        {
            UpdateStatus("Capturing frame...");
            
            // Show original preview
            if (originalPreview != null && webcamCapture.CurrentTexture != null)
            {
                originalPreview.texture = webcamCapture.CurrentTexture;
            }
        }
        else
        {
            UpdateStatus("Cannot capture - webcam not active");
        }
    }
    
    /// <summary>
    /// Stop webcam
    /// </summary>
    private void StopWebcam()
    {
        if (webcamCapture != null)
        {
            webcamCapture.StopWebcam();
            UpdateStatus("Webcam stopped");
        }
    }
    
    /// <summary>
    /// Create character from preview
    /// </summary>
    private void CreateCharacterFromPreview()
    {
        if (lastProcessedTexture != null && pipeline != null)
        {
            UpdateStatus("Creating character...");
            pipeline.CreateCharacterManually(lastProcessedTexture);
        }
        else
        {
            UpdateStatus("No processed image available for character creation");
        }
    }
    
    /// <summary>
    /// Clear all created characters
    /// </summary>
    private void ClearAllCharacters()
    {
        if (pipeline != null)
        {
            pipeline.ClearAllCharacters();
            UpdateStatus("All characters cleared");
        }
    }
    
    /// <summary>
    /// Show settings panel
    /// </summary>
    private void ShowSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
        }
    }
    
    /// <summary>
    /// Hide settings panel
    /// </summary>
    private void HideSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// Handle color tolerance setting change
    /// </summary>
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
    
    /// <summary>
    /// Handle background threshold setting change
    /// </summary>
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
    
    // Event Handlers
    private void OnWebcamStatusUpdate(string status)
    {
        UpdateStatus($"📹 {status}");
    }
    
    private void OnFrameCaptured(Texture2D texture)
    {
        lastOriginalTexture = texture;
        
        // Auto-process if background remover is available
        if (backgroundRemover != null)
        {
            StartCoroutine(ProcessTextureAsync(texture));
        }
    }
    
    private void OnCaptureStarted()
    {
        UpdateUIState();
    }
    
    private void OnCaptureStopped()
    {
        UpdateUIState();
    }
    
    private void OnBackgroundRemoverStatusUpdate(string status)
    {
        UpdateProcessingStatus($"🎨 {status}");
    }
    
    private void OnBackgroundRemoved(Texture2D processedTexture)
    {
        lastProcessedTexture = processedTexture;
        
        // Show processed preview
        if (processedPreview != null)
        {
            processedPreview.texture = processedTexture;
        }
        
        UpdateStatus("Background removal completed");
    }
    
    private void OnProcessingProgress(float progress)
    {
        UpdateProcessingProgress(progress);
    }
    
    private void OnPipelineStatusUpdate(string status)
    {
        UpdateStatus($"🎭 {status}");
    }
    
    private void OnCharacterCreated(WebcamCharacterPipeline.GeneratedCharacter character)
    {
        UpdateStatus($"Character created: {character.characterName}");
    }
    
    private void OnPipelineProcessingProgress(float progress)
    {
        UpdateProcessingProgress(progress);
    }
    
    /// <summary>
    /// Process texture asynchronously
    /// </summary>
    private IEnumerator ProcessTextureAsync(Texture2D texture)
    {
        if (backgroundRemover != null)
        {
            UpdateStatus("Processing background removal...");
            
            // Process in the background
            Texture2D processedTexture = backgroundRemover.ProcessFrame(texture);
            
            if (processedTexture != null)
            {
                yield return new WaitForEndOfFrame();
                OnBackgroundRemoved(processedTexture);
            }
        }
    }
    
    /// <summary>
    /// Update status display
    /// </summary>
    private void UpdateStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }
    
    /// <summary>
    /// Update processing status display
    /// </summary>
    private void UpdateProcessingStatus(string message)
    {
        if (processingText != null)
        {
            processingText.text = message;
        }
    }
    
    /// <summary>
    /// Update processing progress bar
    /// </summary>
    private void UpdateProcessingProgress(float progress)
    {
        if (processingBar != null)
        {
            processingBar.fillAmount = progress;
        }
    }
    
    /// <summary>
    /// Update statistics display
    /// </summary>
    private void UpdateStatsDisplay()
    {
        if (statsText == null) return;
        
        string stats = "System Status:\n";
        
        if (webcamCapture != null)
        {
            stats += $"📹 Camera: {webcamCapture.Status}\n";
        }
        
        if (backgroundRemover != null)
        {
            stats += $"🎨 Background: {backgroundRemover.ProcessingStatus}\n";
        }
        
        if (pipeline != null)
        {
            stats += $"🎭 Pipeline: {pipeline.PipelineStatus}\n";
        }
        
        statsText.text = stats;
    }
    
    /// <summary>
    /// Update button states and UI based on current status
    /// </summary>
    private void UpdateUIState()
    {
        bool webcamReady = webcamCapture != null && webcamCapture.IsInitialized;
        bool capturing = webcamCapture != null && webcamCapture.IsCapturing;
        bool processing = backgroundRemover != null && backgroundRemover.IsProcessing;
        
        // Update button states
        if (initializeButton != null)
            initializeButton.interactable = !webcamReady;
        
        if (captureButton != null)
            captureButton.interactable = capturing && !processing;
        
        if (stopCameraButton != null)
            stopCameraButton.interactable = webcamReady;
        
        if (createCharacterButton != null)
            createCharacterButton.interactable = lastProcessedTexture != null && !processing;
        
        // Update panel visibility
        if (statusPanel != null)
            statusPanel.SetActive(webcamReady);
        
        if (previewPanel != null)
            previewPanel.SetActive(lastOriginalTexture != null || lastProcessedTexture != null);
        
        isInitialized = webcamReady;
    }
    
    /// <summary>
    /// Show quick demo of the system
    /// </summary>
    public void ShowDemo()
    {
        if (webcamCapture == null)
        {
            UpdateStatus("Demo: Webcam not available");
            return;
        }
        
        StartCoroutine(DemoCoroutine());
    }
    
    private IEnumerator DemoCoroutine()
    {
        UpdateStatus("Starting demo...");
        
        // Initialize webcam
        webcamCapture.InitializeWebcam();
        
        // Wait for initialization
        yield return new WaitForSeconds(2f);
        
        if (webcamCapture.IsCapturing)
        {
            // Capture a frame
            CaptureFrame();
            yield return new WaitForSeconds(1f);
            
            // Show settings
            ShowSettings();
            yield return new WaitForSeconds(2f);
            
            HideSettings();
            UpdateStatus("Demo complete");
        }
        else
        {
            UpdateStatus("Demo: Webcam not available");
        }
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (webcamCapture != null)
        {
            webcamCapture.OnStatusUpdate -= OnWebcamStatusUpdate;
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
    }
}