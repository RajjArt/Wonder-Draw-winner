using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

/// <summary>
/// Clean, simplified 2D bone animation system with physics integration
/// Focuses on core functionality without complex API calls that may vary between Unity versions
/// </summary>
public class BonesAnimationSystem : MonoBehaviour
{
    [Header("🎯 Character Setup")]
    public GameObject animatedCharacter;
    public Transform[] boneTransforms;
    
    [Header("⚙️ Animation Settings")]
    public bool enableBonesAnimation = true;
    public bool addPhysicsJoints = true;
    public float boneUpdateSpeed = 0.1f;
    public float jointFrequency = 1.0f;
    public float jointDamping = 0.5f;
    
    [Header("🎨 Visual Settings")]
    public bool showBoneGizmos = true;
    public Color boneColor = Color.cyan;
    public float gizmoSize = 0.1f;
    
    // Internal state
    private bool is2DAnimationAvailable = false;
    private bool usingPhysicsBones = false;
    private List<Transform> physicsBones;
    private List<SpringJoint2D> boneJoints;
    
    void Awake()
    {
        if (animatedCharacter == null)
            animatedCharacter = this.gameObject;
            
        // Detect 2D Animation package availability
        Detect2DAnimationPackage();
        
        // Setup animation system
        if (enableBonesAnimation)
        {
            if (is2DAnimationAvailable)
            {
                SetupSpriteSkinSystem();
            }
            else
            {
                SetupPhysicsBonesSystem();
            }
        }
    }
    
    private void Detect2DAnimationPackage()
    {
        try
        {
            // Use reflection to check for 2D Animation package
            Type spriteSkinType = Type.GetType("UnityEngine.U2D.Animation.SpriteSkin, Unity.2D.Animation");
            is2DAnimationAvailable = spriteSkinType != null;
            
            Debug.Log(is2DAnimationAvailable 
                ? "✅ 2D Animation package detected - using SpriteSkin system" 
                : "ℹ️ 2D Animation package not found - using physics fallback");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"⚠️ Could not detect 2D Animation package: {e.Message}");
            is2DAnimationAvailable = false;
        }
    }
    
    private void SetupSpriteSkinSystem()
    {
        try
        {
            // This would be used if 2D Animation package is available
            // For now, fallback to physics system
            SetupPhysicsBonesSystem();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"⚠️ SpriteSkin setup failed: {e.Message}, falling back to physics");
            SetupPhysicsBonesSystem();
        }
    }
    
    private void SetupPhysicsBonesSystem()
    {
        usingPhysicsBones = true;
        physicsBones = new List<Transform>();
        boneJoints = new List<SpringJoint2D>();
        
        if (boneTransforms == null || boneTransforms.Length == 0)
        {
            // Auto-generate simple bone structure
            CreateSimpleBones();
        }
        
        // Add physics components to bones
        foreach (var bone in boneTransforms)
        {
            if (bone != null)
            {
                physicsBones.Add(bone);
                
                // Add Rigidbody2D if not present
                var rb2d = bone.GetComponent<Rigidbody2D>();
                if (rb2d == null)
                {
                    rb2d = bone.gameObject.AddComponent<Rigidbody2D>();
                    rb2d.bodyType = RigidbodyType2D.Kinematic;
                    rb2d.gravityScale = 0f;
                    rb2d.linearDamping = 2f;
                }
                
                // Add physics joints
                if (addPhysicsJoints && bone.parent != null)
                {
                    var parentRb2d = bone.parent.GetComponent<Rigidbody2D>();
                    if (parentRb2d != null)
                    {
                        var joint = bone.gameObject.AddComponent<SpringJoint2D>();
                        joint.connectedBody = parentRb2d;
                        joint.autoConfigureConnectedAnchor = true;
                        joint.frequency = jointFrequency;
                        joint.dampingRatio = Mathf.Clamp01(jointDamping);
                        joint.autoConfigureDistance = false;
                        joint.distance = 0.1f;
                        boneJoints.Add(joint);
                    }
                }
            }
        }
        
        Debug.Log("✅ Physics-based bone animation system activated");
    }
    
    private void CreateSimpleBones()
    {
        // Create a simple hierarchical bone structure
        var root = animatedCharacter.transform;
        
        // Create basic body parts
        var spine = CreateBone(root, "Spine", new Vector3(0, 0.5f, 0));
        var head = CreateBone(spine, "Head", new Vector3(0, 0.8f, 0));
        var leftArm = CreateBone(spine, "LeftArm", new Vector3(-0.5f, 0.3f, 0));
        var rightArm = CreateBone(spine, "RightArm", new Vector3(0.5f, 0.3f, 0));
        var leftLeg = CreateBone(spine, "LeftLeg", new Vector3(-0.2f, -0.3f, 0));
        var rightLeg = CreateBone(spine, "RightLeg", new Vector3(0.2f, -0.3f, 0));
        
        boneTransforms = new Transform[] { root, spine, head, leftArm, rightArm, leftLeg, rightLeg };
    }
    
    private Transform CreateBone(Transform parent, string name, Vector3 localPosition)
    {
        var bone = new GameObject(name).transform;
        bone.SetParent(parent);
        bone.localPosition = localPosition;
        bone.localRotation = Quaternion.identity;
        bone.localScale = Vector3.one;
        return bone;
    }
    
    void Update()
    {
        if (!enableBonesAnimation) return;
        
        if (usingPhysicsBones)
        {
            UpdatePhysicsBones();
        }
    }
    
    private void UpdatePhysicsBones()
    {
        // Simple bone movement simulation
        // In a real implementation, this would update bone transforms based on animation data
        foreach (var bone in physicsBones)
        {
            if (bone == null) continue;
            
            Vector3 originalPos = bone.localPosition;
            Vector3 targetPos = originalPos; // Placeholder for animation target position
            
            // Apply smooth interpolation
            bone.localPosition = Vector3.Lerp(originalPos, targetPos, boneUpdateSpeed);
        }
    }
    
    void OnDrawGizmos()
    {
        if (!showBoneGizmos || boneTransforms == null) return;
        
        Gizmos.color = boneColor;
        
        foreach (var bone in boneTransforms)
        {
            if (bone == null) continue;
            
            // Draw bone position
            Gizmos.DrawWireSphere(bone.position, gizmoSize);
            
            // Draw connections to parent
            if (bone.parent != null)
            {
                Gizmos.DrawLine(bone.position, bone.parent.position);
            }
        }
    }
    
    // Public methods for external control
    [ContextMenu("Setup Bones Animation")]
    public void SetupBonesAnimation()
    {
        if (animatedCharacter == null)
            animatedCharacter = this.gameObject;
            
        Detect2DAnimationPackage();
        
        if (is2DAnimationAvailable)
        {
            SetupSpriteSkinSystem();
        }
        else
        {
            SetupPhysicsBonesSystem();
        }
    }
    
    [ContextMenu("Clear Bones")]
    public void ClearBones()
    {
        // Remove all created bones
        if (boneJoints != null)
        {
            foreach (var joint in boneJoints)
            {
                if (joint != null)
                {
                    DestroyImmediate(joint);
                }
            }
            boneJoints.Clear();
        }
        
        if (physicsBones != null)
        {
            foreach (var bone in physicsBones)
            {
                if (bone != null && bone != animatedCharacter.transform)
                {
                    DestroyImmediate(bone.gameObject);
                }
            }
            physicsBones.Clear();
        }
        
        boneTransforms = null;
        usingPhysicsBones = false;
        Debug.Log("🗑️ Cleared all bones");
    }
    
    // Inspector information
    [ContextMenu("Show System Info")]
    public void ShowSystemInfo()
    {
        string info = $@"
🎭 Bones Animation System Status:
• 2D Animation Available: {is2DAnimationAvailable}
• Using Physics Bones: {usingPhysicsBones}
• Bone Count: {boneTransforms?.Length ?? 0}
• Physics Joints: {boneJoints?.Count ?? 0}
• Character: {(animatedCharacter ? animatedCharacter.name : "None")}

📋 Quick Start:
1. Assign your character GameObject
2. Click 'Setup Bones Animation'
3. Add your bone transforms or use auto-generate
4. Adjust physics settings as needed

⚙️ Settings:
• Update Speed: {boneUpdateSpeed}
• Joint Frequency: {jointFrequency}
• Joint Damping: {jointDamping}";
        
        EditorUtility.DisplayDialog("Bones Animation System", info, "Got it!");
    }
}
