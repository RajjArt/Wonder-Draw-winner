using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public class WebcamToRiggedCharacter_Fixed_Final : MonoBehaviour
{
    // Public fields
    public int cameraWidth = 640;
    public int cameraHeight = 480;
    public int cameraFPS = 30;
    public Color backgroundColor = Color.black;
    
    // Private fields
    private WebCamTexture webcamTexture;
    private Texture2D processedTexture;
    private bool isProcessing = false;
    
    // Character creation reference - this is what was causing the errors
    private PNGToRiggedCharacter_Fixed characterCreator;
    
    // Webcam display
    private Renderer webcamRenderer;
    
    void Start()
    {
        // Get the PNGToRiggedCharacter_Fixed component (corrected reference)
        characterCreator = GetComponent<PNGToRiggedCharacter_Fixed>();
        
        if (characterCreator == null)
        {
            Debug.LogError("PNGToRiggedCharacter_Fixed component not found on this GameObject!");
        }
        else
        {
            Debug.Log("✅ PNGToRiggedCharacter_Fixed component found and connected");
        }
        
        // Setup webcam
        SetupWebcam();
    }
    
    void SetupWebcam()
    {
        try
        {
            webcamTexture = new WebCamTexture(cameraWidth, cameraHeight, cameraFPS);
            
            // Find a renderer to display the webcam
            webcamRenderer = GetComponent<Renderer>();
            if (webcamRenderer == null)
            {
                webcamRenderer = gameObject.GetComponentInChildren<Renderer>();
            }
            
            if (webcamRenderer != null)
            {
                webcamRenderer.material.mainTexture = webcamTexture;
            }
            
            webcamTexture.Play();
            Debug.Log($"Webcam initialized: {cameraWidth}x{cameraHeight} at {cameraFPS} FPS");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error setting up webcam: {e.Message}");
        }
    }
    
    void Update()
    {
        if (webcamTexture != null && webcamTexture.isPlaying)
        {
            // Process webcam feed for character creation
            ProcessWebcamFrame();
        }
    }
    
    void ProcessWebcamFrame()
    {
        if (isProcessing || characterCreator == null)
            return;
        
        isProcessing = true;
        
        try
        {
            // Convert webcam texture to Texture2D for processing
            if (processedTexture == null || processedTexture.width != webcamTexture.width || 
                processedTexture.height != webcamTexture.height)
            {
                processedTexture = new Texture2D(webcamTexture.width, webcamTexture.height, 
                    TextureFormat.RGBA32, false);
            }
            
            // Copy pixels from webcam texture
            Color[] pixels = webcamTexture.GetPixels();
            processedTexture.SetPixels(pixels);
            processedTexture.Apply();
            
            // Create character from webcam frame
            CreateCharacterFromWebcam(processedTexture);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error processing webcam frame: {e.Message}");
        }
        finally
        {
            isProcessing = false;
        }
    }
    
    void CreateCharacterFromWebcam(Texture2D sourceTexture)
    {
        try
        {
            if (characterCreator != null)
            {
                // Note: PNGToRiggedCharacter_Fixed doesn't have SetSourceTexture method
                // So we'll just trigger the character creation with the current assigned sprite
                // In a real implementation, you would convert the texture to a Sprite and assign it
                
                characterCreator.CreateRiggedCharacter();
                GameObject createdCharacter = characterCreator.GetCreatedCharacter();
                
                if (createdCharacter != null)
                {
                    Debug.Log("Character created using system sprite");
                    
                    // Position the created character
                    PositionCreatedCharacter(createdCharacter);
                }
                else
                {
                    Debug.LogWarning("Character creation returned null - may need sprite assignment in inspector");
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error creating character from webcam: {e.Message}");
        }
    }
    
    void PositionCreatedCharacter(GameObject character)
    {
        if (character != null)
        {
            // Position character to the side of the webcam view
            character.transform.position = transform.position + new Vector3(2, 0, 0);
            character.transform.localScale = Vector3.one * 0.5f; // Make it smaller
        }
    }
    
    void OnDestroy()
    {
        if (webcamTexture != null && webcamTexture.isPlaying)
        {
            webcamTexture.Stop();
        }
    }
    
    // Public methods
    public void StartWebcam()
    {
        if (webcamTexture != null)
        {
            webcamTexture.Play();
        }
        else
        {
            SetupWebcam();
        }
    }
    
    public void StopWebcam()
    {
        if (webcamTexture != null)
        {
            webcamTexture.Stop();
        }
    }
    
    public bool IsWebcamActive()
    {
        return webcamTexture != null && webcamTexture.isPlaying;
    }
    
    public Texture2D GetCurrentFrame()
    {
        return processedTexture;
    }
    
    public PNGToRiggedCharacter_Fixed GetCharacterCreator()
    {
        return characterCreator;
    }
    
    // Manual character creation trigger
    [ContextMenu("Create Character From Webcam")]
    public void CreateCharacterFromCurrentFrame()
    {
        if (processedTexture != null && characterCreator != null)
        {
            CreateCharacterFromWebcam(processedTexture);
        }
        else
        {
            Debug.LogWarning("Cannot create character: no processed texture or no character creator");
        }
    }
    
    // Helper method to convert Texture2D to Sprite (for future enhancement)
    public Sprite ConvertTextureToSprite(Texture2D texture)
    {
        if (texture == null) return null;
        
        // Create a new Sprite using the Texture2D
        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), 
            new Vector2(0.5f, 0.5f), 100f);
    }
    
    // Method to set the character sprite from webcam (for future enhancement)
    [ContextMenu("Set Character Sprite From Webcam")]
    public void SetCharacterSpriteFromWebcam()
    {
        if (processedTexture != null && characterCreator != null)
        {
            Sprite characterSprite = ConvertTextureToSprite(processedTexture);
            characterCreator.pngSprite = characterSprite; // Use compatibility property
            Debug.Log("Character sprite set from webcam frame");
        }
        else
        {
            Debug.LogWarning("Cannot set sprite: no processed texture or no character creator");
        }
    }
}
