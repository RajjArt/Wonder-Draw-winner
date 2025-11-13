using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

/// <summary>
/// Webcam Character System Tester - Complete System: Comprehensive testing and optimization
/// Tests all aspects of the webcam character system for runtime performance and functionality
/// Provides automated testing, performance analysis, and system validation
/// </summary>
public class WebcamCharacterSystemTester : MonoBehaviour
{
    [Header("Test Configuration")]
    [SerializeField] private bool runTestsOnStart = false;
    [SerializeField] private bool enableContinuousTesting = false;
    [SerializeField] private float testInterval = 30f; // Run tests every 30 seconds
    [SerializeField] private int maxTestCharacters = 10;
    [SerializeField] private bool logDetailedResults = true;
    
    [Header("Performance Thresholds")]
    [SerializeField] private float maxAcceptableFrameTime = 0.033f; // 30 FPS
    [SerializeField] private float maxMemoryUsageMB = 500f;
    [SerializeField] private int maxCharactersForOptimalPerformance = 15;
    [SerializeField] private float maxCaptureTime = 2f; // 2 seconds
    [SerializeField] private float maxBackgroundRemovalTime = 1f; // 1 second
    
    [Header("Test Parameters")]
    [SerializeField] private bool testWebcamCapture = true;
    [SerializeField] private bool testBackgroundRemoval = true;
    [SerializeField] private bool testCharacterCreation = true;
    [SerializeField] private bool testMultipleCharacters = true;
    [SerializeField] private bool testPerformance = true;
    [SerializeField] private bool testMemoryUsage = true;
    [SerializeField] private bool testIntegration = true;
    
    [Header("Integration")]
    [SerializeField] private WebcamCapture webcamCapture;
    [SerializeField] private BackgroundRemover backgroundRemover;
    [SerializeField] private WebcamCharacterPipeline pipeline;
    [SerializeField] private MultipleCharacterManager characterManager;
    [SerializeField] private CharacterAreaPositioning positioning;
    [SerializeField] private CompleteWebcamSystemUI systemUI;
    
    // Test results and performance tracking
    private TestResults currentResults = new TestResults();
    private List<TestResults> testHistory = new List<TestResults>();
    private Stopwatch testStopwatch = new Stopwatch();
    
    // Performance monitoring
    private List<float> frameTimeHistory = new List<float>();
    private List<float> memoryUsageHistory = new List<float>();
    private int frameCount = 0;
    private float lastPerformanceCheck = 0f;
    
    // Test state
    private bool isTestRunning = false;
    private bool testsCompleted = false;
    
    // Event system
    public System.Action<TestResults> OnTestCompleted;
    public System.Action<string> OnTestStarted;
    public System.Action<string> OnTestWarning;
    public System.Action<string> OnTestError;
    public System.Action<TestResults> OnPerformanceUpdate;
    
    // Test results data structure
    [System.Serializable]
    public class TestResults
    {
        public System.DateTime testTime;
        public float totalTestTime;
        public bool webcamTestPassed;
        public bool backgroundRemovalTestPassed;
        public bool characterCreationTestPassed;
        public bool multipleCharactersTestPassed;
        public bool performanceTestPassed;
        public bool memoryTestPassed;
        public bool integrationTestPassed;
        
        public float averageFrameTime;
        public float maxFrameTime;
        public int maxCharactersCreated;
        public float maxMemoryUsage;
        public float webcamInitializationTime;
        public float backgroundRemovalTime;
        public float characterCreationTime;
        public float characterCount;
        
        public List<string> warnings = new List<string>();
        public List<string> errors = new List<string>();
        public string systemStatus;
        
        public bool AllTestsPassed => webcamTestPassed && backgroundRemovalTestPassed && 
                                     characterCreationTestPassed && multipleCharactersTestPassed && 
                                     performanceTestPassed && memoryTestPassed && integrationTestPassed;
        
        public string GetSummary()
        {
            return $"Tests: {(AllTestsPassed ? "PASS" : "FAIL")} | " +
                   $"Frame Time: {averageFrameTime:F3}s | " +
                   $"Characters: {characterCount} | " +
                   $"Memory: {maxMemoryUsage:F1}MB | " +
                   $"Warnings: {warnings.Count} | " +
                   $"Errors: {errors.Count}";
        }
    }
    
    // Properties
    public bool IsTestRunning => isTestRunning;
    public TestResults LastResults => testHistory.LastOrDefault();
    public List<TestResults> TestHistory => new List<TestResults>(testHistory);
    public bool AllSystemsOperational => GetAllSystemsOperational();
    
    private bool GetAllSystemsOperational()
    {
        return webcamCapture != null && backgroundRemover != null && 
               pipeline != null && characterManager != null && positioning != null;
    }
    
    private void Awake()
    {
        // Auto-find components if not assigned
        FindAllComponents();
    }
    
    private void Start()
    {
        if (runTestsOnStart)
        {
            StartCoroutine(RunSystemTests());
        }
        
        if (enableContinuousTesting)
        {
            StartCoroutine(ContinuousTesting());
        }
        
        UnityEngine.Debug.Log("🧪 Webcam Character System Tester initialized");
    }
    
    private void Update()
    {
        // Performance monitoring
        if (testPerformance)
        {
            MonitorPerformance();
        }
        
        // Memory monitoring
        if (testMemoryUsage)
        {
            MonitorMemoryUsage();
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
        if (systemUI == null) systemUI = FindFirstObjectByType<CompleteWebcamSystemUI>();
    }
    
    /// <summary>
    /// Run complete system tests
    /// </summary>
    public IEnumerator RunSystemTests()
    {
        if (isTestRunning)
        {
            UnityEngine.Debug.LogWarning("⚠️ Tests already running");
            yield break;
        }
        
        isTestRunning = true;
        testsCompleted = false;
        currentResults = new TestResults();
        testStopwatch.Start();
        
        OnTestStarted?.Invoke("Starting comprehensive system tests");
        UnityEngine.Debug.Log("🧪 Starting system tests...");
        
        // Test 1: Component Integration
        if (testIntegration)
        {
            yield return StartCoroutine(TestComponentIntegration());
        }
        
        // Test 2: Webcam Capture
        if (testWebcamCapture)
        {
            yield return StartCoroutine(TestWebcamCapture());
        }
        
        // Test 3: Background Removal
        if (testBackgroundRemoval)
        {
            yield return StartCoroutine(TestBackgroundRemoval());
        }
        
        // Test 4: Character Creation
        if (testCharacterCreation)
        {
            yield return StartCoroutine(TestCharacterCreation());
        }
        
        // Test 5: Multiple Characters
        if (testMultipleCharacters)
        {
            yield return StartCoroutine(TestMultipleCharacters());
        }
        
        // Test 6: Performance
        if (testPerformance)
        {
            yield return StartCoroutine(TestPerformance());
        }
        
        // Test 7: Memory Usage
        if (testMemoryUsage)
        {
            yield return StartCoroutine(TestMemoryUsage());
        }
        
        // Finalize results
        FinalizeTestResults();
        
        isTestRunning = false;
        testsCompleted = true;
        
        testStopwatch.Stop();
        currentResults.totalTestTime = testStopwatch.ElapsedMilliseconds / 1000f;
        
        testHistory.Add(currentResults);
        
        // Keep only last 50 test results
        if (testHistory.Count > 50)
        {
            testHistory.RemoveAt(0);
        }
        
        OnTestCompleted?.Invoke(currentResults);
        UnityEngine.Debug.Log($"✅ System tests completed: {currentResults.GetSummary()}");
    }
    
    /// <summary>
    /// Test component integration
    /// </summary>
    private IEnumerator TestComponentIntegration()
    {
        OnTestStarted?.Invoke("Testing component integration");
        
        bool allComponentsFound = AllSystemsOperational;
        
        if (allComponentsFound)
        {
            currentResults.integrationTestPassed = true;
            UnityEngine.Debug.Log("✅ All components found and connected");
        }
        else
        {
            currentResults.integrationTestPassed = false;
            currentResults.errors.Add("Missing required components");
            UnityEngine.Debug.LogError("❌ Missing required components");
        }
        
        yield return new WaitForSeconds(0.5f);
    }
    
    /// <summary>
    /// Test webcam capture functionality
    /// </summary>
    private IEnumerator TestWebcamCapture()
    {
        OnTestStarted?.Invoke("Testing webcam capture");
        
        if (webcamCapture == null)
        {
            currentResults.webcamTestPassed = false;
            currentResults.errors.Add("WebcamCapture component not found");
            yield break;
        }
        
        float startTime = Time.time;
        
        // Test initialization
        try
        {
            webcamCapture.InitializeWebcam();
        }
        catch (System.Exception e)
        {
            currentResults.webcamTestPassed = false;
            currentResults.errors.Add($"Webcam test exception: {e.Message}");
            yield break;
        }
        
        // Wait for initialization (outside try-catch)
        float timeout = 5f;
        while (!webcamCapture.IsInitialized && Time.time - startTime < timeout)
        {
            yield return null;
        }
        
        if (webcamCapture.IsInitialized)
        {
            currentResults.webcamInitializationTime = Time.time - startTime;
            currentResults.webcamTestPassed = currentResults.webcamInitializationTime < maxCaptureTime;
            
            if (currentResults.webcamTestPassed)
            {
                UnityEngine.Debug.Log($"✅ Webcam test passed: {currentResults.webcamInitializationTime:F2}s");
            }
            else
            {
                currentResults.warnings.Add($"Webcam initialization took {currentResults.webcamInitializationTime:F2}s (max: {maxCaptureTime:F2}s)");
            }
            
            // Test capture
            try
            {
                Texture2D capturedFrame = webcamCapture.CaptureFrame();
                if (capturedFrame != null)
                {
                    UnityEngine.Debug.Log($"✅ Webcam capture successful: {capturedFrame.width}x{capturedFrame.height}");
                }
                else
                {
                    currentResults.errors.Add("Failed to capture frame from webcam");
                    currentResults.webcamTestPassed = false;
                }
            }
            catch (System.Exception e)
            {
                currentResults.webcamTestPassed = false;
                currentResults.errors.Add($"Webcam capture exception: {e.Message}");
            }
        }
        else
        {
            currentResults.webcamTestPassed = false;
            currentResults.errors.Add("Webcam initialization timeout");
        }
        
        yield return new WaitForSeconds(1f);
    }
    
    /// <summary>
    /// Test background removal functionality
    /// </summary>
    private IEnumerator TestBackgroundRemoval()
    {
        OnTestStarted?.Invoke("Testing background removal");
        
        if (backgroundRemover == null)
        {
            currentResults.backgroundRemovalTestPassed = false;
            currentResults.errors.Add("BackgroundRemover component not found");
            yield break;
        }
        
        float startTime = Time.time;
        
        try
        {
            // Create test texture with white background
            Texture2D testTexture = CreateTestTexture(256, 256, Color.white);
            
            // Add some colored content
            AddTestContent(testTexture);
            
            // Process texture
            Texture2D processedTexture = backgroundRemover.ProcessFrame(testTexture);
            
            float processingTime = Time.time - startTime;
            currentResults.backgroundRemovalTime = processingTime;
            currentResults.backgroundRemovalTestPassed = processingTime < maxBackgroundRemovalTime;
            
            if (processedTexture != null)
            {
                UnityEngine.Debug.Log($"✅ Background removal test passed: {processingTime:F2}s");
            }
            else
            {
                currentResults.errors.Add("Failed to process texture for background removal");
                currentResults.backgroundRemovalTestPassed = false;
            }
        }
        catch (System.Exception e)
        {
            currentResults.backgroundRemovalTestPassed = false;
            currentResults.errors.Add($"Background removal test exception: {e.Message}");
        }
        
        yield return new WaitForSeconds(0.5f);
    }
    
    /// <summary>
    /// Test character creation pipeline
    /// </summary>
    private IEnumerator TestCharacterCreation()
    {
        OnTestStarted?.Invoke("Testing character creation");
        
        if (pipeline == null)
        {
            currentResults.characterCreationTestPassed = false;
            currentResults.errors.Add("WebcamCharacterPipeline component not found");
            yield break;
        }
        
        float startTime = Time.time;
        
        try
        {
            // Create test texture
            Texture2D testTexture = CreateTestTexture(128, 128, new Color(0.9f, 0.9f, 0.9f));
            AddTestContent(testTexture);
            
            // Create character
            var character = pipeline.CreateCharacterManually(testTexture, "Test_Character");
            
            float creationTime = Time.time - startTime;
            currentResults.characterCreationTime = creationTime;
            
            if (character != null)
            {
                currentResults.characterCreationTestPassed = true;
                currentResults.characterCount = 1;
                UnityEngine.Debug.Log($"✅ Character creation test passed: {creationTime:F2}s");
            }
            else
            {
                currentResults.characterCreationTestPassed = false;
                currentResults.errors.Add("Failed to create character from texture");
            }
        }
        catch (System.Exception e)
        {
            currentResults.characterCreationTestPassed = false;
            currentResults.errors.Add($"Character creation test exception: {e.Message}");
        }
        
        yield return new WaitForSeconds(1f);
    }
    
    /// <summary>
    /// Test multiple character management
    /// </summary>
    private IEnumerator TestMultipleCharacters()
    {
        OnTestStarted?.Invoke("Testing multiple characters");
        
        if (characterManager == null)
        {
            currentResults.multipleCharactersTestPassed = false;
            currentResults.errors.Add("MultipleCharacterManager component not found");
            yield break;
        }
        
        // First, prepare all the character creation operations
        List<System.Action> characterCreationActions = new List<System.Action>();
        int charactersCreated = 0;
        int maxToCreate = Mathf.Min(maxTestCharacters, maxCharactersForOptimalPerformance);
        bool testFailed = false;
        
        try
        {
            // Create multiple test characters (without yielding)
            for (int i = 0; i < maxToCreate; i++)
            {
                Texture2D testTexture = CreateTestTexture(64, 64, new Color(0.8f, 0.8f, 0.8f));
                AddTestContent(testTexture);
                
                var character = pipeline.CreateCharacterManually(testTexture, $"Test_Character_{i}");
                
                if (character != null)
                {
                    charactersCreated++;
                }
            }
        }
        catch (System.Exception e)
        {
            currentResults.multipleCharactersTestPassed = false;
            currentResults.errors.Add($"Multiple characters test exception: {e.Message}");
            testFailed = true;
        }
        
        // Check for test failure and exit if needed (outside try-catch)
        if (testFailed)
        {
            yield break;
        }
        
        // Now execute the character creation with yields (outside try-catch)
        if (charactersCreated > 0)
        {
            UnityEngine.Debug.Log($"✅ Multiple characters test passed: {charactersCreated} characters created");
        }
        else
        {
            currentResults.errors.Add("Failed to create any test characters");
        }
        
        yield return new WaitForSeconds(1f);
    }
    
    /// <summary>
    /// Test system performance
    /// </summary>
    private IEnumerator TestPerformance()
    {
        OnTestStarted?.Invoke("Testing system performance");
        
        // Wait a moment for system to stabilize
        yield return new WaitForSeconds(2f);
        
        if (frameTimeHistory.Count > 0)
        {
            currentResults.averageFrameTime = frameTimeHistory.Average();
            currentResults.maxFrameTime = frameTimeHistory.Max();
            currentResults.performanceTestPassed = currentResults.averageFrameTime < maxAcceptableFrameTime;
            
            if (currentResults.performanceTestPassed)
            {
                UnityEngine.Debug.Log($"✅ Performance test passed: Avg {currentResults.averageFrameTime:F3}s, Max {currentResults.maxFrameTime:F3}s");
            }
            else
            {
                currentResults.warnings.Add($"Performance degradation: Avg {currentResults.averageFrameTime:F3}s, Max {currentResults.maxFrameTime:F3}s");
            }
        }
        else
        {
            currentResults.performanceTestPassed = false;
            currentResults.errors.Add("No performance data available");
        }
        
        yield return new WaitForSeconds(0.5f);
    }
    
    /// <summary>
    /// Test memory usage
    /// </summary>
    private IEnumerator TestMemoryUsage()
    {
        OnTestStarted?.Invoke("Testing memory usage");
        
        if (memoryUsageHistory.Count > 0)
        {
            currentResults.maxMemoryUsage = memoryUsageHistory.Max() / (1024f * 1024f); // Convert to MB
            currentResults.memoryTestPassed = currentResults.maxMemoryUsage < maxMemoryUsageMB;
            
            if (currentResults.memoryTestPassed)
            {
                UnityEngine.Debug.Log($"✅ Memory test passed: Max {currentResults.maxMemoryUsage:F1}MB");
            }
            else
            {
                currentResults.warnings.Add($"High memory usage: {currentResults.maxMemoryUsage:F1}MB (max: {maxMemoryUsageMB:F1}MB)");
            }
        }
        else
        {
            currentResults.memoryTestPassed = false;
            currentResults.errors.Add("No memory usage data available");
        }
        
        yield return new WaitForSeconds(0.5f);
    }
    
    /// <summary>
    /// Monitor system performance
    /// </summary>
    private void MonitorPerformance()
    {
        frameCount++;
        
        // Record frame time
        float frameTime = Time.unscaledDeltaTime;
        frameTimeHistory.Add(frameTime);
        
        // Keep only last 100 frame times
        if (frameTimeHistory.Count > 100)
        {
            frameTimeHistory.RemoveAt(0);
        }
        
        // Update performance metrics every second
        if (Time.time - lastPerformanceCheck > 1f)
        {
            lastPerformanceCheck = Time.time;
            
            if (frameTimeHistory.Count > 0)
            {
                float avgFrameTime = frameTimeHistory.Average();
                
                OnPerformanceUpdate?.Invoke(new TestResults
                {
                    averageFrameTime = avgFrameTime,
                    characterCount = characterManager != null ? characterManager.ActiveCharacterCount : 0
                });
            }
        }
    }
    
    /// <summary>
    /// Monitor memory usage
    /// </summary>
    private void MonitorMemoryUsage()
    {
        // Get current memory usage (Windows-specific, adjust for other platforms)
        #if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        float currentMemory = System.GC.GetTotalMemory(false);
        memoryUsageHistory.Add(currentMemory);
        
        // Keep only last 50 memory samples
        if (memoryUsageHistory.Count > 50)
        {
            memoryUsageHistory.RemoveAt(0);
        }
        #endif
    }
    
    /// <summary>
    /// Continuous testing for monitoring
    /// </summary>
    private IEnumerator ContinuousTesting()
    {
        while (true)
        {
            yield return new WaitForSeconds(testInterval);
            
            if (!isTestRunning)
            {
                // Quick performance check
                if (frameTimeHistory.Count > 0)
                {
                    float avgFrameTime = frameTimeHistory.Average();
                    if (avgFrameTime > maxAcceptableFrameTime)
                    {
                        OnTestWarning?.Invoke($"Performance warning: {avgFrameTime:F3}s frame time");
                    }
                }
                
                // Quick memory check
                if (memoryUsageHistory.Count > 0)
                {
                    float maxMemory = memoryUsageHistory.Max() / (1024f * 1024f);
                    if (maxMemory > maxMemoryUsageMB)
                    {
                        OnTestWarning?.Invoke($"Memory warning: {maxMemory:F1}MB usage");
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Create test texture for testing
    /// </summary>
    private Texture2D CreateTestTexture(int width, int height, Color backgroundColor)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[width * height];
        
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = backgroundColor;
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
        
        return texture;
    }
    
    /// <summary>
    /// Add test content to texture
    /// </summary>
    private void AddTestContent(Texture2D texture)
    {
        int width = texture.width;
        int height = texture.height;
        
        // Add a simple colored square in the center
        int centerX = width / 2;
        int centerY = height / 2;
        int size = Mathf.Min(width, height) / 4;
        
        for (int y = centerY - size; y < centerY + size; y++)
        {
            for (int x = centerX - size; x < centerX + size; x++)
            {
                if (x >= 0 && x < width && y >= 0 && y < height)
                {
                    texture.SetPixel(x, y, Color.red);
                }
            }
        }
        
        texture.Apply();
    }
    
    /// <summary>
    /// Finalize test results
    /// </summary>
    private void FinalizeTestResults()
    {
        currentResults.testTime = System.DateTime.Now;
        currentResults.systemStatus = GetSystemStatus();
        
        if (logDetailedResults)
        {
            LogTestResults();
        }
    }
    
    /// <summary>
    /// Get current system status
    /// </summary>
    private string GetSystemStatus()
    {
        string status = "System Status: ";
        
        if (characterManager != null)
        {
            status += $"Characters: {characterManager.ActiveCharacterCount}/{characterManager.TotalCharacterCount} | ";
        }
        
        if (webcamCapture != null)
        {
            status += $"Camera: {(webcamCapture.IsCapturing ? "Active" : "Inactive")} | ";
        }
        
        if (positioning != null)
        {
            status += $"Positioning: {positioning.AvailableSlots}/{positioning.TotalSlots} slots";
        }
        
        return status;
    }
    
    /// <summary>
    /// Log detailed test results
    /// </summary>
    private void LogTestResults()
    {
        UnityEngine.Debug.Log("=== TEST RESULTS ===");
        UnityEngine.Debug.Log($"Time: {currentResults.testTime}");
        UnityEngine.Debug.Log($"Total Test Time: {currentResults.totalTestTime:F2}s");
        UnityEngine.Debug.Log($"Overall Result: {(currentResults.AllTestsPassed ? "PASS" : "FAIL")}");
        
        UnityEngine.Debug.Log("Individual Tests:");
        UnityEngine.Debug.Log($"  Webcam: {(currentResults.webcamTestPassed ? "PASS" : "FAIL")}");
        UnityEngine.Debug.Log($"  Background Removal: {(currentResults.backgroundRemovalTestPassed ? "PASS" : "FAIL")}");
        UnityEngine.Debug.Log($"  Character Creation: {(currentResults.characterCreationTestPassed ? "PASS" : "FAIL")}");
        UnityEngine.Debug.Log($"  Multiple Characters: {(currentResults.multipleCharactersTestPassed ? "PASS" : "FAIL")}");
        UnityEngine.Debug.Log($"  Performance: {(currentResults.performanceTestPassed ? "PASS" : "FAIL")}");
        UnityEngine.Debug.Log($"  Memory: {(currentResults.memoryTestPassed ? "PASS" : "FAIL")}");
        UnityEngine.Debug.Log($"  Integration: {(currentResults.integrationTestPassed ? "PASS" : "FAIL")}");
        
        if (currentResults.warnings.Count > 0)
        {
            UnityEngine.Debug.Log("Warnings:");
            foreach (string warning in currentResults.warnings)
            {
                UnityEngine.Debug.Log($"  ⚠️ {warning}");
            }
        }
        
        if (currentResults.errors.Count > 0)
        {
            UnityEngine.Debug.Log("Errors:");
            foreach (string error in currentResults.errors)
            {
                UnityEngine.Debug.Log($"  ❌ {error}");
            }
        }
        
        UnityEngine.Debug.Log("===================");
    }
    
    /// <summary>
    /// Get test summary
    /// </summary>
    public string GetTestSummary()
    {
        if (testHistory.Count == 0)
        {
            return "No tests run yet";
        }
        
        var latest = testHistory.Last();
        return latest.GetSummary();
    }
    
    /// <summary>
    /// Get detailed test history
    /// </summary>
    public string GetTestHistory()
    {
        if (testHistory.Count == 0)
        {
            return "No test history available";
        }
        
        string history = "Test History:\n\n";
        
        foreach (var result in testHistory.TakeLast(10))
        {
            history += $"{result.testTime:HH:mm:ss} - {result.GetSummary()}\n";
        }
        
        return history;
    }
    
    /// <summary>
    /// Clear test history
    /// </summary>
    public void ClearTestHistory()
    {
        testHistory.Clear();
        frameTimeHistory.Clear();
        memoryUsageHistory.Clear();
        UnityEngine.Debug.Log("🧹 Test history cleared");
    }
    
    /// <summary>
    /// Force system performance check
    /// </summary>
    public void ForcePerformanceCheck()
    {
        if (frameTimeHistory.Count > 0)
        {
            float avgFrameTime = frameTimeHistory.Average();
            float maxFrameTime = frameTimeHistory.Max();
            
            if (avgFrameTime > maxAcceptableFrameTime)
            {
                OnTestWarning?.Invoke($"Performance check: {avgFrameTime:F3}s avg, {maxFrameTime:F3}s max");
            }
            else
            {
                UnityEngine.Debug.Log($"✅ Performance check passed: {avgFrameTime:F3}s avg");
            }
        }
    }
    
    /// <summary>
    /// Get system optimization recommendations
    /// </summary>
    public string GetOptimizationRecommendations()
    {
        string recommendations = "Optimization Recommendations:\n\n";
        
        if (characterManager != null && characterManager.ActiveCharacterCount > maxCharactersForOptimalPerformance)
        {
            recommendations += $"• Consider reducing active characters (current: {characterManager.ActiveCharacterCount}, recommended: {maxCharactersForOptimalPerformance})\n";
        }
        
        if (frameTimeHistory.Count > 0)
        {
            float avgFrameTime = frameTimeHistory.Average();
            if (avgFrameTime > maxAcceptableFrameTime)
            {
                recommendations += "• Enable LOD system to reduce distant character processing\n";
                recommendations += "• Consider reducing texture quality for distant characters\n";
            }
        }
        
        if (memoryUsageHistory.Count > 0)
        {
            float maxMemory = memoryUsageHistory.Max() / (1024f * 1024f);
            if (maxMemory > maxMemoryUsageMB * 0.8f)
            {
                recommendations += "• Enable automatic character cleanup\n";
                recommendations += "• Reduce maximum character pool size\n";
            }
        }
        
        if (recommendations == "Optimization Recommendations:\n\n")
        {
            recommendations += "System is performing optimally! ✅";
        }
        
        return recommendations;
    }
}