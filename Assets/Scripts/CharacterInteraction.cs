using UnityEngine;
using UnityEngine.UI;
using System;

namespace CharacterInteractionSystem
{
    /// <summary>
    /// Component for handling touch/motion interactions on 3D character objects
    /// Requires a Collider component to function properly
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class CharacterInteraction : MonoBehaviour
    {
        [Header("Character Settings")]
        [SerializeField] private string characterName = "Character";
        [SerializeField] private Color interactionColor = Color.yellow;
        [SerializeField] private float visualFeedbackDuration = 0.5f;
        
        [Header("Cooldown Settings")]
        [SerializeField] private float interactionCooldown = 1.0f;
        [SerializeField] private bool enableCooldown = true;
        
        [Header("UI Settings")]
        [SerializeField] private bool autoCreateWorldCanvas = true;
        [SerializeField] private Vector3 worldCanvasOffset = Vector3.up * 2f;
        
        // Public properties
        public string CharacterName => characterName;
        public bool IsOnCooldown => enableCooldown && Time.time < _nextInteractionTime;
        
        // Events
        public event Action<CharacterInteraction> OnCharacterTouched;
        public event Action<CharacterInteraction> OnCharacterHover;
        public event Action<CharacterInteraction> OnCharacterReleased;
        public event Action<CharacterInteraction> OnInteractionTriggered;
        
        // Private fields
        private Renderer _characterRenderer;
        private Color _originalColor;
        private float _nextInteractionTime = 0f;
        private GameObject _worldCanvas;
        private Text _feedbackText;
        private Canvas _canvasComponent;
        private Material _characterMaterial;
        private bool _isHovering = false;
        
        private void Awake()
        {
            InitializeCharacterInteraction();
        }
        
        private void Start()
        {
            SetupWorldCanvas();
        }
        
        /// <summary>
        /// Initialize the character interaction component
        /// </summary>
        private void InitializeCharacterInteraction()
        {
            // Get or find the renderer component
            _characterRenderer = GetComponent<Renderer>();
            
            // Try to find a child renderer if main object doesn't have one
            if (_characterRenderer == null)
            {
                _characterRenderer = GetComponentInChildren<Renderer>();
            }
            
            if (_characterRenderer != null)
            {
                // Create a material instance to avoid modifying shared material
                _characterMaterial = _characterRenderer.material;
                _originalColor = _characterMaterial.color;
            }
            else
            {
                Debug.LogWarning($"CharacterInteraction: No Renderer found on {gameObject.name}");
            }
        }
        
        /// <summary>
        /// Setup world space canvas for UI feedback
        /// </summary>
        private void SetupWorldCanvas()
        {
            if (!autoCreateWorldCanvas) return;
            
            // Create world canvas
            _worldCanvas = new GameObject($"{characterName}_WorldCanvas");
            _worldCanvas.transform.SetParent(transform);
            _worldCanvas.transform.localPosition = worldCanvasOffset;
            
            // Add canvas component
            _canvasComponent = _worldCanvas.AddComponent<Canvas>();
            _canvasComponent.renderMode = RenderMode.WorldSpace;
            _canvasComponent.sortingOrder = 100;
            
            // Set canvas size and scale
            RectTransform canvasRect = _worldCanvas.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(2f, 0.5f);
            canvasRect.localScale = Vector3.one * 0.01f;
            
            // Add canvas scaler for better UI scaling
            CanvasScaler scaler = _worldCanvas.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 10f;
            
            // Add graphic raycaster for UI interactions
            _worldCanvas.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            
            // Create background for text
            GameObject background = new GameObject("Background");
            background.transform.SetParent(_worldCanvas.transform, false);
            RectTransform bgRect = background.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            
            Image bgImage = background.AddComponent<Image>();
            bgImage.color = new Color(0f, 0f, 0f, 0.7f);
            
            // Create text component
            GameObject textObj = new GameObject("FeedbackText");
            textObj.transform.SetParent(_worldCanvas.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            _feedbackText = textObj.AddComponent<Text>();
            _feedbackText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _feedbackText.fontSize = 50;
            _feedbackText.alignment = TextAnchor.MiddleCenter;
            _feedbackText.color = Color.white;
            _feedbackText.text = "";
            
            // Initially hide the canvas
            _worldCanvas.SetActive(false);
        }
        
        /// <summary>
        /// Called when character is touched/hovered
        /// </summary>
        public void HandleCharacterTouched()
        {
            if (IsOnCooldown) return;
            
            _isHovering = true;
            ShowVisualFeedback();
            OnCharacterTouched?.Invoke(this);
        }
        
        /// <summary>
        /// Called when character is hovered over
        /// </summary>
        public void HandleCharacterHover()
        {
            if (_isHovering) return;
            
            _isHovering = true;
            OnCharacterHover?.Invoke(this);
            
            // Show subtle feedback for hover
            ShowSubtleFeedback();
            ShowHoverFeedback();
        }
        
        /// <summary>
        /// Called when character is released/no longer touched
        /// </summary>
        public void HandleCharacterReleased()
        {
            _isHovering = false;
            OnCharacterReleased?.Invoke(this);
            ResetVisualFeedback();
        }
        
        /// <summary>
        /// Trigger interaction with cooldown check
        /// </summary>
        public void TriggerInteraction()
        {
            if (IsOnCooldown)
            {
                ShowCooldownFeedback();
                return;
            }
            
            // Set cooldown
            if (enableCooldown)
            {
                _nextInteractionTime = Time.time + interactionCooldown;
            }
            
            // Trigger event
            OnInteractionTriggered?.Invoke(this);
            
            // Show interaction feedback
            ShowInteractionFeedback();
        }
        
        /// <summary>
        /// Force trigger interaction ignoring cooldown
        /// </summary>
        public void TriggerInteractionForced()
        {
            OnInteractionTriggered?.Invoke(this);
            ShowInteractionFeedback();
        }
        
        /// <summary>
        /// Show visual feedback for interaction
        /// </summary>
        private void ShowVisualFeedback()
        {
            if (_characterMaterial != null)
            {
                // Smooth color transition
                StopAllCoroutines();
                StartCoroutine(SmoothColorTransition(interactionColor, visualFeedbackDuration));
            }
            
            // Show feedback text
            ShowInteractionFeedback();
        }
        
        /// <summary>
        /// Show subtle feedback for hover
        /// </summary>
        private void ShowSubtleFeedback()
        {
            if (_characterMaterial != null)
            {
                Color hoverColor = Color.Lerp(_originalColor, interactionColor, 0.3f);
                _characterMaterial.color = hoverColor;
            }
        }
        
        /// <summary>
        /// Reset visual feedback to original state
        /// </summary>
        private void ResetVisualFeedback()
        {
            if (_characterMaterial != null)
            {
                _characterMaterial.color = _originalColor;
            }
        }
        
        /// <summary>
        /// Show interaction feedback text
        /// </summary>
        private void ShowInteractionFeedback()
        {
            if (_worldCanvas == null || _feedbackText == null) return;
            
            _feedbackText.text = $"✨ {characterName} ✨";
            _worldCanvas.SetActive(true);
            
            // Hide after duration
            CancelInvoke("HideFeedback");
            Invoke("HideFeedback", visualFeedbackDuration);
        }
        
        /// <summary>
        /// Show cooldown feedback
        /// </summary>
        private void ShowCooldownFeedback()
        {
            if (_worldCanvas == null || _feedbackText == null) return;
            
            float remainingCooldown = _nextInteractionTime - Time.time;
            _feedbackText.text = $"⏰ Wait {remainingCooldown:F1}s";
            _worldCanvas.SetActive(true);
            
            // Clear any existing invokes
            CancelInvoke("HideFeedback");
            Invoke("HideFeedback", 1.0f);
        }
        
        /// <summary>
        /// Show subtle hover feedback
        /// </summary>
        private void ShowHoverFeedback()
        {
            if (_worldCanvas == null || _feedbackText == null) return;
            
            _feedbackText.text = $"👋 {characterName}";
            _worldCanvas.SetActive(true);
            
            // Clear any existing invokes
            CancelInvoke("HideFeedback");
            Invoke("HideFeedback", 1.5f);
        }
        
        /// <summary>
        /// Hide feedback UI
        /// </summary>
        private void HideFeedback()
        {
            if (_worldCanvas != null)
            {
                _worldCanvas.SetActive(false);
            }
        }
        
        /// <summary>
        /// Smooth color transition coroutine
        /// </summary>
        private System.Collections.IEnumerator SmoothColorTransition(Color targetColor, float duration)
        {
            Color startColor = _characterMaterial.color;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                _characterMaterial.color = Color.Lerp(startColor, targetColor, t);
                yield return null;
            }
            
            // Reset to original color
            _characterMaterial.color = _originalColor;
        }
        
        /// <summary>
        /// Set character name at runtime
        /// </summary>
        public void SetCharacterName(string newName)
        {
            characterName = newName;
            if (_worldCanvas != null)
            {
                _worldCanvas.name = $"{characterName}_WorldCanvas";
            }
        }
        
        /// <summary>
        /// Set interaction cooldown at runtime
        /// </summary>
        public void SetCooldown(float newCooldown)
        {
            interactionCooldown = Mathf.Max(0f, newCooldown);
        }
        
        /// <summary>
        /// Enable or disable cooldown system
        /// </summary>
        public void SetCooldownEnabled(bool enabled)
        {
            enableCooldown = enabled;
        }
        
        /// <summary>
        /// Get remaining cooldown time
        /// </summary>
        public float GetRemainingCooldown()
        {
            return IsOnCooldown ? _nextInteractionTime - Time.time : 0f;
        }
        
        /// <summary>
        /// Reset cooldown manually
        /// </summary>
        public void ResetCooldown()
        {
            _nextInteractionTime = 0f;
        }
        
        private void OnDrawGizmosSelected()
        {
            // Draw world canvas offset in editor
            Gizmos.color = Color.yellow;
            Vector3 worldCanvasPos = transform.position + worldCanvasOffset;
            Gizmos.DrawWireSphere(worldCanvasPos, 0.1f);
            Gizmos.DrawLine(transform.position, worldCanvasPos);
        }
        
        private void OnValidate()
        {
            // Validate settings in editor
            if (interactionCooldown < 0f) interactionCooldown = 0f;
            if (visualFeedbackDuration < 0f) visualFeedbackDuration = 0f;
        }
        
        private void OnDestroy()
        {
            // Stop all coroutines
            StopAllCoroutines();
            
            // Clean up material instance (only if created by this component)
            if (_characterMaterial != null && Application.isPlaying)
            {
                Destroy(_characterMaterial);
            }
            
            // Clean up world canvas
            if (_worldCanvas != null && Application.isPlaying)
            {
                Destroy(_worldCanvas);
            }
        }
    }
}
