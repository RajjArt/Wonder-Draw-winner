using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.U2D;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Pixel-perfect rendering configuration for 2D games
/// Ensures crisp sprite rendering and prevents sub-pixel blur
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public class PixelPerfectRendering : MonoBehaviour
{
    [Header("🎯 Pixel Perfect Settings")]
    [Tooltip("Enable pixel-perfect rendering")]
    public bool enablePixelPerfect = true;
    
    [Tooltip("Pixels per unit for your sprites")]
    public float pixelsPerUnit = 100f;
    
    [Tooltip("Scale factor for all sprites")]
    public float spriteScale = 1f;
    
    [Header("🔧 Rendering Quality")]
    [Tooltip("Texture filter mode")]
    public FilterMode filterMode = FilterMode.Point;
    
    [Tooltip("Texture compression setting")]
    public TextureImporterCompression compression = TextureImporterCompression.Uncompressed;
    
    [Tooltip("Max texture size for optimal performance")]
    public int maxTextureSize = 1024;
    
    [Header("📐 Camera Settings")]
    [Tooltip("Camera orthographic size for pixel-perfect view")]
    public float cameraOrthographicSize = 5f;
    
    [Tooltip("Zoom level multiplier")]
    public float zoomMultiplier = 1f;
    
    [Header("🎮 UI Scaling")]
    [Tooltip("UI scale mode")]
    public UIScaleMode uiScaleMode = UIScaleMode.ScaleWithScreenSize;
    
    [Tooltip("Reference resolution for UI")]
    public Vector2 referenceResolution = new Vector2(1920, 1080);
    
    [Tooltip("Match width or height for UI scaling")]
    [Range(0f, 1f)]
    public float uiMatchMode = 0.5f;
    
    [Header("🔍 Performance Settings")]
    [Tooltip("Enable mipmaps for textures")]
    public bool enableMipmaps = false;
    
    [Tooltip("Enable texture streaming")]
    public bool enableTextureStreaming = true;
    
    [Tooltip("Texture streaming mip bias")]
    [Range(-2f, 2f)]
    public float textureStreamingMipBias = 0f;
    
    private static PixelPerfectRendering instance;
    
    public static PixelPerfectRendering Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<PixelPerfectRendering>();
            }
            return instance;
        }
    }
    
    private void Start()
    {
        ApplyPixelPerfectSettings();
    }
    
    private void Update()
    {
        if (enablePixelPerfect)
        {
            MaintainPixelPerfectRendering();
        }
    }
    
    public void ApplyPixelPerfectSettings()
    {
        // Setup main camera for pixel-perfect rendering
        var mainCamera = Camera.main;
        if (mainCamera != null)
        {
            mainCamera.orthographic = true;
            mainCamera.orthographicSize = cameraOrthographicSize * zoomMultiplier;
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
            mainCamera.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 1f);
            
            // Enable pixel-perfect camera
            var pixelPerfect = mainCamera.GetComponent<UnityEngine.U2D.PixelPerfectCamera>();
            if (pixelPerfect == null)
            {
                pixelPerfect = mainCamera.gameObject.AddComponent<UnityEngine.U2D.PixelPerfectCamera>();
            }
            
            pixelPerfect.assetsPPU = (int)pixelsPerUnit;
            // Note: zoom and cropFrame properties not available in this version of Unity
            // Use camera.orthographicSize for zoom control
            // pixelPerfect.zoom = 1;
            // pixelPerfect.cropFrame = new Vector2(0, 0);
            pixelPerfect.cropFrameX = false;
            pixelPerfect.cropFrameY = false;
        }
        
        // Setup UI canvas for pixel-perfect scaling
        SetupUICanvas();
        
        // Configure all sprite renderers
        ConfigureSpriteRenderers();
        
        Debug.Log("Pixel-perfect settings applied successfully");
    }
    
    private void SetupUICanvas()
    {
        var canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            // Create UI canvas if none exists
            var canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }
        
        var canvasScaler = canvas.GetComponent<CanvasScaler>();
        if (canvasScaler != null)
        {
            canvasScaler.uiScaleMode = (CanvasScaler.ScaleMode)uiScaleMode;
            canvasScaler.referenceResolution = referenceResolution;
            canvasScaler.screenMatchMode = (CanvasScaler.ScreenMatchMode)uiMatchMode;
        }
        
        var targetCanvas = FindFirstObjectByType<Canvas>();
        if (targetCanvas != null)
        {
            var scaler = targetCanvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = (CanvasScaler.ScaleMode)uiScaleMode;
                scaler.referenceResolution = referenceResolution;
                scaler.matchWidthOrHeight = uiMatchMode;
            }
        }
    }
    
    private void ConfigureSpriteRenderers()
    {
#if UNITY_2023_1_OR_NEWER
        var spriteRenderers = Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
#else
        var spriteRenderers = FindObjectsOfType<SpriteRenderer>();
#endif
        foreach (var spriteRenderer in spriteRenderers)
        {
            if (spriteRenderer != null)
            {
                // Ensure proper sprite settings
                spriteRenderer.sortingOrder = 0;
                // Note: spriteSortMode property not available on SpriteRenderer
                // spriteRenderer.spriteSortMode = SpriteSortMode.Pivot;
                spriteRenderer.sprite = spriteRenderer.sprite; // Force refresh
            }
        }
    }
    
    private void MaintainPixelPerfectRendering()
    {
        var mainCamera = Camera.main;
        if (mainCamera == null) return;
        
        // Snap camera to pixel grid
        Vector3 snappedPosition = mainCamera.transform.position;
        float pixelSize = 1f / pixelsPerUnit;
        snappedPosition.x = Mathf.Round(snappedPosition.x / pixelSize) * pixelSize;
        snappedPosition.y = Mathf.Round(snappedPosition.y / pixelSize) * pixelSize;
        mainCamera.transform.position = snappedPosition;
    }
    
    [ContextMenu("Optimize All Textures")]
    public void OptimizeAllTextures()
    {
        var textureImporters = AssetDatabase.FindAssets("t:Texture2D");
        int optimizedCount = 0;
        
        foreach (string guid in textureImporters)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            
            if (importer != null)
            {
                importer.filterMode = filterMode;
                importer.textureCompression = compression;
                importer.maxTextureSize = maxTextureSize;
                importer.mipmapEnabled = enableMipmaps;
                
                // Force texture to use the correct import settings
                AssetDatabase.ImportAsset(path);
                optimizedCount++;
            }
        }
        
        Debug.Log($"Optimized {optimizedCount} textures for pixel-perfect rendering");
    }
    
    [ContextMenu("Apply Pixel Perfect Settings")]
    public void ApplyPixelPerfectSettingsContext()
    {
        ApplyPixelPerfectSettings();
    }
    
    [ContextMenu("Create Test Scene")]
    public void CreateTestScene()
    {
        // Create a simple test scene to verify pixel-perfect rendering
        var testSprite = CreateTestSprite();
        if (testSprite != null)
        {
            var testGO = new GameObject("Test Sprite");
            var spriteRenderer = testGO.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = testSprite;
            spriteRenderer.sortingOrder = 1;
            
            Debug.Log("Test scene created - verify pixel-perfect rendering");
        }
    }
    
    private Sprite CreateTestSprite()
    {
        // Create a simple test texture
        var texture = new Texture2D(32, 32);
        texture.filterMode = filterMode;
        texture.wrapMode = TextureWrapMode.Clamp;
        
        // Create a simple checkerboard pattern
        for (int x = 0; x < 32; x++)
        {
            for (int y = 0; y < 32; y++)
            {
                if ((x % 4 == 0) ^ (y % 4 == 0))
                    texture.SetPixel(x, y, Color.white);
                else
                    texture.SetPixel(x, y, Color.black);
            }
        }
        
        texture.Apply();
        
        // Create sprite
        return Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), pixelsPerUnit);
    }
    
    private void OnValidate()
    {
        if (enablePixelPerfect && Application.isEditor)
        {
            ApplyPixelPerfectSettings();
        }
    }
}

public enum UIScaleMode
{
    ConstantPixelSize,
    ScaleWithScreenSize,
    ConstantPhysicalSize
}
