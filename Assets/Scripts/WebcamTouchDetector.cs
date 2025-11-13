using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

/// <summary>
/// Webcam-based touch detector that uses computer vision to detect touch/motion
/// and integrates with Unity's touch system
/// </summary>
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
public class WebcamTouchDetector : MonoBehaviour
{
    [Header("Webcam Settings")]
    [SerializeField] private int webcamIndex = 0;
    [SerializeField] private int requestedWidth = 640;
    [SerializeField] private int requestedHeight = 480;
    [SerializeField] private int targetFPS = 30;
    [SerializeField] private bool useFrontFacing = false;
    
    [Header("Detection Settings")]
    [SerializeField] private float motionThreshold = 0.1f;
    [SerializeField] private int minTouchArea = 50;
    [SerializeField] private int maxTouches = 5;
    [SerializeField] private float touchRadius = 10f;
    [SerializeField] private float updateRate = 0.05f; // 20 FPS for processing
    [SerializeField] private float maxTouchArea = 500f;
    [SerializeField] private Color detectionColor = Color.red;
    [SerializeField] private float detectionColorThreshold = 0.1f;
    
    [Header("Optimization")]
    [SerializeField] private bool useThreading = true;
    [SerializeField] private int processEveryNFrames = 2;
    [SerializeField] private bool showDebugTexture = false;
    
    [Header("Events")]
    public Action<List<TouchData>> OnTouchesDetected;
    public Action<int> OnTouchCountChanged;
    public Action<Vector2> OnSingleTouchDetected;
    public Action OnNoTouchesDetected;
    
    private WebCamTexture webcamTexture;
    private Texture2D debugTexture;
    private Color32[] previousFrame;
    private Color32[] currentFrame;
    private Color32[] processedFrame;
    
    private int frameCount = 0;
    private float lastUpdateTime = 0f;
    private bool isProcessing = false;
    private bool enableTouchDetection = true;
    private float touchSensitivity = 0.1f;
    private List<TouchData> currentTouches = new List<TouchData>();
    private List<Vector2> touchHistory = new List<Vector2>();
    
    // Threading
    private System.Threading.Thread processingThread;
    private volatile bool threadRunning = false;
    private readonly object frameLock = new object();
    
    // Material for rendering debug texture
    private Material debugMaterial;
    
    [Serializable]
    public class TouchData
    {
        public Vector2 position;
        public float intensity;
        public int area;
        public bool isActive;
        public float timestamp;
        
        public TouchData(Vector2 pos, float intens, int areaPixels, bool active = true)
        {
            this.position = pos;
            this.intensity = intens;
            this.area = areaPixels;
            this.isActive = active;
            this.timestamp = Time.time;
        }
    }
    
    private void Start()
    {
        InitializeWebcam();
        InitializeDebugTexture();
        InitializeDebugMaterial();
        
        // Start processing thread if enabled
        if (useThreading)
        {
            StartProcessingThread();
        }
    }
    
    private void InitializeWebcam()
    {
        try
        {
            // Get available webcams
            WebCamDevice[] devices = WebCamTexture.devices;
            
            if (devices.Length == 0)
            {
                Debug.LogError("No webcams found!");
                return;
            }
            
            // Select camera (front-facing if requested, otherwise first available)
            string deviceName = devices[0].name;
            if (useFrontFacing)
            {
                for (int i = 0; i < devices.Length; i++)
                {
                    if (devices[i].isFrontFacing)
                    {
                        deviceName = devices[i].name;
                        webcamIndex = i;
                        break;
                    }
                }
            }
            
            Debug.Log($"Using webcam: {deviceName} at index {webcamIndex}");
            
            // Create and configure webcam texture
            webcamTexture = new WebCamTexture(deviceName, requestedWidth, requestedHeight, targetFPS);
            webcamTexture.Play();
            
            // Wait for camera to initialize
            StartCoroutine(WaitForWebcamInit());
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to initialize webcam: {e.Message}");
        }
    }
    
    private IEnumerator WaitForWebcamInit()
    {
        yield return new WaitUntil(() => webcamTexture.width > 0 && webcamTexture.height > 0);
        
        Debug.Log($"Webcam initialized: {webcamTexture.width}x{webcamTexture.height} at {webcamTexture.requestedFPS} FPS");
        
        // Initialize frame buffers
        int pixelCount = webcamTexture.width * webcamTexture.height;
        previousFrame = new Color32[pixelCount];
        currentFrame = new Color32[pixelCount];
        processedFrame = new Color32[pixelCount];
        
        // Copy initial frame
        System.Array.Copy(webcamTexture.GetPixels32(), currentFrame, pixelCount);
        System.Array.Copy(currentFrame, previousFrame, pixelCount);
    }
    
    private void InitializeDebugTexture()
    {
        if (showDebugTexture && webcamTexture != null)
        {
            debugTexture = new Texture2D(webcamTexture.width, webcamTexture.height, TextureFormat.RGBA32, false);
            debugTexture.filterMode = FilterMode.Point;
        }
    }
    
    private void InitializeDebugMaterial()
    {
        if (showDebugTexture)
        {
            debugMaterial = GetComponent<MeshRenderer>().material;
        }
    }
    
    private void StartProcessingThread()
    {
        threadRunning = true;
        processingThread = new System.Threading.Thread(ProcessingThreadMain);
        processingThread.Start();
    }
    
    private void ProcessingThreadMain()
    {
        while (threadRunning)
        {
            if (webcamTexture != null && webcamTexture.isPlaying)
            {
                ProcessTouchDetection();
            }
            
            System.Threading.Thread.Sleep(Mathf.RoundToInt(updateRate * 1000));
        }
    }
    
    private void Update()
    {
        if (webcamTexture == null || !webcamTexture.isPlaying || isProcessing || !enableTouchDetection)
            return;
            
        frameCount++;
        
        // Update frames periodically
        if (Time.time - lastUpdateTime >= updateRate)
        {
            UpdateFrames();
            
            if (!useThreading)
            {
                ProcessTouchDetection();
            }
            
            lastUpdateTime = Time.time;
        }
        
        // Update debug texture
        if (showDebugTexture && debugTexture != null && processedFrame != null)
        {
            debugTexture.SetPixels32(processedFrame);
            debugTexture.Apply();
            
            if (debugMaterial != null)
            {
                debugMaterial.mainTexture = debugTexture;
            }
        }
        
        // Send touches to Unity's touch system
        UpdateUnityTouchSystem();
    }
    
    private void UpdateFrames()
    {
        // Get current frame from webcam
        Color32[] newFrame = webcamTexture.GetPixels32();
        
        lock (frameLock)
        {
            // Shift frames
            System.Array.Copy(currentFrame, previousFrame, currentFrame.Length);
            System.Array.Copy(newFrame, currentFrame, newFrame.Length);
        }
    }
    
    private void ProcessTouchDetection()
    {
        if (isProcessing) return;
        
        isProcessing = true;
        
        try
        {
            List<TouchData> detectedTouches = new List<TouchData>();
            
            lock (frameLock)
            {
                // Process only every N frames for optimization
                if (frameCount % processEveryNFrames == 0)
                {
                    DetectTouches(detectedTouches);
                }
            }
            
            // Update current touches
            var previousTouchCount = currentTouches.Count;
            currentTouches = detectedTouches;
            
            // Fire events
            OnTouchesDetected?.Invoke(currentTouches);
            
            // Fire touch count changed event
            if (previousTouchCount != currentTouches.Count)
            {
                OnTouchCountChanged?.Invoke(currentTouches.Count);
            }
            
            // Fire single touch detected event
            if (currentTouches.Count == 1)
            {
                OnSingleTouchDetected?.Invoke(currentTouches[0].position);
            }
            
            // Fire no touches event
            if (currentTouches.Count == 0 && previousTouchCount > 0)
            {
                OnNoTouchesDetected?.Invoke();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error in touch detection: {e.Message}");
        }
        finally
        {
            isProcessing = false;
        }
    }
    
    private void DetectTouches(List<TouchData> touches)
    {
        int width = webcamTexture.width;
        int height = webcamTexture.height;
        
        // Clear processed frame
        System.Array.Clear(processedFrame, 0, processedFrame.Length);
        
        // Calculate motion differences
        for (int y = 0; y < height; y += 2) // Skip pixels for performance
        {
            for (int x = 0; x < width; x += 2)
            {
                int index = y * width + x;
                
                if (index < previousFrame.Length && index < currentFrame.Length)
                {
                    Color32 prevPixel = previousFrame[index];
                    Color32 currPixel = currentFrame[index];
                    
                    // Calculate RGB difference
                    float diffR = Mathf.Abs(currPixel.r - prevPixel.r) / 255f;
                    float diffG = Mathf.Abs(currPixel.g - prevPixel.g) / 255f;
                    float diffB = Mathf.Abs(currPixel.b - prevPixel.b) / 255f;
                    
                    float totalDiff = (diffR + diffG + diffB) / 3f;
                    
                    if (totalDiff > motionThreshold)
                    {
                        // Mark as motion pixel
                        processedFrame[index] = new Color32(255, 0, 0, 255);
                        
                        // Update intensity for debugging
                        byte intensity = (byte)Mathf.Clamp(totalDiff * 255, 0, 255);
                        processedFrame[index] = new Color32(intensity, 0, 0, 255);
                    }
                }
            }
        }
        
        // Find touch clusters using connected components
        FindTouchClusters(touches);
    }
    
    private void FindTouchClusters(List<TouchData> touches)
    {
        bool[,] visited = new bool[webcamTexture.width, webcamTexture.height];
        int maxTouchesFound = 0;
        
        for (int y = 0; y < webcamTexture.height && maxTouchesFound < maxTouches; y++)
        {
            for (int x = 0; x < webcamTexture.width && maxTouchesFound < maxTouches; x++)
            {
                if (!visited[x, y] && IsMotionPixel(x, y))
                {
                    TouchData touch = BFSCluster(x, y, visited);
                    if (touch.area >= minTouchArea && touch.area <= maxTouchArea)
                    {
                        touches.Add(touch);
                        maxTouchesFound++;
                    }
                }
            }
        }
    }
    
    private TouchData BFSCluster(int startX, int startY, bool[,] visited)
    {
        int width = webcamTexture.width;
        int height = webcamTexture.height;
        
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        List<Vector2Int> clusterPixels = new List<Vector2Int>();
        
        queue.Enqueue(new Vector2Int(startX, startY));
        visited[startX, startY] = true;
        
        int totalIntensity = 0;
        int pixelCount = 0;
        
        while (queue.Count > 0 && clusterPixels.Count < 1000) // Limit cluster size
        {
            Vector2Int current = queue.Dequeue();
            clusterPixels.Add(current);
            
            int index = current.y * width + current.x;
            if (index < processedFrame.Length)
            {
                totalIntensity += processedFrame[index].r;
                pixelCount++;
            }
            
            // Check 8-connected neighbors
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0) continue;
                    
                    int newX = current.x + dx;
                    int newY = current.y + dy;
                    
                    if (newX >= 0 && newX < width && newY >= 0 && newY < height && 
                        !visited[newX, newY] && IsMotionPixel(newX, newY))
                    {
                        visited[newX, newY] = true;
                        queue.Enqueue(new Vector2Int(newX, newY));
                    }
                }
            }
        }
        
        // Calculate touch properties
        Vector2 center = CalculateClusterCenter(clusterPixels);
        float avgIntensity = pixelCount > 0 ? (float)totalIntensity / pixelCount / 255f : 0f;
        
        return new TouchData(center, avgIntensity, clusterPixels.Count);
    }
    
    private Vector2 CalculateClusterCenter(List<Vector2Int> pixels)
    {
        if (pixels.Count == 0) return Vector2.zero;
        
        float sumX = 0, sumY = 0;
        foreach (Vector2Int pixel in pixels)
        {
            sumX += pixel.x;
            sumY += pixel.y;
        }
        
        float centerX = sumX / pixels.Count;
        float centerY = sumY / pixels.Count;
        
        // Convert to UV coordinates (0-1 range)
        return new Vector2(centerX / webcamTexture.width, 1f - (centerY / webcamTexture.height));
    }
    
    private bool IsMotionPixel(int x, int y)
    {
        if (x < 0 || x >= webcamTexture.width || y < 0 || y >= webcamTexture.height)
            return false;
            
        int index = y * webcamTexture.width + x;
        if (index >= processedFrame.Length) return false;
        
        return processedFrame[index].r > 0;
    }
    
    private void UpdateUnityTouchSystem()
    {
        if (currentTouches.Count == 0) return;
        
        // Convert our touches to Unity TouchPhase format
        for (int i = 0; i < currentTouches.Count && i < 3; i++) // Unity supports max 3 touches
        {
            TouchData touchData = currentTouches[i];
            Vector2 screenPosition = new Vector2(
                touchData.position.x * Screen.width,
                touchData.position.y * Screen.height
            );
            
            // Create Unity Touch object (simulated)
            Touch unityTouch = new Touch();
            unityTouch.position = screenPosition;
            unityTouch.fingerId = i;
            unityTouch.phase = TouchPhase.Moved;
            unityTouch.deltaPosition = Vector2.zero;
            unityTouch.tapCount = 1;
            
            // Set touch type based on intensity
            if (touchData.intensity > 0.7f)
                unityTouch.phase = TouchPhase.Began;
            else if (touchData.intensity < 0.3f)
                unityTouch.phase = TouchPhase.Ended;
            
            // You can store these for integration with other systems
            // Note: Unity's Input.touches is read-only, so this is for reference
        }
    }
    
    private void OnDestroy()
    {
        StopProcessingThread();
        
        if (webcamTexture != null)
        {
            webcamTexture.Stop();
        }
    }
    
    private void StopProcessingThread()
    {
        threadRunning = false;
        
        if (processingThread != null && processingThread.IsAlive)
        {
            processingThread.Join(1000); // Wait 1 second max
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        if (Application.isPlaying && currentTouches != null)
        {
            foreach (TouchData touch in currentTouches)
            {
                Vector3 worldPos = Camera.main.ScreenToWorldPoint(
                    new Vector3(touch.position.x * Screen.width, 
                              touch.position.y * Screen.height, 
                              10f)
                );
                
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(worldPos, touchRadius);
                
                // Draw intensity indicator
                Gizmos.color = Color.Lerp(Color.yellow, Color.red, touch.intensity);
                Gizmos.DrawCube(worldPos, Vector3.one * 0.1f);
            }
        }
    }
    
    // Public methods for external control
    public void SetMotionThreshold(float threshold)
    {
        motionThreshold = Mathf.Clamp01(threshold);
    }
    
    public void SetMinTouchArea(int area)
    {
        minTouchArea = Mathf.Max(1, area);
    }
    
    public void SetMaxTouches(int max)
    {
        maxTouches = Mathf.Clamp(max, 1, 10);
    }
    
    public List<TouchData> GetCurrentTouches()
    {
        return new List<TouchData>(currentTouches);
    }
    
    // Public Properties
    public int TouchCount => currentTouches.Count;
    public bool IsProcessing => isProcessing;
    public bool EnableTouchDetection 
    { 
        get => enableTouchDetection; 
        set => enableTouchDetection = value; 
    }
    public float TouchSensitivity 
    { 
        get => touchSensitivity; 
        set => touchSensitivity = Mathf.Clamp01(value); 
    }
    public int MinTouchArea 
    { 
        get => minTouchArea; 
        set => minTouchArea = Mathf.Max(1, value); 
    }
    public float MaxTouchArea 
    { 
        get => maxTouchArea; 
        set => maxTouchArea = Mathf.Max(minTouchArea, value); 
    }
    public int MaxTouchPoints 
    { 
        get => maxTouches; 
        set => maxTouches = Mathf.Clamp(value, 1, 10); 
    }
    public Color DetectionColor 
    { 
        get => detectionColor; 
        set => detectionColor = value; 
    }
    public float DetectionColorThreshold 
    { 
        get => detectionColorThreshold; 
        set => detectionColorThreshold = Mathf.Clamp01(value); 
    }
    public int ProcessingWidth => webcamTexture?.width ?? 0;
    public int ProcessingHeight => webcamTexture?.height ?? 0;
    public List<Vector2> CurrentTouchPositions 
    { 
        get 
        { 
            List<Vector2> positions = new List<Vector2>();
            foreach (var touch in currentTouches)
            {
                positions.Add(touch.position);
            }
            return positions;
        } 
    }
    
    // Public Methods
    public void SimulateTouch(Vector2 position, bool isActive)
    {
        if (isActive)
        {
            TouchData simulatedTouch = new TouchData(position, 1.0f, minTouchArea, true);
            currentTouches.Add(simulatedTouch);
            touchHistory.Add(position);
            OnSingleTouchDetected?.Invoke(position);
            OnTouchCountChanged?.Invoke(currentTouches.Count);
        }
        else
        {
            // Remove touch at position
            currentTouches.RemoveAll(touch => Vector2.Distance(touch.position, position) < 0.1f);
            OnTouchCountChanged?.Invoke(currentTouches.Count);
        }
    }
    
    public void SetTouchSensitivity(float sensitivity)
    {
        touchSensitivity = Mathf.Clamp01(sensitivity);
        motionThreshold = 1f - touchSensitivity;
    }
    
    public void SetTouchDetectionEnabled(bool enabled)
    {
        enableTouchDetection = enabled;
        if (!enabled)
        {
            ClearAllTouches();
        }
    }
    
    public void ClearAllTouches()
    {
        currentTouches.Clear();
        OnTouchCountChanged?.Invoke(0);
        OnNoTouchesDetected?.Invoke();
    }
    
    public void ClearTouchHistory()
    {
        touchHistory.Clear();
    }
    
    public void ProcessWebcamTouches(List<TouchData> touches)
    {
        if (touches != null)
        {
            var previousTouchCount = currentTouches.Count;
            currentTouches = new List<TouchData>(touches);
            
            // Add positions to history
            foreach (var touch in touches)
            {
                if (!touchHistory.Contains(touch.position))
                {
                    touchHistory.Add(touch.position);
                }
            }
            
            // Fire events
            if (previousTouchCount != currentTouches.Count)
            {
                OnTouchCountChanged?.Invoke(currentTouches.Count);
            }
            
            if (currentTouches.Count == 1)
            {
                OnSingleTouchDetected?.Invoke(currentTouches[0].position);
            }
            
            if (currentTouches.Count == 0 && previousTouchCount > 0)
            {
                OnNoTouchesDetected?.Invoke();
            }
        }
    }
    
    public bool IsWebcamReady()
    {
        return webcamTexture != null && webcamTexture.isPlaying;
    }
    
    // Inspector validation
    private void OnValidate()
    {
        motionThreshold = Mathf.Clamp01(motionThreshold);
        minTouchArea = Mathf.Max(1, minTouchArea);
        maxTouches = Mathf.Clamp(maxTouches, 1, 10);
        touchRadius = Mathf.Max(0.1f, touchRadius);
        updateRate = Mathf.Max(0.01f, updateRate);
        processEveryNFrames = Mathf.Max(1, processEveryNFrames);
        maxTouchArea = Mathf.Max(minTouchArea, maxTouchArea);
        touchSensitivity = Mathf.Clamp01(touchSensitivity);
        detectionColorThreshold = Mathf.Clamp01(detectionColorThreshold);
    }
}
