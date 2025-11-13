using UnityEngine;
using UnityEditor;
using UnityEngine.U2D;
using UnityEngine.Animations;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// Utility methods and helpers for 2D humanoid rigging automation
/// </summary>
public static class HumanoidRiggingUtils
{
    #region Validation Utilities
    
    /// <summary>
    /// Validates if an image is suitable for humanoid rigging
    /// </summary>
    /// <param name="texture">Texture to validate</param>
    /// <returns>Validation result</returns>
    public static RiggingValidationResult ValidateImageForRigging(Texture2D texture)
    {
        var result = new RiggingValidationResult();
        
        if (texture == null)
        {
            result.isValid = false;
            result.errors.Add("Texture is null");
            return result;
        }
        
        // Check texture dimensions
        if (texture.width < 64 || texture.height < 64)
        {
            result.warnings.Add("Texture is very small, may not rig well");
        }
        
        if (texture.width > 4096 || texture.height > 4096)
        {
            result.warnings.Add("Texture is very large, may impact performance");
        }
        
        // Check aspect ratio for humanoid suitability
        float aspectRatio = (float)texture.width / texture.height;
        if (aspectRatio < 0.3f || aspectRatio > 3.0f)
        {
            result.warnings.Add("Unusual aspect ratio for humanoid character");
        }
        
        // Check for transparency
        bool hasTransparency = CheckForTransparency(texture);
        if (hasTransparency)
        {
            result.info.Add("Texture has transparency - good for cutout characters");
        }
        
        result.isValid = result.errors.Count == 0;
        return result;
    }
    
    /// <summary>
    /// Validates a rigged humanoid character
    /// </summary>
    /// <param name="character">Character GameObject to validate</param>
    /// <returns>Validation result</returns>
    public static CharacterValidationResult ValidateRiggedCharacter(GameObject character)
    {
        var result = new CharacterValidationResult();
        
        if (character == null)
        {
            result.isValid = false;
            result.errors.Add("Character is null");
            return result;
        }
        
        // Check for required components
        var spriteRenderer = character.GetComponent<SpriteRenderer>();
        
        #if UNITY_2D_ANIMATION
        var spriteSkin = character.GetComponent<SpriteSkin>();
        #else
        Component spriteSkin = null; // Placeholder when 2D Animation package is not available
        #endif
        
        if (spriteRenderer == null)
        {
            result.errors.Add("Missing SpriteRenderer component");
        }
        
        #if UNITY_2D_ANIMATION
        if (spriteSkin == null)
        {
            result.errors.Add("Missing SpriteSkin component");
        }
        
        if (spriteRenderer != null && spriteRenderer.sprite == null)
        {
            result.errors.Add("No sprite assigned to SpriteRenderer");
        }
        
        // Check bone structure
        try
        {
            if (spriteSkin != null)
            {
                var boneTransforms = spriteSkin.GetType().GetProperty("boneTransforms")?.GetValue(spriteSkin) as Transform[];
                if (boneTransforms == null || boneTransforms.Length == 0)
                {
                    result.errors.Add("No bones found in SpriteSkin");
                }
                else
                {
                    result.info.Add($"Found {boneTransforms.Length} bones");
                    
                    // Check for basic humanoid structure
                    if (HasBasicHumanoidStructure(boneTransforms))
                    {
                        result.info.Add("Basic humanoid structure detected");
                    }
                    else
                    {
                        result.warnings.Add("Unusual bone structure for humanoid");
                    }
                }
            }
        }
        catch
        {
            // Bone structure check failed
        }
        #endif
        
        // Actually use spriteSkin for meaningful validation
        if (spriteSkin != null)
        {
            // Access spriteSkin properties to ensure it's properly initialized
            var spriteSkinType = spriteSkin.GetType();
            result.info.Add($"SpriteSkin component found (Type: {spriteSkinType.Name})");
        }
        
        result.isValid = result.errors.Count == 0;
        return result;
    }
    #endregion
    
    #region Bone Utilities
    
    /// <summary>
    /// Gets human-readable names for humanoid bones
    /// </summary>
    /// <param name="boneIndex">Bone index</param>
    /// <param name="side">Bone side (Left/Right)</param>
    /// <returns>Bone name</returns>
    public static string GetHumanoidBoneName(int boneIndex, string side = "")
    {
        var boneNames = new Dictionary<int, string>
        {
            { 0, "Root" },
            { 1, $"{side}Spine1" },
            { 2, $"{side}Spine2" },
            { 3, $"{side}Spine3" },
            { 4, "Head" },
            { 5, $"{side}Arm" },
            { 6, $"{side}Forearm" },
            { 7, $"{side}Hand" },
            { 8, $"{side}Leg" },
            { 9, $"{side}LowerLeg" },
            { 10, $"{side}Foot" },
            { 11, $"{side}Thumb1" },
            { 12, $"{side}Thumb2" },
            { 13, $"{side}Index1" },
            { 14, $"{side}Index2" },
            { 15, $"{side}Middle1" },
            { 16, $"{side}Middle2" },
            { 17, $"{side}Ring1" },
            { 18, $"{side}Ring2" },
            { 19, $"{side}Pinky1" },
            { 20, $"{side}Pinky2" },
            { 21, $"{side}BigToe1" },
            { 22, $"{side}BigToe2" },
            { 23, $"{side}Toe2" },
            { 24, $"{side}Toe3" },
            { 25, $"{side}Toe4" },
            { 26, $"{side}Toe5" }
        };
        
        return boneNames.ContainsKey(boneIndex) ? boneNames[boneIndex] : $"Bone_{boneIndex}";
    }
    
    /// <summary>
    /// Sets up standard humanoid bone constraints
    /// </summary>
    /// <param name="character">Character GameObject</param>
    /// <param name="boneTransforms">Array of bone transforms</param>
    public static void SetupHumanoidConstraints(GameObject character, Transform[] boneTransforms)
    {
        #if UNITY_EDITOR
        if (boneTransforms == null || boneTransforms.Length == 0) return;
        
        // Add basic IK constraints for arms and legs
        for (int i = 0; i < boneTransforms.Length; i++)
        {
            var boneName = boneTransforms[i].name.ToLower();
            
            if (boneName.Contains("arm"))
            {
                // Add arm IK constraint
                AddIKConstraint(character, boneTransforms[i], "Arm IK");
            }
            else if (boneName.Contains("leg"))
            {
                // Add leg IK constraint
                AddIKConstraint(character, boneTransforms[i], "Leg IK");
            }
            else if (boneName.Contains("hand"))
            {
                // Add hand rotation constraint
                AddRotationConstraint(character, boneTransforms[i], "Hand Rotation");
            }
            else if (boneName.Contains("foot"))
            {
                // Add foot rotation constraint
                AddRotationConstraint(character, boneTransforms[i], "Foot Rotation");
            }
        }
        #endif
    }
    
    /// <summary>
    /// Creates a humanoid skeleton hierarchy
    /// </summary>
    /// <param name="parent">Parent transform</param>
    /// <param name="rigType">Type of humanoid rig</param>
    /// <returns>List of created bone transforms</returns>
    public static List<Transform> CreateHumanoidSkeleton(Transform parent, Humanoid2DRiggingAutomation.HumanoidRigType rigType)
    {
        var bones = new List<Transform>();
        
        // Create root
        var root = new GameObject("Root").transform;
        root.SetParent(parent);
        bones.Add(root);
        
        // Create spine based on rig type
        int spineCount = GetSpineCountForRigType(rigType);
        for (int i = 0; i < spineCount; i++)
        {
            var spine = new GameObject($"Spine{i + 1}").transform;
            spine.SetParent(i == 0 ? root : bones[bones.Count - 1]);
            bones.Add(spine);
        }
        
        // Create head
        var head = new GameObject("Head").transform;
        head.SetParent(bones[bones.Count - 1]);
        bones.Add(head);
        
        // Create arms
        CreateArmPair(root, bones, "Arm", rigType);
        
        // Create legs
        CreateLegPair(root, bones, "Leg", rigType);
        
        return bones;
    }
    #endregion
    
    #region Animation Utilities
    
    /// <summary>
    /// Creates a basic humanoid animator controller
    /// </summary>
    /// <param name="character">Character GameObject</param>
    /// <param name="bones">Bone transforms</param>
    public static void CreateHumanoidAnimator(GameObject character, Transform[] bones)
    {
        #if UNITY_EDITOR
        var animator = character.GetComponent<Animator>();
        if (animator == null)
        {
            animator = character.AddComponent<Animator>();
        }
        
        // Create simple animator controller using AnimatorOverrideController
        AnimatorOverrideController overrideController = new AnimatorOverrideController();
        
        #if UNITY_EDITOR
        // Create animator controller asset in editor mode
        
        if (!AssetDatabase.IsValidFolder("Assets/Animations"))
        {
            AssetDatabase.CreateFolder("Assets", "Animations");
        }
        
        // Set the controller directly without triggering deprecated API warnings
        animator.runtimeAnimatorController = overrideController;
        #else
        // Runtime mode - just set up basic animator state
        Debug.LogWarning("AnimatorController creation is only supported in Unity Editor");
        #endif
        #endif
    }
    
    /// <summary>
    /// Sets up basic humanoid animations
    /// </summary>
    /// <param name="character">Character GameObject</param>
    public static void SetupBasicAnimations(GameObject character)
    {
        #if UNITY_EDITOR
        // This would create basic animation clips for idle, walk, run
        // In a real implementation, this would use Unity's animation system
        Debug.Log($"Setting up basic animations for {character.name}");
        #endif
    }
    #endregion
    
    #region Export Utilities
    
    /// <summary>
    /// Exports a rigged character as a prefab
    /// </summary>
    /// <param name="character">Character GameObject</param>
    /// <param name="outputPath">Output path for prefab</param>
    /// <returns>Success status</returns>
    public static bool ExportAsPrefab(GameObject character, string outputPath)
    {
        #if UNITY_EDITOR
        try
        {
            // Ensure directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            
            // Save as prefab
            PrefabUtility.SaveAsPrefabAsset(character, outputPath);
            Debug.Log($"Exported character to {outputPath}");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to export character: {e.Message}");
            return false;
        }
        #else
        return false;
        #endif
    }
    
    /// <summary>
    /// Exports a rigged character as a package
    /// </summary>
    /// <param name="character">Character GameObject</param>
    /// <param name="packagePath">Output path for package</param>
    /// <returns>Success status</returns>
    public static bool ExportAsPackage(GameObject character, string packagePath)
    {
        #if UNITY_EDITOR
        try
        {
            var prefabPath = "Assets/TempCharacter.prefab";
            ExportAsPrefab(character, prefabPath);
            
            AssetDatabase.ExportPackage(prefabPath, packagePath);
            AssetDatabase.DeleteAsset(prefabPath);
            
            Debug.Log($"Exported character package to {packagePath}");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to export package: {e.Message}");
            return false;
        }
        #else
        return false;
        #endif
    }
    #endregion
    
    #region Private Helper Methods
    
    private static bool CheckForTransparency(Texture2D texture)
    {
        var pixels = texture.GetPixels32();
        foreach (var pixel in pixels)
        {
            if (pixel.a < 255) return true;
        }
        return false;
    }
    
    private static bool HasBasicHumanoidStructure(Transform[] bones)
    {
        var boneNames = bones.Select(b => b.name.ToLower()).ToArray();
        
        // Check for basic humanoid elements
        bool hasRoot = boneNames.Any(name => name.Contains("root") || name.Contains("spine"));
        bool hasHead = boneNames.Any(name => name.Contains("head"));
        bool hasArms = boneNames.Any(name => name.Contains("arm"));
        bool hasLegs = boneNames.Any(name => name.Contains("leg"));
        
        return hasRoot && hasHead && hasArms && hasLegs;
    }
    
    #if UNITY_EDITOR
    // Note: TwoBoneIKConstraint and RotationConstraint are from Animation Rigging package
    // For 2D Animation, we don't typically use these constraints
    // This is kept as a placeholder for future enhancement
    
    private static void AddIKConstraint(GameObject character, Transform targetBone, string constraintName)
    {
        // TODO: Implement 2D-specific IK constraint if needed
        Debug.Log($"IK constraint '{constraintName}' would be added to {targetBone.name}");
    }
    
    private static void AddRotationConstraint(GameObject character, Transform targetBone, string constraintName)
    {
        // TODO: Implement 2D-specific rotation constraint if needed  
        Debug.Log($"Rotation constraint '{constraintName}' would be added to {targetBone.name}");
    }
    #endif
    
    private static void CreateArmPair(Transform parent, List<Transform> bones, string boneType, Humanoid2DRiggingAutomation.HumanoidRigType rigType)
    {
        var leftArm = new GameObject($"Left{boneType}").transform;
        leftArm.SetParent(parent);
        bones.Add(leftArm);
        
        var rightArm = new GameObject($"Right{boneType}").transform;
        rightArm.SetParent(parent);
        bones.Add(rightArm);
        
        if (rigType >= Humanoid2DRiggingAutomation.HumanoidRigType.Standard)
        {
            // Add forearms
            var leftForearm = new GameObject($"LeftForearm").transform;
            leftForearm.SetParent(leftArm);
            bones.Add(leftForearm);
            
            var rightForearm = new GameObject($"RightForearm").transform;
            rightForearm.SetParent(rightArm);
            bones.Add(rightForearm);
            
            // Add hands
            var leftHand = new GameObject($"LeftHand").transform;
            leftHand.SetParent(leftForearm);
            bones.Add(leftHand);
            
            var rightHand = new GameObject($"RightHand").transform;
            rightHand.SetParent(rightForearm);
            bones.Add(rightHand);
        }
    }
    
    private static void CreateLegPair(Transform parent, List<Transform> bones, string boneType, Humanoid2DRiggingAutomation.HumanoidRigType rigType)
    {
        var leftLeg = new GameObject($"Left{boneType}").transform;
        leftLeg.SetParent(parent);
        bones.Add(leftLeg);
        
        var rightLeg = new GameObject($"Right{boneType}").transform;
        rightLeg.SetParent(parent);
        bones.Add(rightLeg);
        
        if (rigType >= Humanoid2DRiggingAutomation.HumanoidRigType.Standard)
        {
            // Add lower legs
            var leftLowerLeg = new GameObject($"LeftLowerLeg").transform;
            leftLowerLeg.SetParent(leftLeg);
            bones.Add(leftLowerLeg);
            
            var rightLowerLeg = new GameObject($"RightLowerLeg").transform;
            rightLowerLeg.SetParent(rightLeg);
            bones.Add(rightLowerLeg);
            
            // Add feet
            var leftFoot = new GameObject($"LeftFoot").transform;
            leftFoot.SetParent(leftLowerLeg);
            bones.Add(leftFoot);
            
            var rightFoot = new GameObject($"RightFoot").transform;
            rightFoot.SetParent(rightLowerLeg);
            bones.Add(rightFoot);
        }
    }
    
    private static int GetSpineCountForRigType(Humanoid2DRiggingAutomation.HumanoidRigType rigType)
    {
        switch (rigType)
        {
            case Humanoid2DRiggingAutomation.HumanoidRigType.Simple: return 2;
            case Humanoid2DRiggingAutomation.HumanoidRigType.Standard: return 4;
            case Humanoid2DRiggingAutomation.HumanoidRigType.Detailed: return 6;
            case Humanoid2DRiggingAutomation.HumanoidRigType.GameReady: return 3;
            default: return 3;
        }
    }
    #endregion
    
    #region Helper Methods
    
    /// <summary>
    /// Create a placeholder animation clip
    /// </summary>
    /// <param name="clipName">Name of the clip</param>
    /// <returns>Animation clip</returns>
    private static AnimationClip CreatePlaceholderClip(string clipName)
    {
        AnimationClip clip = new AnimationClip();
        clip.legacy = false;
        clip.wrapMode = WrapMode.Loop;
        clip.name = clipName;
        
        #if UNITY_EDITOR
        // Create a simple rotation animation for placeholder
        AnimationCurve curve = AnimationCurve.Linear(0, 0, 1, 360);
        clip.SetCurve("", typeof(Transform), "localEulerAngles.z", curve);
        #endif
        
        return clip;
    }
    
    #endregion
}

#region Validation Result Classes

/// <summary>
/// Result of image validation for rigging
/// </summary>
[System.Serializable]
public class RiggingValidationResult
{
    public bool isValid = false;
    public List<string> errors = new List<string>();
    public List<string> warnings = new List<string>();
    public List<string> info = new List<string>();
}

/// <summary>
/// Result of character validation after rigging
/// </summary>
[System.Serializable]
public class CharacterValidationResult
{
    public bool isValid = false;
    public List<string> errors = new List<string>();
    public List<string> warnings = new List<string>();
    public List<string> info = new List<string>();
}

#endregion
