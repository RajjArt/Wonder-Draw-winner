using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Background Remover - Phase 1: White background removal for character images
/// Processes webcam captures to remove white/light backgrounds and create transparent PNGs
/// Integrates with WebcamCapture and WebcamCharacterPipeline
/// </summary>
public class BackgroundRemover : MonoBehaviour
{
    [Header("Background Detection Settings")]
    [SerializeField] private Color backgroundColor = Color.white;
    [SerializeField] private float colorTolerance = 0.15f; // How close to white to consider as background
    [SerializeField] private float edgeFeathering = 0.1f; // Smooth edges percentage
    [SerializeField] private bool enableEdgeSmoothing = true;
    [SerializeField] private bool enableColorRefinement = true;
    
    [Header("Processing Settings")]
    [SerializeField] private int blurRadius = 2; // For edge smoothing
    [SerializeField] private float backgroundThreshold = 0.8f; // Alpha threshold for background detection
    [SerializeField] private bool enableMultiPassProcessing = true;
    [SerializeField] private int processingPasses = 2;
    
    [Header("Integration")]
    [SerializeField] private WebcamCapture webcamCapture;
    [SerializeField] private WebcamCharacterPipeline pipeline;
    
    // Processing statistics
    private int totalProcessedFrames = 0;
    private float averageProcessingTime = 0f;
    private bool isProcessing = false;
    
    // Event system
    public System.Action<Texture2D> OnBackgroundRemoved;
    public System.Action<float> OnProcessingProgress;
    public System.Action<string> OnStatusUpdate;
    public System.Action OnProcessingStarted;
    public System.Action OnProcessingCompleted;
    
    // Properties
    public bool IsProcessing => isProcessing;
    public int TotalProcessedFrames => totalProcessedFrames;
    public float AverageProcessingTime => averageProcessingTime;
    public string ProcessingStatus => GetCurrentStatus();
    
    private string GetCurrentStatus()
    {
        if (isProcessing) return $"Processing... ({totalProcessedFrames} frames)";
        return $"Ready - {totalProcessedFrames} frames processed";
    }
    
    private void Awake()
    {
        // Auto-find components if not assigned
        if (webcamCapture == null)
            webcamCapture = FindFirstObjectByType<WebcamCapture>();
        if (pipeline == null)
            pipeline = FindFirstObjectByType<WebcamCharacterPipeline>();
        
        // Subscribe to webcam capture events
        if (webcamCapture != null)
        {
            webcamCapture.OnFrameCaptured += OnFrameCaptured;
        }
    }
    
    private void Start()
    {
        UpdateStatusUI("Background remover ready");
    }
    
    /// <summary>
    /// Process a single frame to remove white background
    /// </summary>
    public Texture2D ProcessFrame(Texture2D inputTexture)
    {
        if (inputTexture == null)
        {
            Debug.LogError("Cannot process null texture");
            return null;
        }
        
        float startTime = Time.time;
        isProcessing = true;
        OnProcessingStarted?.Invoke();
        
        try
        {
            Debug.Log($"🔄 Starting background removal for {inputTexture.width}x{inputTexture.height} texture");
            UpdateStatusUI("Processing background removal...");
            
            // Create output texture with same dimensions
            Texture2D outputTexture = new Texture2D(inputTexture.width, inputTexture.height, TextureFormat.RGBA32, false);
            
            // Get pixel data
            Color[] inputPixels = inputTexture.GetPixels();
            Color[] outputPixels = new Color[inputPixels.Length];
            
            // Process pixels
            ProcessPixels(inputPixels, outputPixels);
            
            // Apply edge smoothing if enabled
            if (enableEdgeSmoothing)
            {
                ApplyEdgeSmoothing(outputPixels, inputTexture.width, inputTexture.height);
            }
            
            // Multi-pass processing for better results
            if (enableMultiPassProcessing)
            {
                for (int pass = 1; pass < processingPasses; pass++)
                {
                    ApplyRefinementPass(outputPixels, inputTexture.width, inputTexture.height);
                    OnProcessingProgress?.Invoke((float)pass / processingPasses);
                }
            }
            
            // Apply to output texture
            outputTexture.SetPixels(outputPixels);
            outputTexture.Apply();
            
            // Update statistics
            totalProcessedFrames++;
            float processingTime = Time.time - startTime;
            UpdateAverageProcessingTime(processingTime);
            
            Debug.Log($"✅ Background removal completed in {processingTime:F2}s");
            UpdateStatusUI($"Completed - {processingTime:F2}s");
            
            // Notify listeners
            OnBackgroundRemoved?.Invoke(outputTexture);
            OnProcessingCompleted?.Invoke();
            
            isProcessing = false;
            return outputTexture;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Background removal failed: {e.Message}");
            UpdateStatusUI($"Error: {e.Message}");
            isProcessing = false;
            return inputTexture; // Return original on error
        }
    }
    
    /// <summary>
    /// Event handler for frame captured
    /// </summary>
    private void OnFrameCaptured(Texture2D texture)
    {
        if (texture != null)
        {
            ProcessFrame(texture);
        }
    }
    
    /// <summary>
    /// Process individual pixels to detect and remove background
    /// </summary>
    private void ProcessPixels(Color[] inputPixels, Color[] outputPixels)
    {
        for (int i = 0; i < inputPixels.Length; i++)
        {
            Color inputColor = inputPixels[i];
            Color outputColor = inputColor;
            
            // Calculate color distance from background color
            float colorDistance = ColorDistance(inputColor, backgroundColor);
            
            // Determine if this pixel is background
            bool isBackground = colorDistance <= colorTolerance;
            
            if (isBackground)
            {
                // Make background transparent
                outputColor.a = 0f;
            }
            else
            {
                // Ensure foreground is opaque
                outputColor.a = 1f;
                
                // Color refinement for better edge detection
                if (enableColorRefinement)
                {
                    outputColor = RefineForegroundColor(outputColor, inputColor, colorDistance);
                }
            }
            
            outputPixels[i] = outputColor;
        }
    }
    
    /// <summary>
    /// Calculate distance between two colors (HSV-based for better accuracy)
    /// </summary>
    private float ColorDistance(Color color1, Color color2)
    {
        // Convert to HSV for better color distance calculation
        Color.RGBToHSV(color1, out float h1, out float s1, out float v1);
        Color.RGBToHSV(color2, out float h2, out float s2, out float v2);
        
        // Distance in HSV space
        float hDist = Mathf.Min(Mathf.Abs(h1 - h2), 1f - Mathf.Abs(h1 - h2));
        float sDist = Mathf.Abs(s1 - s2);
        float vDist = Mathf.Abs(v1 - v2);
        
        // Weighted distance (hue is more important for similar colors)
        return Mathf.Sqrt(hDist * hDist * 0.6f + sDist * sDist * 0.2f + vDist * vDist * 0.2f);
    }
    
    /// <summary>
    /// Apply edge smoothing to improve transition between foreground and background
    /// </summary>
    private void ApplyEdgeSmoothing(Color[] pixels, int width, int height)
    {
        // Create a temporary buffer for smooth transitions
        Color[] smoothedPixels = new Color[pixels.Length];
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                Color centerColor = pixels[index];
                
                // If center is background, check neighbors for edge detection
                if (centerColor.a < 0.1f)
                {
                    float edgeStrength = 0f;
                    int neighborCount = 0;
                    
                    // Check surrounding pixels
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            if (dx == 0 && dy == 0) continue;
                            
                            int nx = x + dx;
                            int ny = y + dy;
                            
                            if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                            {
                                int neighborIndex = ny * width + nx;
                                edgeStrength += pixels[neighborIndex].a;
                                neighborCount++;
                            }
                        }
                    }
                    
                    // If surrounded by foreground, make partially transparent (edge)
                    if (neighborCount > 0)
                    {
                        float averageEdge = edgeStrength / neighborCount;
                        if (averageEdge > backgroundThreshold)
                        {
                            centerColor.a = averageEdge * edgeFeathering;
                        }
                    }
                }
                
                smoothedPixels[index] = centerColor;
            }
        }
        
        // Copy smoothed pixels back
        System.Array.Copy(smoothedPixels, pixels, pixels.Length);
    }
    
    /// <summary>
    /// Refine foreground colors for better separation
    /// </summary>
    private Color RefineForegroundColor(Color outputColor, Color inputColor, float colorDistance)
    {
        // If color is very close to background, it might be shadow or edge
        if (colorDistance < colorTolerance * 1.5f)
        {
            // Reduce alpha for borderline colors
            outputColor.a = Mathf.Lerp(0f, 1f, (colorDistance - colorTolerance) / (colorTolerance * 0.5f));
        }
        
        return outputColor;
    }
    
    /// <summary>
    /// Apply refinement pass to improve background removal
    /// </summary>
    private void ApplyRefinementPass(Color[] pixels, int width, int height)
    {
        Color[] refinedPixels = new Color[pixels.Length];
        
        for (int i = 0; i < pixels.Length; i++)
        {
            int x = i % width;
            int y = i / width;
            
            Color currentColor = pixels[i];
            
            // If this is a foreground pixel, check if it's actually background
            if (currentColor.a > 0.5f)
            {
                // Check surrounding pixels for background patterns
                int backgroundNeighbors = 0;
                int totalNeighbors = 0;
                
                for (int dy = -blurRadius; dy <= blurRadius; dy++)
                {
                    for (int dx = -blurRadius; dx <= blurRadius; dx++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        
                        int nx = x + dx;
                        int ny = y + dy;
                        
                        if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                        {
                            totalNeighbors++;
                            int neighborIndex = ny * width + nx;
                            if (pixels[neighborIndex].a < 0.1f)
                            {
                                backgroundNeighbors++;
                            }
                        }
                    }
                }
                
                // If surrounded by background, reduce alpha
                if (totalNeighbors > 0 && (float)backgroundNeighbors / totalNeighbors > 0.7f)
                {
                    currentColor.a *= 0.3f; // Reduce opacity for edge pixels
                }
            }
            
            refinedPixels[i] = currentColor;
        }
        
        System.Array.Copy(refinedPixels, pixels, pixels.Length);
    }
    
    /// <summary>
    /// Process a batch of frames for bulk operations
    /// </summary>
    public Texture2D[] ProcessBatch(Texture2D[] inputFrames)
    {
        if (inputFrames == null || inputFrames.Length == 0)
        {
            Debug.LogWarning("No frames to process");
            return null;
        }
        
        Texture2D[] processedFrames = new Texture2D[inputFrames.Length];
        
        for (int i = 0; i < inputFrames.Length; i++)
        {
            OnProcessingProgress?.Invoke((float)i / inputFrames.Length);
            processedFrames[i] = ProcessFrame(inputFrames[i]);
        }
        
        OnProcessingProgress?.Invoke(1f);
        return processedFrames;
    }
    
    /// <summary>
    /// Update average processing time
    /// </summary>
    private void UpdateAverageProcessingTime(float latestTime)
    {
        // Simple moving average
        averageProcessingTime = (averageProcessingTime * (totalProcessedFrames - 1) + latestTime) / totalProcessedFrames;
    }
    
    private void UpdateStatusUI(string message)
    {
        OnStatusUpdate?.Invoke(message);
        Debug.Log($"🎨 Background Remover: {message}");
    }
    
    /// <summary>
    /// Get processing statistics
    /// </summary>
    public string GetProcessingStats()
    {
        return $"Processed Frames: {totalProcessedFrames}\n" +
               $"Average Time: {averageProcessingTime:F2}s\n" +
               $"Status: {(isProcessing ? "Processing" : "Ready")}\n" +
               $"Color Tolerance: {colorTolerance:F2}\n" +
               $"Background: RGB({Mathf.RoundToInt(backgroundColor.r * 255)}, {Mathf.RoundToInt(backgroundColor.g * 255)}, {Mathf.RoundToInt(backgroundColor.b * 255)})";
    }
    
    /// <summary>
    /// Adjust processing parameters in real-time
    /// </summary>
    public void UpdateProcessingSettings(float newTolerance, float newThreshold)
    {
        colorTolerance = Mathf.Clamp01(newTolerance);
        backgroundThreshold = Mathf.Clamp01(newThreshold);
        
        Debug.Log($"Updated settings: Tolerance={colorTolerance:F2}, Threshold={backgroundThreshold:F2}");
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from webcam events
        if (webcamCapture != null)
        {
            webcamCapture.OnFrameCaptured -= OnFrameCaptured;
        }
    }
}
