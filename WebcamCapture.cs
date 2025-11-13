using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Linq;

/// <summary>
/// Webcam Capture System - Phase 1: Real-time webcam integration
/// Manages webcam initialization, capture, and display during gameplay
/// Compatible with the existing character creation pipeline
/// </summary>
public class WebcamCapture : MonoBehaviour
{
    [Header("Webcam Settings")]
    [SerializeField] private int cameraIndex = 0;
    [SerializeField] private int targetWidth = 1280;
    [SerializeField] private int targetHeight = 720;
    [SerializeField] private int frameRate = 30;
    [SerializeField] private bool startOnAwake = false;
    
    [Header("UI References")]
    [SerializeField] private RawImage webcamDisplay;
    [SerializeField] private GameObject loadingUI;
    [SerializeField] private Text statusText;
    
    [Header("Integration")]
    [SerializeField] private BackgroundRemover backgroundRemover;
    [SerializeField] private WebcamCharacterPipeline pipeline;
    
    // Core components
    private WebCamTexture webCamTexture;
    private bool isCapturing = false;
    private bool isInitialized = false;
    private Texture2D capturedTexture;
    private string currentStatus = "Not Initialized";
    
    // Event system for integration
    public System.Action<Texture2D> OnFrameCaptured;
    public System.Action<string> OnStatusUpdate;
    public System.Action OnCaptureStarted;
    public System.Action OnCaptureStopped;
    
    // Properties - Public getters for all required properties
    public bool IsCapturing => isCapturing;
    public bool IsInitialized => isInitialized;
    public Texture CurrentTexture => webCamTexture;
    public string Status => currentStatus;
    public int CaptureWidth => webCamTexture?.width ?? targetWidth;
    public int CaptureHeight => webCamTexture?.height ?? targetHeight;
    public int captureWidth => targetWidth; // Legacy compatibility
    public int captureHeight => targetHeight; // Legacy compatibility
    
    private string GetCurrentStatus()
    {
        if (!isInitialized) return "Not Initialized";
        if (isCapturing) return $"Capturing - Camera {cameraIndex}";
        return $"Ready - Camera {cameraIndex}";
    }
    
    private void Awake()
    {
        // Auto-find UI components if not assigned
        if (webcamDisplay == null)
            webcamDisplay = GetComponentInChildren<RawImage>();
        if (backgroundRemover == null)
            backgroundRemover = FindFirstObjectByType<BackgroundRemover>();
        if (pipeline == null)
            pipeline = FindFirstObjectByType<WebcamCharacterPipeline>();
    }
    
    private void Start()
    {
        if (startOnAwake)
        {
            InitializeWebcam();
        }
        UpdateStatusUI("Ready to initialize");
    }
    
    /// <summary>
    /// Initialize the webcam with specified settings
    /// </summary>
    public void InitializeWebcam()
    {
        StartCoroutine(InitializeWebcamCoroutine());
    }
    
    private IEnumerator InitializeWebcamCoroutine()
    {
        UpdateStatusUI("Initializing webcam...");
        ShowLoadingUI(true);
        
        try
        {
            // Get available cameras
            if (WebCamTexture.devices.Length == 0)
            {
                throw new System.Exception("No cameras found on device");
            }
            
            // Validate camera index
            if (cameraIndex >= WebCamTexture.devices.Length)
            {
                cameraIndex = 0;
                Debug.LogWarning($"Camera index {cameraIndex} not available, using index 0");
            }
            
            string deviceName = WebCamTexture.devices[cameraIndex].name;
            Debug.Log($"Initializing camera: {deviceName}");
            
            // Create WebCamTexture
            webCamTexture = new WebCamTexture(deviceName, targetWidth, targetHeight, frameRate);
            
            // Configure for optimal performance
            webCamTexture.wrapMode = TextureWrapMode.Clamp;
            webCamTexture.filterMode = FilterMode.Bilinear;
            
            // Start the webcam
            webCamTexture.Play();
            
            // Check if webcam started successfully
            if (webCamTexture.width == 0 || webCamTexture.height == 0)
            {
                throw new System.Exception("Webcam failed to start - invalid dimensions");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Webcam initialization failed: {e.Message}");
            UpdateStatusUI($"Error: {e.Message}");
            HideLoadingUI();
            
            // Fallback to demo mode
            isInitialized = false;
            CreateDemoTexture();
            yield break;
        }
        
        // Success path - only execute if no exception was thrown
        // Wait for webcam to initialize (outside try-catch)
        float timeout = 5f;
        float elapsed = 0f;
        
        while (!webCamTexture.didUpdateThisFrame && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            UpdateStatusUI($"Initializing... {Mathf.RoundToInt((timeout - elapsed) * 10f) / 10f}s");
            yield return null;
        }
        
        if (!webCamTexture.didUpdateThisFrame)
        {
            Debug.LogError($"❌ Webcam initialization timeout after {timeout}s");
            UpdateStatusUI("Webcam initialization timeout");
            HideLoadingUI();
            yield break;
        }
        
        // Continue initialization outside try-catch
        if (webCamTexture.width == 16 || webCamTexture.height == 16)
        {
            Debug.LogError("❌ Webcam returned default resolution (16x16), likely an error");
            UpdateStatusUI("Webcam error: Invalid resolution");
            HideLoadingUI();
            yield break;
        }
            
        // Success - complete initialization
        isInitialized = true;
        isCapturing = true;
        currentStatus = GetCurrentStatus();
        
        // Setup display
        if (webcamDisplay != null)
        {
            webcamDisplay.texture = webCamTexture;
            webcamDisplay.SetNativeSize();
        }
        
        // Update UI
        UpdateStatusUI($"Initialized: {webCamTexture.width}x{webCamTexture.height}");
        ShowLoadingUI(false);
        
        // Notify listeners
        OnCaptureStarted?.Invoke();
        
        Debug.Log($"✅ Webcam initialized successfully: {webCamTexture.width}x{webCamTexture.height}");
        yield break;
    }
    
    /// <summary>
    /// Create a demo texture for testing when webcam is unavailable
    /// </summary>
    private void CreateDemoTexture()
    {
        Debug.Log("Creating demo texture for testing...");
        
        capturedTexture = new Texture2D(targetWidth, targetHeight, TextureFormat.RGBA32, false);
        Color[] colors = new Color[targetWidth * targetHeight];
        
        // Create a simple gradient pattern for demo
        for (int y = 0; y < targetHeight; y++)
        {
            for (int x = 0; x < targetWidth; x++)
            {
                float t = Mathf.Lerp(0f, 1f, (x + y) / (targetWidth + targetHeight));
                colors[y * targetWidth + x] = new Color(t, 1-t, 0.5f, 1f);
            }
        }
        
        capturedTexture.SetPixels(colors);
        capturedTexture.Apply();
        
        if (webcamDisplay != null)
        {
            webcamDisplay.texture = capturedTexture;
        }
        
        isInitialized = true;
        UpdateStatusUI("Demo mode active (no webcam detected)");
    }
    
    /// <summary>
    /// Stop webcam capture
    /// </summary>
    public void StopWebcam()
    {
        if (webCamTexture != null)
        {
            webCamTexture.Stop();
            webCamTexture = null;
        }
        
        isCapturing = false;
        isInitialized = false;
        currentStatus = "Webcam stopped";
        UpdateStatusUI("Webcam stopped");
        OnCaptureStopped?.Invoke();
    }
    
    /// <summary>
    /// Stop capture (alias for StopWebcam for compatibility)
    /// </summary>
    public void StopCapture()
    {
        StopWebcam();
    }
    
    /// <summary>
    /// Capture current frame as Texture2D
    /// </summary>
    public Texture2D CaptureFrame()
    {
        if (!isCapturing || webCamTexture == null || !webCamTexture.didUpdateThisFrame)
        {
            Debug.LogWarning("Cannot capture frame - webcam not ready");
            return null;
        }
        
        try
        {
            // Create new texture for this frame
            capturedTexture = new Texture2D(webCamTexture.width, webCamTexture.height, TextureFormat.RGBA32, false);
            capturedTexture.SetPixels32(webCamTexture.GetPixels32());
            capturedTexture.Apply();
            
            // Notify listeners for processing
            OnFrameCaptured?.Invoke(capturedTexture);
            
            Debug.Log($"📷 Frame captured: {capturedTexture.width}x{capturedTexture.height}");
            return capturedTexture;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Frame capture failed: {e.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Capture multiple frames for processing
    /// </summary>
    public Texture2D[] CaptureFrames(int count)
    {
        Texture2D[] frames = new Texture2D[count];
        
        for (int i = 0; i < count; i++)
        {
            frames[i] = CaptureFrame();
            if (frames[i] == null) break;
            
            // Small delay between captures
            if (i < count - 1)
                System.Threading.Thread.Sleep(100);
        }
        
        return frames;
    }
    
    /// <summary>
    /// Get current webcam statistics
    /// </summary>
    public string GetWebcamStats()
    {
        if (!isInitialized) return "Not initialized";
        
        return $"Camera: {cameraIndex}\n" +
               $"Resolution: {webCamTexture.width}x{webCamTexture.height}\n" +
               $"Status: {(isCapturing ? "Capturing" : "Stopped")}\n" +
               $"Update Frame: {webCamTexture.didUpdateThisFrame}";
    }
    
    private void UpdateStatusUI(string message)
    {
        currentStatus = message;
        if (statusText != null)
        {
            statusText.text = message;
        }
        OnStatusUpdate?.Invoke(message);
        
        Debug.Log($"📋 Webcam Status: {message}");
    }
    
    private void ShowLoadingUI(bool show)
    {
        if (loadingUI != null)
        {
            loadingUI.SetActive(show);
        }
    }
    
    private void HideLoadingUI()
    {
        if (loadingUI != null)
        {
            loadingUI.SetActive(false);
        }
    }
    
    private void OnDestroy()
    {
        StopWebcam();
        
        if (capturedTexture != null)
        {
            Destroy(capturedTexture);
        }
    }
    
    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            // Pause webcam when app is paused
            if (isCapturing && webCamTexture != null)
            {
                webCamTexture.Pause();
            }
        }
        else
        {
            // Resume webcam when app is unpaused
            if (isCapturing && webCamTexture != null && !webCamTexture.isPlaying)
            {
                webCamTexture.Play();
            }
        }
    }
    
    /// <summary>
    /// Switch to a different camera (if multiple available)
    /// </summary>
    public void SwitchCamera(int newCameraIndex)
    {
        if (newCameraIndex == cameraIndex) return;
        
        cameraIndex = newCameraIndex;
        
        if (isCapturing)
        {
            StopWebcam();
            InitializeWebcam();
        }
    }
    
    /// <summary>
    /// Get list of available cameras
    /// </summary>
    public string[] GetAvailableCameras()
    {
        return WebCamTexture.devices.Select(device => device.name).ToArray();
    }
}