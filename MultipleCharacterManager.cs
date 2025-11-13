using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Multiple Character Manager - Complete System: Manages multiple webcam characters simultaneously
/// Handles character pooling, area assignment, and memory optimization for runtime operation
/// Integrates with WebcamCharacterPipeline and existing character movement system
/// </summary>
public class MultipleCharacterManager : MonoBehaviour
{
    [Header("Character Pool Settings")]
    [SerializeField] private int maxCharacters = 20;
    [SerializeField] private bool enablePooling = true;
    [SerializeField] private bool enableAutoCleanup = true;
    [SerializeField] private float autoCleanupTime = 300f; // 5 minutes
    [SerializeField] private int maxInactiveCharacters = 10;
    
    [Header("Memory Management")]
    [SerializeField] private bool enableLOD = true;
    [SerializeField] private float farDistanceLOD = 15f;
    [SerializeField] private float veryFarDistanceLOD = 25f;
    
    [Header("Integration")]
    [SerializeField] private WebcamCharacterPipeline pipeline;
    [SerializeField] private CharacterAreaPositioning areaPositioning;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private GameObject characterParent;
    
    // Character storage and management
    private Dictionary<string, ManagedCharacter> activeCharacters = new Dictionary<string, ManagedCharacter>();
    private Dictionary<string, ManagedCharacter> inactiveCharacters = new Dictionary<string, ManagedCharacter>();
    private Queue<GameObject> characterPool = new Queue<GameObject>();
    private List<ManagedCharacter> allCharacters = new List<ManagedCharacter>();
    
    // Performance tracking
    private int totalCharactersCreated = 0;
    private float averageFrameTime = 0f;
    private float lastPerformanceCheck = 0f;
    private int performanceCheckInterval = 60; // Check every 60 seconds
    
    // Event system
    public System.Action<ManagedCharacter> OnCharacterAdded;
    public System.Action<string> OnCharacterRemoved;
    public System.Action<string> OnCharacterActivated;
    public System.Action<string> OnCharacterDeactivated;
    public System.Action<float> OnPerformanceUpdate;
    public System.Action<string> OnStatusUpdate;
    
    // Properties
    public int ActiveCharacterCount => activeCharacters.Count;
    public int TotalCharacterCount => allCharacters.Count;
    public float AverageFrameTime => averageFrameTime;
    public string ManagerStatus => GetCurrentStatus();
    public Dictionary<string, ManagedCharacter> GetActiveCharacters() => new Dictionary<string, ManagedCharacter>(activeCharacters);
    
    private string GetCurrentStatus()
    {
        return $"Active: {activeCharacters.Count}/{maxCharacters} | Total: {totalCharactersCreated} | Pool: {characterPool.Count}";
    }
    
    // Character LOD Level enum
    public enum CharacterLODLevel
    {
        High,     // Full quality, close to camera
        Medium,   // Reduced quality, medium distance
        Low,      // Very low quality, far from camera
        Invisible // Not rendered, very far from camera
    }
    
    // Character data structure with LOD and performance tracking
    [System.Serializable]
    public class ManagedCharacter
    {
        public string characterId;
        public string characterName;
        public GameObject gameObject;
        public SpriteRenderer spriteRenderer;
        public EnhancedPolishedCharacterController controller;
        public Vector3 spawnPosition;
        public float spawnTime;
        public CharacterLODLevel lodLevel;
        public bool isActive;
        public bool isInvisible;
        public float distanceToCamera;
        public Texture2D originalTexture;
        public Texture2D compressedTexture;
        public System.DateTime lastUsed;
        
        public ManagedCharacter(string id, string name, GameObject obj, Vector3 position, Texture2D texture)
        {
            characterId = id;
            characterName = name;
            gameObject = obj;
            spawnPosition = position;
            spawnTime = Time.time;
            originalTexture = texture;
            lastUsed = System.DateTime.Now;
            lodLevel = CharacterLODLevel.High;
            isActive = true;
            isInvisible = false;
            
            // Get components
            spriteRenderer = obj.GetComponent<SpriteRenderer>();
            controller = obj.GetComponent<EnhancedPolishedCharacterController>();
        }
        
        public void SetActive(bool active)
        {
            isActive = active;
            if (gameObject != null)
            {
                gameObject.SetActive(active);
            }
        }
        
        public void SetLODLevel(CharacterLODLevel newLOD)
        {
            if (lodLevel == newLOD) return;
            
            lodLevel = newLOD;
            
            if (spriteRenderer != null)
            {
                switch (newLOD)
                {
                    case CharacterLODLevel.High:
                        spriteRenderer.enabled = true;
                        spriteRenderer.color = Color.white;
                        break;
                    case CharacterLODLevel.Medium:
                        spriteRenderer.enabled = true;
                        spriteRenderer.color = new Color(0.8f, 0.8f, 0.8f, 0.9f);
                        break;
                    case CharacterLODLevel.Low:
                        spriteRenderer.enabled = true;
                        spriteRenderer.color = new Color(0.6f, 0.6f, 0.6f, 0.7f);
                        break;
                    case CharacterLODLevel.Invisible:
                        spriteRenderer.enabled = false;
                        break;
                }
            }
            
            lastUsed = System.DateTime.Now;
        }
    }
    
    private void Awake()
    {
        // Auto-find components if not assigned
        if (pipeline == null)
            pipeline = FindFirstObjectByType<WebcamCharacterPipeline>();
        if (areaPositioning == null)
            areaPositioning = FindFirstObjectByType<CharacterAreaPositioning>();
        if (mainCamera == null)
            mainCamera = Camera.main;
        if (characterParent == null)
            characterParent = new GameObject("WebcamCharacters");
        
        // Initialize character parent
        if (characterParent != null && characterParent.transform.position == Vector3.zero)
        {
            characterParent.transform.position = Vector3.zero;
        }
    }
    
    private void Start()
    {
        InitializeManager();
        SetupEventSubscriptions();
        
        Debug.Log("🎭 Multiple Character Manager initialized");
    }
    
    private void Update()
    {
        // Performance monitoring
        if (Time.time - lastPerformanceCheck > performanceCheckInterval)
        {
            CheckPerformance();
            lastPerformanceCheck = Time.time;
        }
        
        // Update character LOD based on distance
        if (enableLOD && mainCamera != null)
        {
            UpdateCharacterLOD();
        }
        
        // Auto cleanup inactive characters
        if (enableAutoCleanup)
        {
            CheckAutoCleanup();
        }
    }
    
    /// <summary>
    /// Initialize the character manager
    /// </summary>
    private void InitializeManager()
    {
        // Create character pool if pooling is enabled
        if (enablePooling)
        {
            CreateCharacterPool();
        }
        
        UpdateStatusUI("Character manager initialized");
    }
    
    /// <summary>
    /// Setup event subscriptions with pipeline
    /// </summary>
    private void SetupEventSubscriptions()
    {
        if (pipeline != null)
        {
            pipeline.OnCharacterCreated += OnCharacterCreated;
            pipeline.OnPipelineCompleted += OnPipelineCompleted;
        }
    }
    
    /// <summary>
    /// Create a pool of reusable character GameObjects
    /// </summary>
    private void CreateCharacterPool()
    {
        for (int i = 0; i < maxCharacters; i++)
        {
            GameObject pooledCharacter = CreatePooledCharacter();
            characterPool.Enqueue(pooledCharacter);
            pooledCharacter.SetActive(false);
        }
        
        Debug.Log($"🏊 Created character pool: {maxCharacters} objects");
    }
    
    /// <summary>
    /// Create a single pooled character GameObject
    /// </summary>
    private GameObject CreatePooledCharacter()
    {
        GameObject character = new GameObject("PooledCharacter");
        character.transform.SetParent(characterParent.transform);
        
        // Add components
        SpriteRenderer renderer = character.AddComponent<SpriteRenderer>();
        renderer.sortingLayerName = "Default";
        renderer.sortingOrder = 0;
        
        // Add controller
        EnhancedPolishedCharacterController controller = character.AddComponent<EnhancedPolishedCharacterController>();
        
        return character;
    }
    
    /// <summary>
    /// Handle new character creation from pipeline
    /// </summary>
    private void OnCharacterCreated(WebcamCharacterPipeline.GeneratedCharacter characterData)
    {
        if (activeCharacters.Count >= maxCharacters)
        {
            Debug.LogWarning($"⚠️ Max characters reached ({maxCharacters}) - cannot add more");
            UpdateStatusUI("Maximum characters reached");
            return;
        }
        
        // Get spawn position from area positioning
        Vector3 spawnPosition = Vector3.zero;
        if (areaPositioning != null)
        {
            spawnPosition = areaPositioning.GetNextSpawnPosition();
        }
        else
        {
            // Default positioning if no area system
            spawnPosition = GetDefaultSpawnPosition();
        }
        
        // Create managed character
        ManagedCharacter managedCharacter = CreateManagedCharacter(characterData, spawnPosition);
        
        if (managedCharacter != null)
        {
            // Add to active characters
            activeCharacters[managedCharacter.characterId] = managedCharacter;
            allCharacters.Add(managedCharacter);
            totalCharactersCreated++;
            
            // Notify listeners
            OnCharacterAdded?.Invoke(managedCharacter);
            OnCharacterActivated?.Invoke(managedCharacter.characterId);
            
            Debug.Log($"✅ Character added: {managedCharacter.characterName} at {spawnPosition}");
            UpdateStatusUI($"Character created: {managedCharacter.characterName}");
        }
    }
    
    /// <summary>
    /// Create a managed character from pipeline data
    /// </summary>
    private ManagedCharacter CreateManagedCharacter(WebcamCharacterPipeline.GeneratedCharacter data, Vector3 position)
    {
        try
        {
            // Get pooled object or create new
            GameObject characterObject = GetPooledCharacter();
            
            // Setup character object
            characterObject.name = data.characterName;
            characterObject.transform.position = position;
            characterObject.transform.localScale = Vector3.one * data.scale;
            characterObject.SetActive(true);
            
            // Get components
            SpriteRenderer renderer = characterObject.GetComponent<SpriteRenderer>();
            EnhancedPolishedCharacterController controller = characterObject.GetComponent<EnhancedPolishedCharacterController>();
            
            // Apply character texture
            if (data.characterTexture != null && renderer != null)
            {
                renderer.sprite = Sprite.Create(
                    data.characterTexture,
                    new Rect(0, 0, data.characterTexture.width, data.characterTexture.height),
                    new Vector2(0.5f, 0.5f),
                    100f
                );
            }
            
            // Setup controller
            if (controller != null)
            {
                // Configure for multiple character support
                controller.SetCharacterId(data.characterId);
                controller.SetAllowInput(true);
                controller.AutoSetupPhysics();
            }
            
            // Create managed character
            ManagedCharacter managed = new ManagedCharacter(
                data.characterId,
                data.characterName,
                characterObject,
                position,
                data.characterTexture
            );
            
            managed.spriteRenderer = renderer;
            managed.controller = controller;
            
            return managed;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Failed to create managed character: {e.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Get a character from the pool or create new one
    /// </summary>
    private GameObject GetPooledCharacter()
    {
        if (enablePooling && characterPool.Count > 0)
        {
            return characterPool.Dequeue();
        }
        
        // Create new if pool is empty
        return CreatePooledCharacter();
    }
    
    /// <summary>
    /// Get default spawn position when no area system is available
    /// </summary>
    private Vector3 GetDefaultSpawnPosition()
    {
        // Grid-based positioning
        int gridSize = Mathf.CeilToInt(Mathf.Sqrt(maxCharacters));
        int currentIndex = allCharacters.Count;
        
        int row = currentIndex / gridSize;
        int col = currentIndex % gridSize;
        
        float spacing = 3f;
        float startX = -(gridSize - 1) * spacing / 2;
        float startY = -(gridSize - 1) * spacing / 2;
        
        return new Vector3(startX + col * spacing, startY + row * spacing, 0);
    }
    
    /// <summary>
    /// Update character LOD based on distance to camera
    /// </summary>
    private void UpdateCharacterLOD()
    {
        if (mainCamera == null) return;
        
        foreach (var character in activeCharacters.Values)
        {
            if (character.gameObject == null) continue;
            
            float distance = Vector3.Distance(character.gameObject.transform.position, mainCamera.transform.position);
            character.distanceToCamera = distance;
            
            CharacterLODLevel newLOD = CharacterLODLevel.High;
            
            if (distance > veryFarDistanceLOD)
            {
                newLOD = CharacterLODLevel.Invisible;
            }
            else if (distance > farDistanceLOD)
            {
                newLOD = CharacterLODLevel.Low;
            }
            else if (distance > farDistanceLOD * 0.7f)
            {
                newLOD = CharacterLODLevel.Medium;
            }
            
            character.SetLODLevel(newLOD);
        }
    }
    
    /// <summary>
    /// Check for auto cleanup of inactive characters
    /// </summary>
    private void CheckAutoCleanup()
    {
        // Clean up old inactive characters
        if (inactiveCharacters.Count > maxInactiveCharacters)
        {
            var charactersToRemove = inactiveCharacters
                .Where(pair => (Time.time - pair.Value.spawnTime) > autoCleanupTime)
                .OrderBy(pair => pair.Value.spawnTime)
                .Take(inactiveCharacters.Count - maxInactiveCharacters)
                .ToList();
            
            foreach (var pair in charactersToRemove)
            {
                RemoveCharacter(pair.Key);
            }
        }
    }
    
    /// <summary>
    /// Check system performance
    /// </summary>
    private void CheckPerformance()
    {
        // Calculate average frame time over the last minute
        float startTime = Time.time;
        
        // Simple performance check - could be expanded
        int activeCount = activeCharacters.Count;
        float frameTime = Time.deltaTime;
        
        // Update moving average
        averageFrameTime = (averageFrameTime * 0.9f) + (frameTime * 0.1f);
        
        // Report performance
        OnPerformanceUpdate?.Invoke(averageFrameTime);
        
        if (averageFrameTime > 0.05f) // 50ms frame time
        {
            Debug.LogWarning($"⚠️ Performance warning: Frame time {averageFrameTime:F3}s with {activeCount} active characters");
        }
    }
    
    /// <summary>
    /// Remove a character from the system
    /// </summary>
    public void RemoveCharacter(string characterId)
    {
        ManagedCharacter character;
        if (activeCharacters.TryGetValue(characterId, out character))
        {
            // Move to inactive or destroy
            if (enablePooling)
            {
                // Return to pool
                character.gameObject.SetActive(false);
                inactiveCharacters[characterId] = character;
                characterPool.Enqueue(character.gameObject);
            }
            else
            {
                // Destroy completely
                Destroy(character.gameObject);
            }
            
            activeCharacters.Remove(characterId);
            OnCharacterRemoved?.Invoke(characterId);
            
            Debug.Log($"🗑️ Character removed: {character.characterName}");
        }
        else if (inactiveCharacters.TryGetValue(characterId, out character))
        {
            // Remove from inactive
            inactiveCharacters.Remove(characterId);
            if (enablePooling)
            {
                characterPool.Enqueue(character.gameObject);
            }
            else
            {
                Destroy(character.gameObject);
            }
        }
    }
    
    /// <summary>
    /// Activate a specific character
    /// </summary>
    public void ActivateCharacter(string characterId)
    {
        ManagedCharacter character;
        if (inactiveCharacters.TryGetValue(characterId, out character))
        {
            character.SetActive(true);
            activeCharacters[characterId] = character;
            inactiveCharacters.Remove(characterId);
            OnCharacterActivated?.Invoke(characterId);
            
            Debug.Log($"▶️ Character activated: {character.characterName}");
        }
    }
    
    /// <summary>
    /// Deactivate a specific character
    /// </summary>
    public void DeactivateCharacter(string characterId)
    {
        ManagedCharacter character;
        if (activeCharacters.TryGetValue(characterId, out character))
        {
            character.SetActive(false);
            inactiveCharacters[characterId] = character;
            activeCharacters.Remove(characterId);
            OnCharacterDeactivated?.Invoke(characterId);
            
            Debug.Log($"⏸️ Character deactivated: {character.characterName}");
        }
    }
    
    /// <summary>
    /// Get character statistics
    /// </summary>
    public string GetCharacterStatistics()
    {
        return $"Active Characters: {activeCharacters.Count}\n" +
               $"Inactive Characters: {inactiveCharacters.Count}\n" +
               $"Total Created: {totalCharactersCreated}\n" +
               $"Pool Size: {characterPool.Count}\n" +
               $"Average Frame Time: {averageFrameTime:F3}s\n" +
               $"LOD Enabled: {enableLOD}\n" +
               $"Auto Cleanup: {enableAutoCleanup}";
    }
    
    /// <summary>
    /// Clear all characters
    /// </summary>
    public void ClearAllCharacters()
    {
        // Remove all active characters
        var characterIds = activeCharacters.Keys.ToList();
        foreach (string id in characterIds)
        {
            RemoveCharacter(id);
        }
        
        // Clear inactive characters
        foreach (var character in inactiveCharacters.Values)
        {
            if (character.gameObject != null)
            {
                Destroy(character.gameObject);
            }
        }
        inactiveCharacters.Clear();
        
        // Clear pool
        characterPool.Clear();
        if (enablePooling)
        {
            CreateCharacterPool();
        }
        
        allCharacters.Clear();
        totalCharactersCreated = 0;
        
        Debug.Log("🧹 All characters cleared");
        UpdateStatusUI("All characters cleared");
    }
    
    private void OnPipelineCompleted()
    {
        UpdateStatusUI("Pipeline processing completed");
    }
    
    private void UpdateStatusUI(string message)
    {
        OnStatusUpdate?.Invoke(message);
        Debug.Log($"🎭 Character Manager: {message}");
    }
    
    private void OnDestroy()
    {
        // Cleanup
        if (pipeline != null)
        {
            pipeline.OnCharacterCreated -= OnCharacterCreated;
            pipeline.OnPipelineCompleted -= OnPipelineCompleted;
        }
        
        // Clear all data
        ClearAllCharacters();
    }
}