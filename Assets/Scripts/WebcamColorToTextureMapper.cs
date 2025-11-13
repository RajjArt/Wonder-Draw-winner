using UnityEngine;

public class WebcamColorToTextureMapper : MonoBehaviour
{
    private WebCamTexture webcamTexture;
    private Texture2D colorTexture;
    private Material modelMaterial;
    private Mesh modelMesh;
    
    // Texture resolution (512x512 is a good balance)
    private int textureWidth = 512;
    private int textureHeight = 512;
    
    void Start()
    {
        // Get model components
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            modelMaterial = renderer.material;
        }
        
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter != null)
        {
            modelMesh = meshFilter.mesh;
        }
        
        // Create color texture
        colorTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
        colorTexture.filterMode = FilterMode.Bilinear;
        
        // Start camera
        StartWebcam();
    }
    
    void StartWebcam()
    {
        webcamTexture = new WebCamTexture();
        webcamTexture.Play();
    }
    
    void Update()
    {
        // S = Start camera
        if (Input.GetKeyDown(KeyCode.S))
        {
            if (!webcamTexture.isPlaying)
            {
                StartWebcam();
                Debug.Log("📹 Camera started");
            }
        }
        
        // C = Capture and map colors to texture
        if (Input.GetKeyDown(KeyCode.C))
        {
            MapWebcamToTexture();
        }
        
        // A = Apply texture to material
        if (Input.GetKeyDown(KeyCode.A))
        {
            ApplyTextureToMaterial();
        }
        
        // R = Reset to default
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetMaterial();
        }
    }
    
    void MapWebcamToTexture()
    {
        if (webcamTexture == null || !webcamTexture.isPlaying || webcamTexture.width < 16)
        {
            Debug.LogWarning("⚠️ Camera not ready");
            return;
        }
        
        if (modelMesh == null)
        {
            Debug.LogError("❌ No mesh found");
            return;
        }
        
        // Get mesh bounds for coordinate mapping
        Bounds bounds = modelMesh.bounds;
        Vector3[] vertices = modelMesh.vertices;
        
        // Create color array for texture
        Color[] colors = new Color[textureWidth * textureHeight];
        
        Debug.Log($"🎨 Mapping {vertices.Length} vertices to {textureWidth}x{textureHeight} texture");
        
        // For each vertex, find the corresponding color from webcam
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 vertex = vertices[i];
            
            // Handle tiny models (our scaling solution)
            float scalingFactor = (bounds.size.magnitude < 0.0001f) ? 100f : 1f;
            Vector3 scaledVertex = vertex * scalingFactor;
            
            // Normalize vertex position (0 to 1 range)
            float normalizedX = Mathf.InverseLerp(bounds.min.x * scalingFactor, bounds.max.x * scalingFactor, scaledVertex.x);
            float normalizedY = Mathf.InverseLerp(bounds.min.z * scalingFactor, bounds.max.z * scalingFactor, scaledVertex.z);
            
            // Clamp to valid range
            normalizedX = Mathf.Clamp01(normalizedX);
            normalizedY = Mathf.Clamp01(normalizedY);
            
            // Convert to pixel coordinates
            int pixelX = Mathf.FloorToInt(normalizedX * (textureWidth - 1));
            int pixelY = Mathf.FloorToInt(normalizedY * (textureHeight - 1));
            
            // Get color from webcam
            Color vertexColor = GetWebcamColor(pixelX, pixelY);
            
            // Apply this color to all vertices that map to this pixel area
            ApplyColorToPixelArea(colors, pixelX, pixelY, vertexColor);
        }
        
        // Apply colors to texture
        colorTexture.SetPixels(colors);
        colorTexture.Apply();
        
        Debug.Log("✅ Color mapping completed");
    }
    
    Color GetWebcamColor(int pixelX, int pixelY)
    {
        if (webcamTexture == null) return Color.white;
        
        // Convert texture coordinates to webcam coordinates
        int webcamX = Mathf.FloorToInt((float)pixelX / textureWidth * webcamTexture.width);
        int webcamY = Mathf.FloorToInt((float)pixelY / textureHeight * webcamTexture.height);
        
        // Clamp webcam coordinates
        webcamX = Mathf.Clamp(webcamX, 0, webcamTexture.width - 1);
        webcamY = Mathf.Clamp(webcamY, 0, webcamTexture.height - 1);
        
        // Get pixel from webcam
        return webcamTexture.GetPixel(webcamX, webcamY);
    }
    
    void ApplyColorToPixelArea(Color[] colors, int centerX, int centerY, Color color)
    {
        int radius = 1; // Expand to neighboring pixels for better quality
        
        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                int colorX = centerX + x;
                int colorY = centerY + y;
                
                if (colorX >= 0 && colorX < textureWidth && colorY >= 0 && colorY < textureHeight)
                {
                    int index = colorY * textureWidth + colorX;
                    colors[index] = color;
                }
            }
        }
    }
    
    void ApplyTextureToMaterial()
    {
        if (colorTexture != null && modelMaterial != null)
        {
            modelMaterial.mainTexture = colorTexture;
            Debug.Log("🖼️ Texture applied to material");
        }
        else
        {
            Debug.LogWarning("⚠️ Cannot apply texture - missing components");
        }
    }
    
    void ResetMaterial()
    {
        if (modelMaterial != null)
        {
            modelMaterial.mainTexture = null;
            Debug.Log("🔄 Material reset");
        }
    }
    
    void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 400, 30), "COLOR → TEXTURE MAPPER");
        GUI.Label(new Rect(10, 30, 400, 20), "S = Start Camera | C = Map Colors | A = Apply | R = Reset");
        
        if (webcamTexture != null && webcamTexture.isPlaying)
        {
            GUI.Label(new Rect(10, 50, 400, 20), $"📹 Camera: {webcamTexture.width}x{webcamTexture.height}");
        }
        
        if (colorTexture != null)
        {
            GUI.Label(new Rect(10, 70, 400, 20), $"🎨 Texture: {colorTexture.width}x{colorTexture.height}");
        }
    }
    
    void OnDestroy()
    {
        if (webcamTexture != null)
        {
            webcamTexture.Stop();
        }
        
        if (colorTexture != null)
        {
            Destroy(colorTexture);
        }
    }
}
