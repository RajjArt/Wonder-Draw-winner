using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class TouchIndicator : MonoBehaviour
{
    [Header("Touch Indicator Settings")]
    [SerializeField] private GameObject indicatorPrefab;
    [SerializeField] private float fadeDuration = 1.0f;
    [SerializeField] private Vector2 indicatorSize = new Vector2(0.5f, 0.5f);
    [SerializeField] private Color indicatorColor = Color.white;
    [SerializeField] private bool useCustomColor = true;
    
    [Header("Pooling Settings")]
    [SerializeField] private int initialPoolSize = 10;
    [SerializeField] private bool expandPoolDynamically = true;
    [SerializeField] private int maxPoolSize = 50;
    
    [Header("Touch Integration")]
    [SerializeField] private bool enableTouchInput = true;
    [SerializeField] private Camera targetCamera;
    
    // Pooling system
    private Queue<GameObject> indicatorPool = new Queue<GameObject>();
    private List<GameObject> activeIndicators = new List<GameObject>();
    private Transform poolParent;
    
    // Touch input tracking
    private Dictionary<int, GameObject> touchIndicators = new Dictionary<int, GameObject>();
    
    private static TouchIndicator instance;
    public static TouchIndicator Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<TouchIndicator>();
                if (instance == null)
                {
                    GameObject go = new GameObject("TouchIndicator");
                    instance = go.AddComponent<TouchIndicator>();
                }
            }
            return instance;
        }
    }
    
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
        
        // Create pool parent
        GameObject poolParentGO = new GameObject("IndicatorPool");
        poolParentGO.transform.SetParent(transform);
        poolParent = poolParentGO.transform;
        
        // Initialize pool
        InitializePool();
    }
    
    private void InitializePool()
    {
        for (int i = 0; i < initialPoolSize; i++)
        {
            GameObject indicator = CreateIndicator();
            ReturnToPool(indicator);
        }
    }
    
    private GameObject CreateIndicator()
    {
        GameObject indicator;
        
        if (indicatorPrefab != null)
        {
            indicator = Instantiate(indicatorPrefab, poolParent);
        }
        else
        {
            // Create default sphere indicator
            indicator = CreateDefaultIndicator();
        }
        
        // Get or add components
        SpriteRenderer spriteRenderer = indicator.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = indicator.AddComponent<SpriteRenderer>();
        
        MeshRenderer meshRenderer = indicator.GetComponent<MeshRenderer>();
        if (meshRenderer == null)
            meshRenderer = indicator.AddComponent<MeshRenderer>();
        
        MeshFilter meshFilter = indicator.GetComponent<MeshFilter>();
        if (meshFilter == null)
            meshFilter = indicator.AddComponent<MeshFilter>();
        
        // Configure indicator
        indicator.name = "TouchIndicator";
        indicator.SetActive(false);
        
        return indicator;
    }
    
    private GameObject CreateDefaultIndicator()
    {
        GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        indicator.transform.SetParent(poolParent);
        
        // Remove collider for performance
        Collider collider = indicator.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);
        
        // Add custom shader setup for fade effect
        Material material = new Material(Shader.Find("Standard"));
        material.SetFloat("_Mode", 3); // Fade mode
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = 3000;
        
        MeshRenderer renderer = indicator.GetComponent<MeshRenderer>();
        renderer.material = material;
        
        return indicator;
    }
    
    private void Update()
    {
        if (!enableTouchInput || targetCamera == null)
            return;
        
        HandleTouchInput();
        HandleMouseInput();
    }
    
    private void HandleTouchInput()
    {
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            
            switch (touch.phase)
            {
                case TouchPhase.Began:
                    ShowTouchIndicator(touch.position, touch.fingerId);
                    break;
                    
                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    HideTouchIndicator(touch.fingerId);
                    break;
            }
        }
    }
    
    private void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            ShowTouchIndicator(Input.mousePosition, -1); // -1 for mouse
        }
        else if (Input.GetMouseButtonUp(0))
        {
            HideTouchIndicator(-1);
        }
    }
    
    public void ShowTouchIndicator(Vector2 screenPosition, int touchId = -1)
    {
        Vector3 worldPosition = ScreenToWorldPosition(screenPosition);
        GameObject indicator = GetIndicatorFromPool();
        
        if (indicator != null)
        {
            SetupIndicator(indicator, worldPosition, touchId);
            StartCoroutine(AnimateAndReturn(indicator, touchId));
        }
    }
    
    public void ShowTouchIndicator(Vector3 worldPosition, int touchId = -1)
    {
        GameObject indicator = GetIndicatorFromPool();
        
        if (indicator != null)
        {
            SetupIndicator(indicator, worldPosition, touchId);
            StartCoroutine(AnimateAndReturn(indicator, touchId));
        }
    }
    
    private void SetupIndicator(GameObject indicator, Vector3 position, int touchId)
    {
        indicator.transform.position = position;
        indicator.transform.localScale = indicatorSize;
        
        // Set color
        if (useCustomColor)
        {
            MeshRenderer meshRenderer = indicator.GetComponent<MeshRenderer>();
            if (meshRenderer != null && meshRenderer.material != null)
            {
                Color color = indicatorColor;
                color.a = 1f; // Start fully opaque
                meshRenderer.material.color = color;
            }
            
            SpriteRenderer spriteRenderer = indicator.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                Color color = indicatorColor;
                color.a = 1f;
                spriteRenderer.color = color;
            }
        }
        
        indicator.SetActive(true);
        
        // Track active touch
        if (!touchIndicators.ContainsKey(touchId))
        {
            touchIndicators[touchId] = indicator;
            activeIndicators.Add(indicator);
        }
    }
    
    private Vector3 ScreenToWorldPosition(Vector2 screenPosition)
    {
        float zDistance = Mathf.Abs(targetCamera.transform.position.z);
        Vector3 worldPosition = targetCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, zDistance));
        return worldPosition;
    }
    
    private IEnumerator AnimateAndReturn(GameObject indicator, int touchId)
    {
        float elapsed = 0f;
        Material indicatorMaterial = indicator.GetComponent<MeshRenderer>()?.material;
        SpriteRenderer spriteRenderer = indicator.GetComponent<SpriteRenderer>();
        
        while (elapsed < fadeDuration && indicator.activeSelf)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;
            float alpha = 1f - t;
            
            if (indicatorMaterial != null)
            {
                Color color = indicatorMaterial.color;
                color.a = alpha;
                indicatorMaterial.color = color;
            }
            
            if (spriteRenderer != null)
            {
                Color color = spriteRenderer.color;
                color.a = alpha;
                spriteRenderer.color = color;
            }
            
            yield return null;
        }
        
        HideTouchIndicator(touchId);
    }
    
    public void HideTouchIndicator(int touchId)
    {
        if (touchIndicators.ContainsKey(touchId))
        {
            GameObject indicator = touchIndicators[touchId];
            touchIndicators.Remove(touchId);
            
            if (activeIndicators.Contains(indicator))
            {
                activeIndicators.Remove(indicator);
            }
            
            ReturnToPool(indicator);
        }
    }
    
    private GameObject GetIndicatorFromPool()
    {
        GameObject indicator = null;
        
        if (indicatorPool.Count > 0)
        {
            indicator = indicatorPool.Dequeue();
        }
        else if (expandPoolDynamically && activeIndicators.Count < maxPoolSize)
        {
            indicator = CreateIndicator();
        }
        
        return indicator;
    }
    
    private void ReturnToPool(GameObject indicator)
    {
        if (indicator != null)
        {
            indicator.SetActive(false);
            indicator.transform.SetParent(poolParent);
            indicatorPool.Enqueue(indicator);
        }
    }
    
    // Public API methods
    public void SetIndicatorColor(Color color)
    {
        indicatorColor = color;
        useCustomColor = true;
    }
    
    public void SetIndicatorSize(Vector2 size)
    {
        indicatorSize = size;
    }
    
    public void SetFadeDuration(float duration)
    {
        fadeDuration = Mathf.Max(0.1f, duration);
    }
    
    public void EnableTouchInput(bool enable)
    {
        enableTouchInput = enable;
    }
    
    public void SetTargetCamera(Camera camera)
    {
        targetCamera = camera;
    }
    
    public void ClearAllIndicators()
    {
        // Stop all coroutines
        StopAllCoroutines();
        
        // Clear active indicators
        foreach (var kvp in touchIndicators.ToList())
        {
            ReturnToPool(kvp.Value);
        }
        
        touchIndicators.Clear();
        activeIndicators.Clear();
    }
    
    public int GetActiveIndicatorCount()
    {
        return activeIndicators.Count;
    }
    
    public int GetPooledIndicatorCount()
    {
        return indicatorPool.Count;
    }
    
    private void OnDestroy()
    {
        // Clean up all indicators
        ClearAllIndicators();
        
        // Clear pool
        foreach (GameObject indicator in indicatorPool)
        {
            if (indicator != null)
            {
                Destroy(indicator);
            }
        }
        indicatorPool.Clear();
    }
    
    // Gizmos for editor visualization
    private void OnDrawGizmosSelected()
    {
        if (activeIndicators.Count > 0)
        {
            Gizmos.color = Color.yellow;
            foreach (GameObject indicator in activeIndicators)
            {
                if (indicator != null && indicator.activeSelf)
                {
                    Gizmos.DrawWireSphere(indicator.transform.position, indicatorSize.x * 0.5f);
                }
            }
        }
    }
}

// Optional: TouchIndicatorManager for advanced touch management
public class TouchIndicatorManager : MonoBehaviour
{
    [SerializeField] private TouchIndicator touchIndicator;
    
    public void InitializeCustomIndicator(GameObject prefab, Color color, float size, float fadeDuration)
    {
        if (touchIndicator == null)
            touchIndicator = TouchIndicator.Instance;
        
        if (touchIndicator != null)
        {
            touchIndicator.SetIndicatorColor(color);
            touchIndicator.SetIndicatorSize(new Vector2(size, size));
            touchIndicator.SetFadeDuration(fadeDuration);
        }
    }
    
    public void ShowIndicatorAtScreenPosition(Vector2 screenPosition)
    {
        if (touchIndicator == null)
            touchIndicator = TouchIndicator.Instance;
        
        if (touchIndicator != null)
        {
            touchIndicator.ShowTouchIndicator(screenPosition);
        }
    }
    
    public void ShowIndicatorAtWorldPosition(Vector3 worldPosition)
    {
        if (touchIndicator == null)
            touchIndicator = TouchIndicator.Instance;
        
        if (touchIndicator != null)
        {
            touchIndicator.ShowTouchIndicator(worldPosition);
        }
    }
}