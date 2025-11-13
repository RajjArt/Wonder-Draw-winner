using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Character Area Positioning System - Complete System: Manages designated spawn areas for multiple characters
/// Provides intelligent area assignment, grid-based layout, and dynamic expansion for runtime character management
/// Integrates with MultipleCharacterManager for optimal character placement
/// </summary>
public class CharacterAreaPositioning : MonoBehaviour
{
    [Header("Area Configuration")]
    [SerializeField] private Vector2 areaCenter = Vector2.zero;
    [SerializeField] private Vector2 areaSize = new Vector2(20f, 12f);
    [SerializeField] private int maxColumns = 6;
    [SerializeField] private int maxRows = 4;
    [SerializeField] private float characterSpacing = 3f;
    [SerializeField] private float edgePadding = 1f;
    
    [Header("Layout Options")]
    [SerializeField] private bool enableGridLayout = true;
    [SerializeField] private bool enableDynamicExpansion = true;
    [SerializeField] private CharacterDirection defaultDirection = CharacterDirection.Right;
    
    [Header("Visualization")]
    [SerializeField] private bool showAreaBounds = true;
    [SerializeField] private bool showGridLines = true;
    [SerializeField] private Color areaColor = new Color(0, 1, 0, 0.2f);
    [SerializeField] private Color gridColor = new Color(0, 1, 0, 0.3f);
    [SerializeField] private Color availableColor = new Color(0, 0, 1, 0.5f);
    [SerializeField] private Color occupiedColor = new Color(1, 0, 0, 0.5f);
    
    [Header("Integration")]
    [SerializeField] private MultipleCharacterManager characterManager;
    [SerializeField] private Camera mainCamera;
    
    // Area management
    private List<CharacterSlot> availableSlots = new List<CharacterSlot>();
    private List<CharacterSlot> occupiedSlots = new List<CharacterSlot>();
    private int currentSlotIndex = 0;
    private Dictionary<string, CharacterSlot> characterSlots = new Dictionary<string, CharacterSlot>();
    
    // Grid management
    private Vector2[,] gridPositions;
    private bool[,] gridOccupied;
    private int currentGridRow = 0;
    private int currentGridCol = 0;
    
    // Expansion management
    private bool isExpanded = false;
    private int expansionLevel = 0;
    private List<Vector2> expansionAreas = new List<Vector2>();
    
    // Event system
    public System.Action<Vector3> OnSpawnPositionAssigned;
    public System.Action<string, Vector3> OnCharacterPositioned;
    public System.Action OnAreaExpanded;
    public System.Action<string> OnStatusUpdate;
    
    // Character direction enum
    public enum CharacterDirection
    {
        Up, Down, Left, Right,
        UpRight, UpLeft, DownRight, DownLeft,
        Random
    }
    
    // Character slot data structure
    [System.Serializable]
    public class CharacterSlot
    {
        public Vector2 position;
        public bool isOccupied;
        public string characterId;
        public float assignmentTime;
        public CharacterDirection direction;
        public int gridRow;
        public int gridCol;
        
        public CharacterSlot(Vector2 pos, int row, int col, CharacterDirection dir)
        {
            position = pos;
            gridRow = row;
            gridCol = col;
            direction = dir;
            isOccupied = false;
            characterId = "";
            assignmentTime = 0f;
        }
    }
    
    // Properties
    public int TotalSlots => availableSlots.Count + occupiedSlots.Count;
    public int AvailableSlots => availableSlots.Count;
    public int OccupiedSlots => occupiedSlots.Count;
    public int MaxCapacity => maxRows * maxColumns;
    public float AreaUtilization => (float)occupiedSlots.Count / MaxCapacity;
    public string PositioningStatus => GetCurrentStatus();
    
    private string GetCurrentStatus()
    {
        return $"Slots: {occupiedSlots.Count}/{MaxCapacity} | Expansion: {expansionLevel} | Grid: {currentGridRow},{currentGridCol}";
    }
    
    private void Awake()
    {
        // Auto-find components if not assigned
        if (characterManager == null)
            characterManager = FindFirstObjectByType<MultipleCharacterManager>();
        if (mainCamera == null)
            mainCamera = Camera.main;
    }
    
    private void Start()
    {
        InitializePositioningSystem();
        Debug.Log("🎯 Character Area Positioning System initialized");
    }
    
    private void Update()
    {
        // Update slot utilization
        UpdateSlotUtilization();
        
        // Check for dynamic expansion
        if (enableDynamicExpansion && AreaUtilization > 0.8f)
        {
            CheckForExpansion();
        }
    }
    
    /// <summary>
    /// Initialize the positioning system
    /// </summary>
    private void InitializePositioningSystem()
    {
        CreateInitialGrid();
        CreateExpansionAreas();
        UpdateSlotUtilization();
        
        UpdateStatusUI("Positioning system initialized");
    }
    
    /// <summary>
    /// Create the initial grid of character slots
    /// </summary>
    private void CreateInitialGrid()
    {
        // Create grid arrays
        gridPositions = new Vector2[maxRows, maxColumns];
        gridOccupied = new bool[maxRows, maxColumns];
        
        // Calculate grid center and spacing
        float centerX = areaCenter.x;
        float centerY = areaCenter.y;
        float totalWidth = (maxColumns - 1) * characterSpacing;
        float totalHeight = (maxRows - 1) * characterSpacing;
        
        float startX = centerX - totalWidth / 2;
        float startY = centerY - totalHeight / 2;
        
        // Create slots for each grid position
        availableSlots.Clear();
        
        for (int row = 0; row < maxRows; row++)
        {
            for (int col = 0; col < maxColumns; col++)
            {
                float x = startX + col * characterSpacing;
                float y = startY + row * characterSpacing;
                
                // Check if position is within area bounds
                if (IsPositionInBounds(new Vector2(x, y)))
                {
                    Vector2 position = new Vector2(x, y);
                    gridPositions[row, col] = position;
                    gridOccupied[row, col] = false;
                    
                    CharacterDirection direction = GetDirectionForPosition(row, col);
                    CharacterSlot slot = new CharacterSlot(position, row, col, direction);
                    availableSlots.Add(slot);
                }
            }
        }
        
        Debug.Log($"📐 Created initial grid: {availableSlots.Count} slots in {maxRows}x{maxColumns} grid");
    }
    
    /// <summary>
    /// Create expansion areas for dynamic growth
    /// </summary>
    private void CreateExpansionAreas()
    {
        expansionAreas.Clear();
        
        // Define expansion rings around the main area
        for (int ring = 1; ring <= 3; ring++)
        {
            float expansionSize = areaSize.magnitude * 0.3f * ring;
            Vector2 expansionCenter = areaCenter;
            
            // Add expansion area
            expansionAreas.Add(expansionCenter);
        }
    }
    
    /// <summary>
    /// Get the next available spawn position
    /// </summary>
    public Vector3 GetNextSpawnPosition()
    {
        if (availableSlots.Count == 0)
        {
            if (enableDynamicExpansion)
            {
                ExpandArea();
                if (availableSlots.Count == 0)
                {
                    Debug.LogWarning("⚠️ No available positions even after expansion");
                    return GetRandomFallbackPosition();
                }
            }
            else
            {
                Debug.LogWarning("⚠️ No available positions - area full");
                return GetRandomFallbackPosition();
            }
        }
        
        // Get next available slot
        CharacterSlot slot = availableSlots[currentSlotIndex];
        currentSlotIndex = (currentSlotIndex + 1) % availableSlots.Count;
        
        // Move to occupied
        slot.isOccupied = true;
        slot.assignmentTime = Time.time;
        availableSlots.Remove(slot);
        occupiedSlots.Add(slot);
        
        // Create 3D position (2D game with Z=0)
        Vector3 spawnPosition = new Vector3(slot.position.x, slot.position.y, 0);
        
        OnSpawnPositionAssigned?.Invoke(spawnPosition);
        UpdateStatusUI($"Position assigned: {spawnPosition}");
        
        return spawnPosition;
    }
    
    /// <summary>
    /// Get spawn position for a specific character
    /// </summary>
    public Vector3 GetSpawnPositionForCharacter(string characterId)
    {
        Vector3 position = GetNextSpawnPosition();
        characterSlots[characterId] = new CharacterSlot(new Vector2(position.x, position.y), currentGridRow, currentGridCol, defaultDirection);
        OnCharacterPositioned?.Invoke(characterId, position);
        return position;
    }
    
    /// <summary>
    /// Reserve a specific position for a character
    /// </summary>
    public bool ReservePosition(string characterId, Vector2 position)
    {
        // Find available slot closest to requested position
        CharacterSlot closestSlot = null;
        float closestDistance = float.MaxValue;
        
        foreach (var slot in availableSlots)
        {
            float distance = Vector2.Distance(slot.position, position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestSlot = slot;
            }
        }
        
        if (closestSlot != null && closestDistance <= characterSpacing)
        {
            // Reserve the slot
            closestSlot.isOccupied = true;
            closestSlot.characterId = characterId;
            closestSlot.assignmentTime = Time.time;
            
            availableSlots.Remove(closestSlot);
            occupiedSlots.Add(closestSlot);
            characterSlots[characterId] = closestSlot;
            
            Vector3 spawnPosition = new Vector3(closestSlot.position.x, closestSlot.position.y, 0);
            OnCharacterPositioned?.Invoke(characterId, spawnPosition);
            
            Debug.Log($"📍 Reserved position for {characterId}: {spawnPosition}");
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Release a character's position
    /// </summary>
    public void ReleasePosition(string characterId)
    {
        if (characterSlots.TryGetValue(characterId, out CharacterSlot slot))
        {
            // Make slot available again
            slot.isOccupied = false;
            slot.characterId = "";
            slot.assignmentTime = 0f;
            
            occupiedSlots.Remove(slot);
            availableSlots.Add(slot);
            characterSlots.Remove(characterId);
            
            // Re-sort available slots by assignment time (oldest first)
            availableSlots = availableSlots.OrderBy(s => s.assignmentTime).ToList();
            
            UnityEngine.Debug.Log($"🔓 Released position for {characterId}");
        }
    }
    
    /// <summary>
    /// Expand the area to accommodate more characters
    /// </summary>
    private void ExpandArea()
    {
        if (expansionLevel >= expansionAreas.Count)
        {
            Debug.LogWarning("⚠️ Maximum expansion reached");
            return;
        }
        
        expansionLevel++;
        isExpanded = true;
        
        // Calculate expanded area
        float expansionFactor = 1f + (expansionLevel * 0.3f);
        Vector2 expandedCenter = expansionAreas[expansionLevel - 1];
        Vector2 expandedSize = areaSize * expansionFactor;
        
        // Create new slots in expanded area
        CreateExpansionSlots(expandedCenter, expandedSize);
        
        OnAreaExpanded?.Invoke();
        UpdateStatusUI($"Area expanded to level {expansionLevel}");
        
        Debug.Log($"📈 Area expanded: Level {expansionLevel}, {availableSlots.Count} new slots added");
    }
    
    /// <summary>
    /// Create slots in expansion area
    /// </summary>
    private void CreateExpansionSlots(Vector2 center, Vector2 size)
    {
        int additionalSlots = maxColumns * 2; // Add 2 rows worth of slots
        
        for (int i = 0; i < additionalSlots; i++)
        {
            float x = center.x + Random.Range(-size.x/2, size.x/2);
            float y = center.y + Random.Range(-size.y/2, size.y/2);
            
            Vector2 position = new Vector2(x, y);
            
            if (IsPositionInBounds(position) && !IsPositionOccupied(position))
            {
                CharacterDirection direction = (CharacterDirection)Random.Range(0, 8);
                CharacterSlot slot = new CharacterSlot(position, -1, -1, direction); // -1 indicates expansion area
                availableSlots.Add(slot);
            }
        }
    }
    
    /// <summary>
    /// Check if position is within area bounds
    /// </summary>
    private bool IsPositionInBounds(Vector2 position)
    {
        float minX = areaCenter.x - areaSize.x / 2 + edgePadding;
        float maxX = areaCenter.x + areaSize.x / 2 - edgePadding;
        float minY = areaCenter.y - areaSize.y / 2 + edgePadding;
        float maxY = areaCenter.y + areaSize.y / 2 - edgePadding;
        
        return position.x >= minX && position.x <= maxX &&
               position.y >= minY && position.y <= maxY;
    }
    
    /// <summary>
    /// Check if position is already occupied
    /// </summary>
    private bool IsPositionOccupied(Vector2 position)
    {
        foreach (var slot in occupiedSlots)
        {
            if (Vector2.Distance(slot.position, position) < characterSpacing * 0.8f)
            {
                return true;
            }
        }
        return false;
    }
    
    /// <summary>
    /// Get direction for grid position
    /// </summary>
    private CharacterDirection GetDirectionForPosition(int row, int col)
    {
        if (!enableGridLayout) return defaultDirection;
        
        int centerRow = maxRows / 2;
        int centerCol = maxColumns / 2;
        
        if (row < centerRow && col == centerCol) return CharacterDirection.Up;
        if (row > centerRow && col == centerCol) return CharacterDirection.Down;
        if (col < centerCol && row == centerRow) return CharacterDirection.Left;
        if (col > centerCol && row == centerRow) return CharacterDirection.Right;
        if (row < centerRow && col < centerCol) return CharacterDirection.UpLeft;
        if (row < centerRow && col > centerCol) return CharacterDirection.UpRight;
        if (row > centerRow && col < centerCol) return CharacterDirection.DownLeft;
        if (row > centerRow && col > centerCol) return CharacterDirection.DownRight;
        
        return defaultDirection;
    }
    
    /// <summary>
    /// Get a random fallback position when no slots are available
    /// </summary>
    private Vector3 GetRandomFallbackPosition()
    {
        float x = Random.Range(areaCenter.x - areaSize.x / 2, areaCenter.x + areaSize.x / 2);
        float y = Random.Range(areaCenter.y - areaSize.y / 2, areaCenter.y + areaSize.y / 2);
        return new Vector3(x, y, 0);
    }
    
    /// <summary>
    /// Check if expansion is needed
    /// </summary>
    private void CheckForExpansion()
    {
        if (availableSlots.Count < 2) // Less than 2 slots remaining
        {
            ExpandArea();
        }
    }
    
    /// <summary>
    /// Update slot utilization display
    /// </summary>
    private void UpdateSlotUtilization()
    {
        float utilization = AreaUtilization;
        if (utilization > 0.9f)
        {
            UpdateStatusUI("Area nearly full - consider expansion");
        }
    }
    
    /// <summary>
    /// Get positioning statistics
    /// </summary>
    public string GetPositioningStatistics()
    {
        return $"Total Slots: {TotalSlots}\n" +
               $"Available: {AvailableSlots}\n" +
               $"Occupied: {OccupiedSlots}\n" +
               $"Utilization: {AreaUtilization:P1}\n" +
               $"Expansion Level: {expansionLevel}\n" +
               $"Grid Size: {maxRows}x{maxColumns}\n" +
               $"Area Size: {areaSize.x}x{areaSize.y}";
    }
    
    /// <summary>
    /// Reset positioning system
    /// </summary>
    public void ResetPositioningSystem()
    {
        // Clear all data
        availableSlots.Clear();
        occupiedSlots.Clear();
        characterSlots.Clear();
        currentSlotIndex = 0;
        expansionLevel = 0;
        isExpanded = false;
        
        // Recreate initial grid
        CreateInitialGrid();
        CreateExpansionAreas();
        
        Debug.Log("🔄 Positioning system reset");
        UpdateStatusUI("Positioning system reset");
    }
    
    private void UpdateStatusUI(string message)
    {
        OnStatusUpdate?.Invoke(message);
        Debug.Log($"🎯 Positioning: {message}");
    }
    
    /// <summary>
    /// Visualize the positioning system in the editor
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!showAreaBounds) return;
        
        // Draw area bounds
        Gizmos.color = areaColor;
        Vector3 center = new Vector3(areaCenter.x, areaCenter.y, 0);
        Vector3 size = new Vector3(areaSize.x, areaSize.y, 0);
        Gizmos.DrawWireCube(center, size);
        
        // Draw grid lines
        if (showGridLines && Application.isPlaying)
        {
            Gizmos.color = gridColor;
            
            // Draw available slots
            foreach (var slot in availableSlots)
            {
                Gizmos.DrawWireCube(new Vector3(slot.position.x, slot.position.y, 0), Vector3.one * 0.5f);
            }
            
            // Draw occupied slots
            Gizmos.color = occupiedColor;
            foreach (var slot in occupiedSlots)
            {
                Gizmos.DrawCube(new Vector3(slot.position.x, slot.position.y, 0), Vector3.one * 0.5f);
            }
        }
        
        // Draw expansion areas
        if (enableDynamicExpansion)
        {
            Gizmos.color = new Color(1, 1, 0, 0.1f);
            foreach (var expansionArea in expansionAreas)
            {
                Gizmos.DrawWireCube(new Vector3(expansionArea.x, expansionArea.y, 0), areaSize * 1.3f);
            }
        }
    }
}