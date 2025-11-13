using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Clean, simplified 2D physics auto-setup system
/// Automatically configures optimal 2D physics settings for character animation
/// </summary>
public class PhysicsAutoSetup : MonoBehaviour
{
    [Header("🎯 Character Physics Setup")]
    public GameObject targetCharacter;
    public MovementType movementType = MovementType.Platformer;
    
    [Header("⚙️ Physics Settings")]
    public bool addRigidbody2D = true;
    public bool addCollider2D = true;
    public bool createPhysicsMaterial = true;
    public bool optimizeForAnimation = true;
    
    [Header("🔧 Advanced Settings")]
    public bool createChildObjects = true;
    public bool setupForKinematics = true;
    public bool addJoints = true;
    public float physicsScale = 1f;
    
    private void Start()
    {
        SetupOptimalPhysics();
    }
    
    [ContextMenu("Setup Optimal Physics")]
    public void SetupOptimalPhysics()
    {
        if (targetCharacter == null)
        {
            targetCharacter = gameObject;
        }
        
        LogStep("Setting up optimal 2D physics for character animation");
        
        // Step 1: Add Rigidbody2D if needed
        if (addRigidbody2D)
        {
            SetupRigidbody2D();
        }
        
        // Step 2: Add Collider2D if needed
        if (addCollider2D)
        {
            SetupCollider2D();
        }
        
        // Step 3: Create and assign PhysicsMaterial2D
        if (createPhysicsMaterial)
        {
            CreatePhysicsMaterial2D();
        }
        
        // Step 4: Optimize for animation
        if (optimizeForAnimation)
        {
            OptimizeForAnimation();
        }
        
        // Step 5: Add joints if requested
        if (addJoints)
        {
            SetupJoints();
        }
        
        LogStep("2D physics setup complete");
    }
    
    private void SetupRigidbody2D()
    {
        var rb2d = targetCharacter.GetComponent<Rigidbody2D>();
        if (rb2d == null)
        {
            rb2d = targetCharacter.AddComponent<Rigidbody2D>();
        }
        
        // Optimal settings for character animation
        rb2d.bodyType = RigidbodyType2D.Dynamic;
        rb2d.mass = 1f;
        rb2d.linearDamping = 0f;
        rb2d.angularDamping = 0.05f;
        rb2d.gravityScale = 0f; // Override default gravity
        rb2d.freezeRotation = true;
        rb2d.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb2d.sleepMode = RigidbodySleepMode2D.StartAwake;
        rb2d.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        
        LogStep("Rigidbody2D configured for animation");
    }
    
    private void SetupCollider2D()
    {
        // Remove existing colliders first
        var existingColliders = targetCharacter.GetComponents<Collider2D>();
        foreach (var collider in existingColliders)
        {
            DestroyImmediate(collider);
        }
        
        // Add appropriate collider based on character shape
        if (HasChildSprites(targetCharacter))
        {
            // Character likely has multiple sprites, use box collider
            var boxCollider = targetCharacter.AddComponent<BoxCollider2D>();
            boxCollider.size = Vector2.one * 1f;
            boxCollider.offset = Vector2.zero;
            boxCollider.autoTiling = true;
        }
        else
        {
            // Single sprite character, use circle or box collider
            var circleCollider = targetCharacter.AddComponent<CircleCollider2D>();
            circleCollider.radius = 0.5f;
            circleCollider.offset = Vector2.zero;
        }
        
        LogStep("Collider2D configured for character");
    }
    
    private void CreatePhysicsMaterial2D()
    {
        // Create Physics Material for optimal animation performance
        var material = new PhysicsMaterial2D("CharacterPhysicsMaterial2D");
        
        // Set optimal values based on movement type
        switch (movementType)
        {
            case MovementType.Platformer:
                material.friction = 0.4f;
                material.bounciness = 0.1f;
                material.frictionCombine = PhysicsMaterialCombine2D.Average;
                material.bounceCombine = PhysicsMaterialCombine2D.Minimum;
                break;
                
            case MovementType.TopDown:
                material.friction = 0.2f;
                material.bounciness = 0.1f;
                material.frictionCombine = PhysicsMaterialCombine2D.Minimum;
                material.bounceCombine = PhysicsMaterialCombine2D.Minimum;
                break;
                
            case MovementType.SideScroller:
                material.friction = 0.3f;
                material.bounciness = 0.05f;
                material.frictionCombine = PhysicsMaterialCombine2D.Average;
                material.bounceCombine = PhysicsMaterialCombine2D.Minimum;
                break;
                
            case MovementType.Action:
                material.friction = 0.1f;
                material.bounciness = 0.3f;
                material.frictionCombine = PhysicsMaterialCombine2D.Minimum;
                material.bounceCombine = PhysicsMaterialCombine2D.Maximum;
                break;
                
            case MovementType.Racing:
                material.friction = 0.6f;
                material.bounciness = 0.2f;
                material.frictionCombine = PhysicsMaterialCombine2D.Average;
                material.bounceCombine = PhysicsMaterialCombine2D.Maximum;
                break;
        }
        
        // Apply material to all colliders
        var colliders = targetCharacter.GetComponents<Collider2D>();
        foreach (var collider in colliders)
        {
            collider.sharedMaterial = material;
        }
        
        LogStep("Physics Material 2D created and assigned");
    }
    
    private void OptimizeForAnimation()
    {
        var rb2d = targetCharacter.GetComponent<Rigidbody2D>();
        if (rb2d != null)
        {
            // Animation-optimized settings
            rb2d.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb2d.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            
            // Reduce physics simulation frequency for better performance
            // Note: maxAngularVelocity and maxVelocity don't exist on Rigidbody2D
            // These properties are only available on Rigidbody (3D)
            // rb2d.maxAngularVelocity = 7f;
            // rb2d.maxVelocity = 10f;
        }
        
        // Optimize transform for animation
        targetCharacter.layer = LayerMask.NameToLayer("Default");
        
        LogStep("Character optimized for animation");
    }
    
    private void SetupJoints()
    {
        // Add spring joint for smooth movement
        if (movementType == MovementType.Platformer || movementType == MovementType.SideScroller)
        {
            var springJoint = targetCharacter.AddComponent<SpringJoint2D>();
            springJoint.autoConfigureConnectedAnchor = true;
            springJoint.autoConfigureDistance = true;
            springJoint.enableCollision = false;
            springJoint.dampingRatio = 0.8f;
            springJoint.frequency = 3f;
        }
    }
    
    private bool HasChildSprites(GameObject obj)
    {
        var spriteRenderers = obj.GetComponentsInChildren<SpriteRenderer>();
        return spriteRenderers.Length > 1;
    }
    
    private void LogStep(string message)
    {
        Debug.Log($"[PhysicsAutoSetup] {message}");
    }
    
    [ContextMenu("Reset to Defaults")]
    public void ResetToDefaults()
    {
        movementType = MovementType.Platformer;
        addRigidbody2D = true;
        addCollider2D = true;
        createPhysicsMaterial = true;
        optimizeForAnimation = true;
        setupForKinematics = true;
        addJoints = true;
        physicsScale = 1f;
        
        LogStep("Settings reset to defaults");
    }
}

public enum MovementType
{
    Platformer,    // Mario-style platform jumping
    TopDown,       // Zelda-style top-down movement
    SideScroller,  // Sonic-style side-scrolling
    Action,        // Action game physics
    Racing         // Racing game physics
}
