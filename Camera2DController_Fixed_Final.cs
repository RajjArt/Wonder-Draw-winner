using UnityEngine;
using UnityEditor;
using System.Collections;

/// <summary>
/// 2D Camera controller for pixel-perfect rendering and character following
/// Supports smooth camera movement, screen shake, zoom, and multiple camera modes
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class Camera2DController : MonoBehaviour
{
    [Header("🎯 Target & Movement")]
    [Tooltip("Character to follow (auto-detected if not assigned)")]
    public Transform target;
    
    [Tooltip("Camera follow mode")]
    public CameraFollowMode followMode = CameraFollowMode.Smooth;
    
    [Tooltip("How fast camera moves toward target")]
    public float followSpeed = 5f;
    
    [Tooltip("Camera position offset from target")]
    public Vector3 positionOffset = new Vector3(0, 2, -10);
    
    [Header("📐 View Settings")]
    [Tooltip("Camera orthographic size (size of view in world units)")]
    public float orthographicSize = 5f;
    
    [Tooltip("Set camera to orthographic mode")]
    public bool forceOrthographic = true;
    
    [Tooltip("Maintain aspect ratio (prevents distortion)")]
    public bool maintainAspectRatio = true;
    
    [Header("🎮 Advanced Follow")]
    [Tooltip("Look ahead distance for target prediction")]
    public float lookAheadDistance = 2f;
    
    [Tooltip("Time to look ahead (seconds)")]
    public float lookAheadTime = 0.5f;
    
    [Tooltip("Smoothing factor for look ahead")]
    public float lookAheadSmoothing = 1f;
    
    [Header("🎨 Screen Shake")]
    [Tooltip("Enable screen shake effects")]
    public bool enableScreenShake = true;
    
    [Tooltip("Base shake intensity")]
    public float baseShakeIntensity = 0.1f;
    
    [Tooltip("Shake duration in seconds")]
    public float shakeDuration = 0.5f;
    
    [Header("📊 Camera Limits")]
    [Tooltip("Enable camera boundaries")]
    public bool enableBounds = false;
    
    [Tooltip("Camera boundary center")]
    public Vector2 boundsCenter = Vector2.zero;
    
    [Tooltip("Camera boundary size")]
    public Vector2 boundsSize = new Vector2(20, 20);
    
    private Vector3 targetVelocity;
    private Vector3 shakeOffset;
    private float shakeTime;
    private float currentShakeIntensity;
    
    private void Start()
    {
        SetupCamera();
        AutoDetectTarget();
    }
    
    private void Update()
    {
        if (Application.isPlaying)
        {
            HandleCameraMovement();
            HandleScreenShake();
            HandlePixelPerfectRendering();
            HandleResolutionScaling();
        }
    }
    
    private void AutoDetectTarget()
    {
        // Look for character with Rigidbody2D first
        var rb2d = FindFirstObjectByType<Rigidbody2D>();
        if (rb2d != null)
        {
            target = rb2d.transform;
            return;
        }
        
        // Look for character with Animator
        var animator = FindFirstObjectByType<Animator>();
        if (animator != null)
        {
            target = animator.transform;
            return;
        }
        
        // Look for character with SpriteRenderer
        var spriteRenderer = FindFirstObjectByType<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            target = spriteRenderer.transform;
            return;
        }
    }
    
    private void HandleCameraMovement()
    {
        if (target == null) return;
        
        Vector3 desiredPosition = target.position + positionOffset;
        
        switch (followMode)
        {
            case CameraFollowMode.Instant:
                transform.position = desiredPosition;
                break;
                
            case CameraFollowMode.Smooth:
                transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref targetVelocity, 1f / followSpeed);
                break;
                
            case CameraFollowMode.LookAhead:
                Vector3 lookAheadPos = target.position + positionOffset;
                
                if (targetVelocity.magnitude > 0.1f)
                {
                    Vector3 velocityDirection = targetVelocity.normalized;
                    lookAheadPos += velocityDirection * lookAheadDistance * lookAheadSmoothing;
                }
                
                transform.position = Vector3.SmoothDamp(transform.position, lookAheadPos, ref targetVelocity, 1f / followSpeed);
                break;
                
            case CameraFollowMode.Bounded:
                Vector3 boundedPos = new Vector3(
                    Mathf.Clamp(desiredPosition.x, boundsCenter.x - boundsSize.x / 2, boundsCenter.x + boundsSize.x / 2),
                    Mathf.Clamp(desiredPosition.y, boundsCenter.y - boundsSize.y / 2, boundsCenter.y + boundsSize.y / 2),
                    desiredPosition.z
                );
                transform.position = Vector3.SmoothDamp(transform.position, boundedPos, ref targetVelocity, 1f / followSpeed);
                break;
        }
        
        // Add screen shake offset
        transform.position += shakeOffset;
    }
    
    private void HandleScreenShake()
    {
        if (!enableScreenShake || shakeTime <= 0) 
        {
            shakeOffset = Vector3.zero;
            return;
        }
        
        float shakeX = Random.Range(-1f, 1f) * currentShakeIntensity;
        float shakeY = Random.Range(-1f, 1f) * currentShakeIntensity;
        
        shakeOffset = new Vector3(shakeX, shakeY, 0);
        
        shakeTime -= Time.deltaTime;
        currentShakeIntensity = Mathf.Lerp(currentShakeIntensity, 0, Time.deltaTime * 2f);
    }
    
    private void HandlePixelPerfectRendering()
    {
        if (forceOrthographic)
        {
            var camera = GetComponent<Camera>();
            if (camera != null && !camera.orthographic)
            {
                camera.orthographic = true;
                camera.orthographicSize = orthographicSize;
            }
        }
        
        if (maintainAspectRatio)
        {
            var camera = GetComponent<Camera>();
            if (camera != null)
            {
                // Snap to pixel grid for crisp rendering
                Vector3 snappedPosition = transform.position;
                snappedPosition.x = Mathf.Round(snappedPosition.x * 100f) / 100f;
                snappedPosition.y = Mathf.Round(snappedPosition.y * 100f) / 100f;
                transform.position = snappedPosition;
            }
        }
    }
    
    private void HandleResolutionScaling()
    {
        // Update camera size based on screen resolution
        var camera = GetComponent<Camera>();
        if (camera != null && maintainAspectRatio)
        {
            float aspectRatio = (float)Screen.width / Screen.height;
            camera.orthographicSize = orthographicSize * Mathf.Max(1f, aspectRatio / 1.777f);
        }
    }
    
    private void SetupCamera()
    {
        var camera = GetComponent<Camera>();
        if (camera != null)
        {
            camera.orthographic = forceOrthographic;
            camera.orthographicSize = orthographicSize;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);
        }
    }
    
    [Tooltip("Trigger screen shake")]
    public void TriggerShake(float intensity = -1f, float duration = -1f)
    {
        if (intensity > 0) currentShakeIntensity = intensity;
        if (duration > 0) shakeTime = duration;
        else shakeTime = shakeDuration;
    }
    
    [Tooltip("Set camera target")]
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
    
    [Tooltip("Set camera bounds")]
    public void SetBounds(Vector2 center, Vector2 size)
    {
        enableBounds = true;
        boundsCenter = center;
        boundsSize = size;
    }
    
    [ContextMenu("Auto Detect Target")]
    public void AutoDetectTargetContext()
    {
        AutoDetectTarget();
    }
    
    [ContextMenu("Trigger Screen Shake")]
    public void TriggerShakeContext()
    {
        TriggerShake();
    }
}

/// <summary>
/// Camera follow modes for different gameplay styles
/// </summary>
public enum CameraFollowMode
{
    Instant,     // Immediate follow without smoothing
    Smooth,      // Smooth follow with customizable speed
    LookAhead,   // Smooth follow with movement prediction
    Bounded      // Follow within specified boundaries
}
