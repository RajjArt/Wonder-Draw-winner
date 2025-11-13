using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// Webcam Character Pipeline - Phase 1: Integration and character creation
/// Bridges webcam capture and background removal with the existing character system
/// Creates rigged characters from processed webcam images
/// </summary>
public class WebcamCharacterPipeline : MonoBehaviour
{
    [Header("File Management")]
    [SerializeField] private string characterSavePath = "Assets/GeneratedCharacters/";
    [SerializeField] private string characterPrefix = "WebcamCharacter_";
    [SerializeField] private bool autoCreateDirectory = true;
    [SerializeField] private bool saveOriginalCaptures = false;
    
    [Header("Character Settings")]
    [SerializeField] private float defaultCharacterScale = 1.0f;
    [SerializeField] private Vector3 defaultCharacterPosition = new Vector3(0, 0, 0);
    [SerializeField] private string defaultLayerName = "Default";
    
    [Header("Integration Settings")]
    [SerializeField] private WebcamCapture webcamCapture;
    [SerializeField] private BackgroundRemover backgroundRemover;
    [SerializeField] private EnhancedPolishedCharacterController characterController; // From previous work
    [SerializeField] private bool integrateWithExistingSystem = true;
    
    // Character creation data
    private List<GeneratedCharacter> createdCharacters = new List<GeneratedCharacter>();
    private int characterCounter = 0;
    private bool isProcessing = false;
    
    // Processing queue
    private Queue<Texture2D> processingQueue = new Queue<Texture2D>();
    private Texture2D currentProcessingTexture;
    
    // Event system
    public System.Action<GeneratedCharacter> OnCharacterCreated;
    public System.Action<Texture2D> OnTextureReady;
    public System.Action<float> OnProcessingProgress;
    public System.Action<string> OnStatusUpdate;
    public System.Action OnPipelineStarted;
    public System.Action OnPipelineCompleted;
    
    // Properties
    public bool IsProcessing => isProcessing;
    public int TotalCharactersCreated => createdCharacters.Count;
    public string PipelineStatus => GetCurrentStatus();
    public List<GeneratedCharacter> CreatedCharacters => new List<GeneratedCharacter>(createdCharacters);
    
    private string GetCurrentStatus()
    {
        if (isProcessing) return $"Processing character {characterCounter + 1}...";
        if (createdCharacters.Count > 0) return $"{createdCharacters.Count} characters created";
        return "Ready to create characters";
    }
    
    // Character data structure
    [System.Serializable]
    public class GeneratedCharacter
    {
        public string characterId;
        public string characterName;
        public string filePath;
        public Texture2D characterTexture;
        public GameObject characterObject;
        public Vector3 position;
        public float scale;
        public System.DateTime creationTime;
        public bool isActive;
        
        public GeneratedCharacter(string id, string name, string path, Texture2D texture, GameObject obj, Vector3 pos, float scl)
        {
            characterId = id;
            characterName = name;
            filePath = path;
            characterTexture = texture;
            characterObject = obj;
            position = pos;
            scale = scl;
            creationTime = System.DateTime.Now;
            isActive = true;
        }
    }
    
    private void Awake()
    {
        // Auto-find components if not assigned
        if (webcamCapture == null)
            webcamCapture = FindFirstObjectByType<WebcamCapture>();
        if (backgroundRemover == null)
            backgroundRemover = FindFirstObjectByType<BackgroundRemover>();
        if (characterController == null && integrateWithExistingSystem)
            characterController = FindFirstObjectByType<EnhancedPolishedCharacterController>();
        
        // Subscribe to component events
        if (webcamCapture != null)
        {
            webcamCapture.OnFrameCaptured += QueueForProcessing;
        }
        if (backgroundRemover != null)
        {
            backgroundRemover.OnBackgroundRemoved += ProcessReadyTexture;
        }
    }
    
    private void Start()
    {
        InitializePipeline();
    }
    
    /// <summary>
    /// Initialize the character pipeline
    /// </summary>
    private void InitializePipeline()
    {
        if (autoCreateDirectory && !string.IsNullOrEmpty(characterSavePath))
        {
            if (!Directory.Exists(characterSavePath))
            {
                Directory.CreateDirectory(characterSavePath);
                Debug.Log($"📁 Created character directory: {characterSavePath}");
            }
        }
        
        UpdateStatusUI("Character pipeline initialized");
        Debug.Log("🎭 Webcam Character Pipeline ready");
    }
    
    /// <summary>
    /// Queue a captured frame for processing
    /// </summary>
    public void QueueForProcessing(Texture2D capturedTexture)
    {
        if (capturedTexture == null)
        {
            Debug.LogWarning("Cannot queue null texture");
            return;
        }
        
        processingQueue.Enqueue(capturedTexture);
        UpdateStatusUI($"Queued texture for processing (Queue: {processingQueue.Count})");
        
        // Start processing if not already processing
        if (!isProcessing)
        {
            StartProcessing();
        }
    }
    
    /// <summary>
    /// Start processing the queue
    /// </summary>
    private void StartProcessing()
    {
        if (processingQueue.Count == 0)
        {
            Debug.Log("No textures in processing queue");
            return;
        }
        
        isProcessing = true;
        OnPipelineStarted?.Invoke();
        
        currentProcessingTexture = processingQueue.Dequeue();
        OnProcessingProgress?.Invoke(0f);
        
        Debug.Log($"🔄 Started processing texture: {currentProcessingTexture.width}x{currentProcessingTexture.height}");
        UpdateStatusUI("Processing character creation...");
        
        // Process the texture
        if (backgroundRemover != null)
        {
            // Background removal will trigger OnBackgroundRemoved event
            backgroundRemover.ProcessFrame(currentProcessingTexture);
        }
        else
        {
            // Process without background removal
            ProcessReadyTexture(currentProcessingTexture);
        }
    }
    
    /// <summary>
    /// Process a texture that has had background removed
    /// </summary>
    private void ProcessReadyTexture(Texture2D processedTexture)
    {
        if (processedTexture == null)
        {
            Debug.LogError("Cannot process null processed texture");
            EndProcessing();
            return;
        }
        
        OnProcessingProgress?.Invoke(0.5f);
        
        try
        {
            // Create character
            GeneratedCharacter character = CreateCharacterFromTexture(processedTexture);
            
            if (character != null)
            {
                createdCharacters.Add(character);
                OnCharacterCreated?.Invoke(character);
                characterCounter++;
                
                Debug.Log($"✅ Character created: {character.characterName}");
            }
            else
            {
                Debug.LogError("Failed to create character from texture");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Character creation failed: {e.Message}");
        }
        
        OnProcessingProgress?.Invoke(1f);
        EndProcessing();
    }
    
    /// <summary>
    /// Create a character GameObject from a processed texture
    /// </summary>
    private GeneratedCharacter CreateCharacterFromTexture(Texture2D processedTexture)
    {
        // Generate character ID and name
        string characterId = $"{characterPrefix}{characterCounter:D3}";
        string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string characterName = $"{characterId}_{timestamp}";
        
        // Save texture to file
        string filePath = Path.Combine(characterSavePath, $"{characterName}.png");
        
        try
        {
            // Convert texture to PNG bytes
            byte[] pngBytes = processedTexture.EncodeToPNG();
            
            // Save file
            if (saveOriginalCaptures || autoCreateDirectory)
            {
                File.WriteAllBytes(filePath, pngBytes);
                Debug.Log($"💾 Saved character texture: {filePath}");
            }
            
            // Create GameObject for character
            GameObject characterObject = new GameObject(characterName);
            
            // Add SpriteRenderer for 2D rendering
            SpriteRenderer spriteRenderer = characterObject.AddComponent<SpriteRenderer>();
            
            // Create sprite from texture
            Sprite characterSprite = Sprite.Create(
                processedTexture,
                new Rect(0, 0, processedTexture.width, processedTexture.height),
                new Vector2(0.5f, 0.5f), // Pivot at center
                100f // Pixels per unit
            );
            
            spriteRenderer.sprite = characterSprite;
            spriteRenderer.sortingLayerName = defaultLayerName;
            
            // Position and scale
            characterObject.transform.position = defaultCharacterPosition;
            characterObject.transform.localScale = Vector3.one * defaultCharacterScale;
            
            // Integrate with existing character system
            if (integrateWithExistingSystem && characterController != null)
            {
                // Add character controller if integrating
                EnhancedPolishedCharacterController newController = characterObject.AddComponent<EnhancedPolishedCharacterController>();
                
                // Copy settings from existing controller
                newController.CopySettingsFrom(characterController);
                
                // Auto-setup physics if needed
                newController.AutoSetupPhysics();
                
                Debug.Log($"🎮 Character controller integrated for {characterName}");
            }
            else
            {
                // Add basic movement if no existing controller
                characterObject.AddComponent<EnhancedPolishedCharacterController>();
            }
            
            // Create character data
            GeneratedCharacter character = new GeneratedCharacter(
                characterId,
                characterName,
                filePath,
                processedTexture,
                characterObject,
                defaultCharacterPosition,
                defaultCharacterScale
            );
            
            return character;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Failed to create character: {e.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// End the current processing cycle
    /// </summary>
    private void EndProcessing()
    {
        isProcessing = false;
        currentProcessingTexture = null;
        
        OnPipelineCompleted?.Invoke();
        UpdateStatusUI("Processing completed");
        
        // Process next item in queue if available
        if (processingQueue.Count > 0)
        {
            StartProcessing();
        }
        else
        {
            Debug.Log("🎭 Character pipeline processing complete");
        }
    }
    
    /// <summary>
    /// Manually create a character from a texture
    /// </summary>
    public GeneratedCharacter CreateCharacterManually(Texture2D texture, string customName = null)
    {
        if (texture == null)
        {
            Debug.LogError("Cannot create character from null texture");
            return null;
        }
        
        string characterName = customName ?? $"Manual_{System.DateTime.Now:yyyyMMdd_HHmmss}";
        return CreateCharacterFromTexture(texture);
    }
    
    /// <summary>
    /// Activate/deactivate a created character
    /// </summary>
    public void SetCharacterActive(string characterId, bool active)
    {
        GeneratedCharacter character = createdCharacters.FirstOrDefault(c => c.characterId == characterId);
        
        if (character != null)
        {
            character.isActive = active;
            if (character.characterObject != null)
            {
                character.characterObject.SetActive(active);
            }
            Debug.Log($"{(active ? "Activated" : "Deactivated")} character: {character.characterName}");
        }
        else
        {
            Debug.LogWarning($"Character not found: {characterId}");
        }
    }
    
    /// <summary>
    /// Remove a created character
    /// </summary>
    public void RemoveCharacter(string characterId)
    {
        GeneratedCharacter character = createdCharacters.FirstOrDefault(c => c.characterId == characterId);
        
        if (character != null)
        {
            if (character.characterObject != null)
            {
                Destroy(character.characterObject);
            }
            
            createdCharacters.Remove(character);
            Debug.Log($"Removed character: {character.characterName}");
        }
        else
        {
            Debug.LogWarning($"Character not found for removal: {characterId}");
        }
    }
    
    /// <summary>
    /// Get statistics about created characters
    /// </summary>
    public string GetCharacterStats()
    {
        int activeCharacters = createdCharacters.Count(c => c.isActive);
        int totalCharacters = createdCharacters.Count;
        
        return $"Total Characters: {totalCharacters}\n" +
               $"Active Characters: {activeCharacters}\n" +
               $"Processing Queue: {processingQueue.Count}\n" +
               $"Status: {(isProcessing ? "Processing" : "Ready")}\n" +
               $"Save Path: {characterSavePath}";
    }
    
    /// <summary>
    /// Clear all created characters
    /// </summary>
    public void ClearAllCharacters()
    {
        foreach (var character in createdCharacters)
        {
            if (character.characterObject != null)
            {
                Destroy(character.characterObject);
            }
        }
        
        createdCharacters.Clear();
        characterCounter = 0;
        
        Debug.Log("🧹 Cleared all created characters");
        UpdateStatusUI("All characters cleared");
    }
    
    private void UpdateStatusUI(string message)
    {
        OnStatusUpdate?.Invoke(message);
        Debug.Log($"🎭 Pipeline: {message}");
    }
    
    /// <summary>
    /// Export character data to JSON
    /// </summary>
    public string ExportCharacterData()
    {
        var exportData = new
        {
            totalCharacters = createdCharacters.Count,
            exportTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            characters = createdCharacters.Select(c => new
            {
                id = c.characterId,
                name = c.characterName,
                filePath = c.filePath,
                position = new { x = c.position.x, y = c.position.y, z = c.position.z },
                scale = c.scale,
                creationTime = c.creationTime.ToString("yyyy-MM-dd HH:mm:ss"),
                isActive = c.isActive
            }).ToArray()
        };
        
        return JsonUtility.ToJson(exportData, true);
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (webcamCapture != null)
        {
            webcamCapture.OnFrameCaptured -= QueueForProcessing;
        }
        if (backgroundRemover != null)
        {
            backgroundRemover.OnBackgroundRemoved -= ProcessReadyTexture;
        }
    }
}
