using UnityEngine;
using UnityEditor;
using UnityEngine.U2D;
using UnityEngine.Animations;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// Automated humanoid rigging system for 2D sprites
/// Handles sprite cutting, bone generation, and rigging in one workflow
/// </summary>
public class Humanoid2DRiggingAutomation : MonoBehaviour
{
    #region Enums and Data Structures
    
    /// <summary>
    /// Types of humanoid rigs available
    /// </summary>
    public enum HumanoidRigType
    {
        Simple,     // Basic humanoid with 2 spine bones
        Standard,   // Standard humanoid with 4 spine bones and hands/feet
        Detailed,   // Detailed humanoid with 6 spine bones and full finger/toe articulation
        GameReady   // Optimized for games with 3 spine bones
    }
    
    /// <summary>
    /// Configuration for humanoid rigging process
    /// </summary>
    [System.Serializable]
    public class RiggingConfig
    {
        public HumanoidRigType rigType = HumanoidRigType.Standard;
        public bool generateConstraints = true;
        public bool createAnimator = true;
        public bool exportAsPrefab = false;
        public string outputPath = "Assets/RiggedCharacters/";
        public bool autoValidate = true;
        public bool enablePhysics = false;
        public bool setupBasicAnimations = true;
    }
    
    #endregion
    
    #region Main Workflow Methods
    
    /// <summary>
    /// Main method to rig a humanoid character from sprite
    /// </summary>
    /// <param name="sprite">Source sprite to rig</param>
    /// <param name="config">Rigging configuration</param>
    /// <returns>Rigged character GameObject</returns>
    public static GameObject RigHumanoidCharacter(Sprite sprite, RiggingConfig config = null)
    {
        if (sprite == null)
        {
            Debug.LogError("Cannot rig null sprite");
            return null;
        }
        
        if (config == null)
        {
            config = new RiggingConfig();
        }
        
        // Validate input sprite
        var validationResult = HumanoidRiggingUtils.ValidateImageForRigging(sprite.texture);
        if (!validationResult.isValid)
        {
            Debug.LogError("Sprite validation failed: " + string.Join(", ", validationResult.errors));
            return null;
        }
        
        // Log validation warnings
        foreach (var warning in validationResult.warnings)
        {
            Debug.LogWarning(warning);
        }
        
        // Step 1: Create character GameObject
        GameObject character = new GameObject(sprite.name + "_Rigged");
        var spriteRenderer = character.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = sprite;
        
        // Step 2: Generate bone hierarchy
        Transform[] bones = GenerateBoneHierarchy(character.transform, config.rigType);
        if (bones == null || bones.Length == 0)
        {
            Debug.LogError("Failed to generate bone hierarchy");
            return null;
        }
        
        // Step 3: Setup SpriteSkin for 2D Animation
        if (!SetupSpriteSkin(character, bones, config))
        {
            Debug.LogWarning("Failed to setup SpriteSkin - continuing without deformation");
        }
        
        // Step 4: Create animator if requested
        if (config.createAnimator)
        {
            HumanoidRiggingUtils.CreateHumanoidAnimator(character, bones);
        }
        
        // Step 5: Setup constraints if requested
        if (config.generateConstraints)
        {
            HumanoidRiggingUtils.SetupHumanoidConstraints(character, bones);
        }
        
        // Step 6: Setup basic animations if requested
        if (config.setupBasicAnimations)
        {
            HumanoidRiggingUtils.SetupBasicAnimations(character);
        }
        
        // Step 7: Validate final result
        if (config.autoValidate)
        {
            var finalValidation = HumanoidRiggingUtils.ValidateRiggedCharacter(character);
            if (!finalValidation.isValid)
            {
                Debug.LogWarning("Final validation found issues: " + string.Join(", ", finalValidation.errors));
            }
            
            foreach (var info in finalValidation.info)
            {
                Debug.Log(info);
            }
        }
        
        // Step 8: Export as prefab if requested
        if (config.exportAsPrefab)
        {
            string prefabPath = config.outputPath + character.name + ".prefab";
            if (HumanoidRiggingUtils.ExportAsPrefab(character, prefabPath))
            {
                Debug.Log($"Character exported to {prefabPath}");
            }
        }
        
        return character;
    }
    
    /// <summary>
    /// Quick rig method with default settings
    /// </summary>
    /// <param name="sprite">Source sprite</param>
    /// <returns>Rigged character</returns>
    public static GameObject QuickRig(Sprite sprite)
    {
        var config = new RiggingConfig
        {
            rigType = HumanoidRigType.Standard,
            createAnimator = true,
            generateConstraints = false,
            setupBasicAnimations = true,
            autoValidate = true
        };
        
        return RigHumanoidCharacter(sprite, config);
    }
    
    /// <summary>
    /// Batch rig multiple sprites
    /// </summary>
    /// <param name="sprites">Array of sprites to rig</param>
    /// <param name="config">Rigging configuration</param>
    /// <returns>Array of rigged characters</returns>
    public static GameObject[] BatchRigCharacters(Sprite[] sprites, RiggingConfig config = null)
    {
        if (sprites == null || sprites.Length == 0)
        {
            Debug.LogError("No sprites provided for batch rigging");
            return null;
        }
        
        var characters = new List<GameObject>();
        
        foreach (var sprite in sprites)
        {
            if (sprite != null)
            {
                var character = RigHumanoidCharacter(sprite, config);
                if (character != null)
                {
                    characters.Add(character);
                }
            }
        }
        
        Debug.Log($"Batch rigged {characters.Count} characters");
        return characters.ToArray();
    }
    
    #endregion
    
    #region Bone Generation Methods
    
    /// <summary>
    /// Generates humanoid bone hierarchy
    /// </summary>
    /// <param name="parent">Parent transform</param>
    /// <param name="rigType">Type of rig to generate</param>
    /// <returns>Array of bone transforms</returns>
    private static Transform[] GenerateBoneHierarchy(Transform parent, HumanoidRigType rigType)
    {
        try
        {
            var bones = HumanoidRiggingUtils.CreateHumanoidSkeleton(parent, rigType);
            return bones.ToArray();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to generate bone hierarchy: {e.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Setup SpriteSkin component for 2D animation deformation
    /// </summary>
    /// <param name="character">Character GameObject</param>
    /// <param name="bones">Bone transforms</param>
    /// <param name="config">Rigging configuration</param>
    /// <returns>Success status</returns>
    private static bool SetupSpriteSkin(GameObject character, Transform[] bones, RiggingConfig config)
    {
        #if UNITY_2D_ANIMATION
        try
        {
            var spriteSkin = character.GetComponent<SpriteSkin>();
            if (spriteSkin == null)
            {
                spriteSkin = character.AddComponent<SpriteSkin>();
            }
            
            // Setup bone references
            var boneArray = new Transform[bones.Length];
            for (int i = 0; i < bones.Length; i++)
            {
                boneArray[i] = bones[i];
            }
            
            // Use reflection to safely set bone transforms property
            var boneTransformsProperty = typeof(SpriteSkin).GetProperty("boneTransforms");
            if (boneTransformsProperty != null)
            {
                boneTransformsProperty.SetValue(spriteSkin, boneArray);
            }
            
            Debug.Log("SpriteSkin setup completed");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Failed to setup SpriteSkin: {e.Message}");
            return false;
        }
        #else
        Debug.LogWarning("2D Animation package not available - SpriteSkin will not be created");
        return false;
        #endif
    }
    
    #endregion
    
    #region Editor Menu Integration
    
    /// <summary>
    /// Creates a rigged character from selected sprite in Unity Editor
    /// </summary>
    [MenuItem("Humanoid2D/Create Rigged Character")]
    private static void CreateRiggedCharacterFromSelection()
    {
        var selectedObject = Selection.activeObject;
        Sprite sprite = null;
        
        if (selectedObject is Sprite)
        {
            sprite = (Sprite)selectedObject;
        }
        else if (selectedObject is GameObject)
        {
            var spriteRenderer = ((GameObject)selectedObject).GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                sprite = spriteRenderer.sprite;
            }
        }
        
        if (sprite == null)
        {
            EditorUtility.DisplayDialog("Error", "Please select a Sprite or GameObject with a SpriteRenderer", "OK");
            return;
        }
        
        var config = new RiggingConfig
        {
            rigType = HumanoidRigType.Standard,
            createAnimator = true,
            generateConstraints = true,
            setupBasicAnimations = true,
            autoValidate = true
        };
        
        var character = RigHumanoidCharacter(sprite, config);
        if (character != null)
        {
            // Select the new character
            Selection.activeGameObject = character;
            
            // Focus on the new character
            Selection.activeObject = character;
            
            Debug.Log($"Created rigged character: {character.name}");
        }
    }
    
    /// <summary>
    /// Enables the menu item only when a sprite is selected
    /// </summary>
    [MenuItem("Humanoid2D/Create Rigged Character", true)]
    private static bool ValidateCreateRiggedCharacter()
    {
        var selectedObject = Selection.activeObject;
        if (selectedObject == null) return false;
        
        // Check if it's a sprite
        if (selectedObject is Sprite) return true;
        
        // Check if it's a GameObject with SpriteRenderer
        if (selectedObject is GameObject)
        {
            var spriteRenderer = ((GameObject)selectedObject).GetComponent<SpriteRenderer>();
            return spriteRenderer != null && spriteRenderer.sprite != null;
        }
        
        return false;
    }
    
    #endregion
    
    #region Utility Methods
    
    /// <summary>
    /// Validates if a character is properly rigged
    /// </summary>
    /// <param name="character">Character to validate</param>
    /// <returns>Validation result</returns>
    public static CharacterValidationResult ValidateCharacter(GameObject character)
    {
        return HumanoidRiggingUtils.ValidateRiggedCharacter(character);
    }
    
    /// <summary>
    /// Exports a rigged character as a package
    /// </summary>
    /// <param name="character">Character to export</param>
    /// <param name="packagePath">Output path</param>
    /// <returns>Success status</returns>
    public static bool ExportCharacterPackage(GameObject character, string packagePath)
    {
        return HumanoidRiggingUtils.ExportAsPackage(character, packagePath);
    }
    
    #endregion
}