using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simple test controller for WebcamCapture and BackgroundRemover
/// This demonstrates basic setup and testing of the webcam system
/// </summary>
public class WebcamTestController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WebcamCapture webcamCapture;
    [SerializeField] private BackgroundRemover backgroundRemover;
    [SerializeField] private RawImage processedDisplay;
    
    [Header("UI References")]
    [SerializeField] private Button startWebcamButton;
    [SerializeField] private Button captureButton;
    [SerializeField] private Text statusText;
    
    private void Awake()
    {
        // Auto-find components if not assigned
        if (webcamCapture == null)
            webcamCapture = FindFirstObjectByType<WebcamCapture>();
        if (backgroundRemover == null)
            backgroundRemover = FindFirstObjectByType<BackgroundRemover>();
        
        // Subscribe to events
        if (webcamCapture != null)
        {
            webcamCapture.OnStatusUpdate += OnWebcamStatusUpdate;
            webcamCapture.OnFrameCaptured += OnFrameCaptured;
        }
        
        if (backgroundRemover != null)
        {
            backgroundRemover.OnStatusUpdate += OnBackgroundStatusUpdate;
            backgroundRemover.OnBackgroundRemoved += OnBackgroundRemoved;
        }
    }
    
    private void Start()
    {
        // Setup button listeners
        if (startWebcamButton != null)
            startWebcamButton.onClick.AddListener(StartWebcam);
        if (captureButton != null)
            captureButton.onClick.AddListener(CaptureFrame);
        
        // Initial button states
        UpdateButtonStates();
        
        UpdateStatus("Ready to test webcam system");
    }
    
    private void OnDestroy()
    {
        // Clean up event subscriptions
        if (webcamCapture != null)
        {
            webcamCapture.OnStatusUpdate -= OnWebcamStatusUpdate;
            webcamCapture.OnFrameCaptured -= OnFrameCaptured;
        }
        
        if (backgroundRemover != null)
        {
            backgroundRemover.OnStatusUpdate -= OnBackgroundStatusUpdate;
            backgroundRemover.OnBackgroundRemoved -= OnBackgroundRemoved;
        }
    }
    
    public void StartWebcam()
    {
        if (webcamCapture != null && !webcamCapture.IsInitialized)
        {
            UpdateStatus("Starting webcam...");
            webcamCapture.InitializeWebcam();
        }
        else if (webcamCapture != null && webcamCapture.IsInitialized)
        {
            UpdateStatus("Stopping webcam...");
            webcamCapture.StopWebcam();
        }
        
        UpdateButtonStates();
    }
    
    public void CaptureFrame()
    {
        if (webcamCapture != null && webcamCapture.IsCapturing)
        {
            UpdateStatus("Capturing frame...");
            Texture2D capturedFrame = webcamCapture.CaptureFrame();
            
            if (capturedFrame != null)
            {
                UpdateStatus($"Frame captured: {capturedFrame.width}x{capturedFrame.height}");
            }
            else
            {
                UpdateStatus("Failed to capture frame");
            }
        }
        else
        {
            UpdateStatus("Webcam not ready");
        }
    }
    
    private void OnWebcamStatusUpdate(string message)
    {
        UpdateStatus($"Webcam: {message}");
        UpdateButtonStates();
    }
    
    private void OnBackgroundStatusUpdate(string message)
    {
        UpdateStatus($"Background: {message}");
    }
    
    private void OnFrameCaptured(Texture2D frame)
    {
        if (backgroundRemover != null)
        {
            UpdateStatus("Processing background removal...");
            Texture2D processedFrame = backgroundRemover.ProcessFrame(frame);
            
            if (processedDisplay != null && processedFrame != null)
            {
                processedDisplay.texture = processedFrame;
                UpdateStatus("Background removal completed!");
            }
        }
    }
    
    private void OnBackgroundRemoved(Texture2D processedFrame)
    {
        if (processedDisplay != null && processedFrame != null)
        {
            processedDisplay.texture = processedFrame;
            UpdateStatus("Background removal completed!");
        }
    }
    
    private void UpdateStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
        
        Debug.Log($"[WebcamTestController] {message}");
    }
    
    private void UpdateButtonStates()
    {
        bool webcamReady = webcamCapture != null && webcamCapture.IsInitialized && webcamCapture.IsCapturing;
        
        if (startWebcamButton != null)
        {
            startWebcamButton.GetComponentInChildren<Text>().text = webcamReady ? "Stop Webcam" : "Start Webcam";
        }
        
        if (captureButton != null)
        {
            captureButton.interactable = webcamReady;
        }
    }
}
