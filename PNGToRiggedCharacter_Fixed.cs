using UnityEngine;

/// <summary>
/// Fixed Production Character Rigging Script
/// Compatible with Unity 2020+ and fixes all API compatibility issues
/// </summary>
[ExecuteAlways]
public class PNGToRiggedCharacter_Fixed : MonoBehaviour
{
    [Header("🎯 Character Setup")]
    public Sprite characterSprite;  // Main sprite to rig
    public RigType rigType = RigType.BoneChain;  // Bone structure type
    public bool autoGenerateBones = true;  // Auto-create bones on play
    
    [Header("⚙️ Animation Settings")]
    public bool createAnimator = true;  // Add Animator component
    public bool createPhysics = true;  // Add physics components
    public int boneCount = 6;  // Number of bones to create
    
    // Internal variables
    private GameObject createdCharacter;
    private GameObject rigRoot;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private int totalBones = 0;

    void Start()
    {
        if (Application.isPlaying && autoGenerateBones && characterSprite != null)
        {
            CreateRiggedCharacter();
        }
    }

    void Update()
    {
        // Auto-regenerate in play mode if enabled
        if (Application.isPlaying && autoGenerateBones && characterSprite != null && createdCharacter == null)
        {
            CreateRiggedCharacter();
        }
    }

    [ContextMenu("Create Rigged Character")]
    public void CreateRiggedCharacter()
    {
        if (characterSprite == null)
        {
            Debug.LogError("❌ No sprite assigned! Please assign a PNG sprite in the inspector.");
            return;
        }

        // Clean up previous character
        ClearPreviousCharacter();

        // Create main character object
        string characterName = characterSprite.name + "_Rigged";
        createdCharacter = new GameObject(characterName);
        createdCharacter.transform.SetParent(transform, false);
        createdCharacter.transform.position = transform.position;

        // Add SpriteRenderer with sprite
        spriteRenderer = createdCharacter.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = characterSprite;
        spriteRenderer.sortingOrder = 0;

        // Add Animator if enabled
        if (createAnimator)
        {
            animator = createdCharacter.AddComponent<Animator>();
            Debug.Log("✅ Animator component added");
        }

        // Add Physics components if enabled
        if (createPhysics)
        {
            AddPhysicsComponents();
        }

        // Create bone structure
        CreateBones();

        Debug.Log($"🎭 Character '{characterName}' created successfully with {totalBones} bones!");
    }

    private void ClearPreviousCharacter()
    {
        if (createdCharacter != null)
        {
            DestroyImmediate(createdCharacter);
            createdCharacter = null;
        }
    }

    private void AddPhysicsComponents()
    {
        // Add Rigidbody2D
        var rb2d = createdCharacter.AddComponent<Rigidbody2D>();
        rb2d.bodyType = RigidbodyType2D.Dynamic;
        rb2d.gravityScale = 1f;
        rb2d.mass = 1f;

        // Add BoxCollider2D
        var bounds = characterSprite.bounds;
        var boxCollider = createdCharacter.AddComponent<BoxCollider2D>();
        boxCollider.size = bounds.size;
        boxCollider.offset = bounds.center;

        // Create Physics Material
        var physicsMaterial = new PhysicsMaterial2D("CharacterPhysics");
        physicsMaterial.friction = 0.4f;
        physicsMaterial.bounciness = 0.1f;
        boxCollider.sharedMaterial = physicsMaterial;

        Debug.Log("✅ Physics components added");
    }

    private void CreateBones()
    {
        totalBones = 0;
        Bounds spriteBounds = characterSprite.bounds;
        Vector3 center = spriteBounds.center;

        switch (rigType)
        {
            case RigType.Simple:
                // Simple 3-bone rig
                CreateBone("Root", center);
                CreateBone("Body", new Vector3(center.x, center.y + 0.3f, 0));
                CreateBone("Head", new Vector3(center.x, center.y + 0.6f, 0));
                break;

            case RigType.BoneChain:
                // 6-bone rig with arms and legs
                CreateBone("LeftArm", new Vector3(center.x - 0.3f, center.y + 0.2f, 0));
                CreateBone("LeftForearm", new Vector3(center.x - 0.5f, center.y + 0.1f, 0));
                CreateBone("RightArm", new Vector3(center.x + 0.3f, center.y + 0.2f, 0));
                CreateBone("RightForearm", new Vector3(center.x + 0.5f, center.y + 0.1f, 0));
                CreateBone("LeftLeg", new Vector3(center.x - 0.2f, center.y - 0.3f, 0));
                CreateBone("RightLeg", new Vector3(center.x + 0.2f, center.y - 0.3f, 0));
                break;

            case RigType.Hierarchical:
                // Full 15-bone rig
                CreateBone("Root", center);
                CreateBone("Spine", new Vector3(center.x, center.y + 0.15f, 0));
                CreateBone("Chest", new Vector3(center.x, center.y + 0.25f, 0));
                CreateBone("Neck", new Vector3(center.x, center.y + 0.4f, 0));
                CreateBone("Head", new Vector3(center.x, center.y + 0.55f, 0));
                CreateBone("LeftShoulder", new Vector3(center.x - 0.2f, center.y + 0.25f, 0));
                CreateBone("LeftArm", new Vector3(center.x - 0.35f, center.y + 0.2f, 0));
                CreateBone("LeftForearm", new Vector3(center.x - 0.5f, center.y + 0.1f, 0));
                CreateBone("RightShoulder", new Vector3(center.x + 0.2f, center.y + 0.25f, 0));
                CreateBone("RightArm", new Vector3(center.x + 0.35f, center.y + 0.2f, 0));
                CreateBone("RightForearm", new Vector3(center.x + 0.5f, center.y + 0.1f, 0));
                CreateBone("LeftHip", new Vector3(center.x - 0.1f, center.y - 0.15f, 0));
                CreateBone("LeftLeg", new Vector3(center.x - 0.1f, center.y - 0.35f, 0));
                CreateBone("RightHip", new Vector3(center.x + 0.1f, center.y - 0.15f, 0));
                CreateBone("RightLeg", new Vector3(center.x + 0.1f, center.y - 0.35f, 0));
                break;
        }
    }

    private void CreateBone(string boneName, Vector3 position)
    {
        GameObject bone = new GameObject(boneName);
        bone.transform.SetParent(createdCharacter.transform, false);
        bone.transform.localPosition = position;
        bone.transform.localRotation = Quaternion.identity;
        bone.transform.localScale = Vector3.one;
        totalBones++;
    }

    // Public accessors for compatibility
    public GameObject GetCreatedCharacter() => createdCharacter;
    public GameObject CreatedCharacter => createdCharacter;
    public int TotalBones => totalBones;
    
    // COMPATIBILITY PROPERTIES for existing scripts (like WebcamToRiggedCharacter)
    public Sprite pngSprite 
    { 
        get => characterSprite; 
        set => characterSprite = value; 
    }
    
    public GameObject yourPNGCharacter 
    { 
        get => createdCharacter; 
        set => createdCharacter = value; 
    }
    
    public GameObject characterGameObject => createdCharacter;
    public GameObject yourPNGCharacterGameObject => createdCharacter;

    [ContextMenu("Quick Rig My Character")]
    public void QuickRigMyCharacter()
    {
        CreateRiggedCharacter();
    }

    [ContextMenu("Show Character Info")]
    public void ShowCharacterInfo()
    {
        string info = $@"
🎭 Character Creation Status:
• Sprite: {characterSprite?.name ?? "None"}
• Rig Type: {rigType}
• Auto Generate Bones: {autoGenerateBones}
• Total Bones: {totalBones}
• Has Animator: {animator != null}
• Has Physics: {createPhysics}

📋 Next Steps:
1. Adjust bone positions if needed
2. Create animation clips
3. Set up Animator states
4. Test animations

⚙️ Quick Tips:
• BoneChain rig = 6 bones (arms, legs)
• Hierarchical rig = 15 bones (full body)
• Simple rig = 3 bones (basic animation)";
        
        Debug.Log(info);
    }
}

public enum RigType
{
    Simple,        // 3 bones: Root, Body, Head
    BoneChain,     // 6 bones: LeftArm, LeftForearm, RightArm, RightForearm, LeftLeg, RightLeg  
    Hierarchical   // 15 bones: Full body structure with detailed limbs
}