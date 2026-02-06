using UnityEngine;
using TMPro;

/// <summary>
/// Collectible number that adds to the escape code sequence
/// Place this on objects around the map
/// </summary>
public class CollectibleNumber : MonoBehaviour
{
    [Header("Number Settings")]
    [Tooltip("The number digit this collectible represents (0-9)")]
    public int numberDigit = 0;
    
    [Header("Visual Settings")]
    public TextMeshPro numberDisplay; // Optional: 3D text to show the number
    public Color glowColor = Color.yellow;
    public float rotationSpeed = 50f;
    public float floatSpeed = 1f;
    public float floatAmplitude = 0.3f;
    
    [Header("Collection Settings")]
    public float collectionRadius = 3f;
    public AudioClip collectionSound;
    public GameObject collectionEffect; // Optional particle effect
    
    private Vector3 startPosition;
    private bool isCollected = false;
    private bool isShowingPrompt = false;
    private AudioSource audioSource;

    void Start()
    {
        startPosition = transform.position;
        
        Debug.Log($"[CollectibleNumber] {gameObject.name} initialized with digit: {numberDigit}");
        
        // Setup audio source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f; // 3D sound
        
        // Setup number display if available
        if (numberDisplay != null)
        {
            numberDisplay.text = numberDigit.ToString();
            numberDisplay.color = glowColor;
        }
        
        // Apply glow to material
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null && renderer.material != null)
        {
            renderer.material.EnableKeyword("_EMISSION");
            renderer.material.SetColor("_EmissionColor", glowColor * 0.8f);
        }
    }

    void Update()
    {
        if (isCollected) return;
        
        // Rotation animation
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        
        // Float animation
        float newY = startPosition.y + Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
        transform.position = new Vector3(startPosition.x, newY, startPosition.z);
        
        // Check for player proximity
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;
        
        float distance = Vector3.Distance(transform.position, player.transform.position);
        
        if (distance <= collectionRadius && !isCollected)
        {
            if (!isShowingPrompt)
            {
                Debug.Log($"[CollectibleNumber] Player near number {numberDigit}");
                isShowingPrompt = true;
            }
            
            // Check for E key press to collect
            if (Input.GetKeyDown(KeyCode.E))
            {
                CollectNumber();
            }
        }
        else if (distance > collectionRadius && isShowingPrompt)
        {
            isShowingPrompt = false;
        }
    }

    void CollectNumber()
    {
        if (isCollected) return;
        
        isCollected = true;
        
        Debug.Log($"[CollectibleNumber] Collected number: {numberDigit}");
        
        // Notify the code manager
        CodeManager codeManager = FindObjectOfType<CodeManager>();
        if (codeManager != null)
        {
            codeManager.CollectNumber(numberDigit);
        }
        else
        {
            Debug.LogError("[CollectibleNumber] CodeManager not found in scene!");
        }
        
        // Play collection sound
        if (collectionSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(collectionSound);
        }
        
        // Spawn collection effect
        if (collectionEffect != null)
        {
            Instantiate(collectionEffect, transform.position, Quaternion.identity);
        }
        
        // Destroy after delay to allow sound to play
        Destroy(gameObject, 0.5f);
    }

    void OnDrawGizmosSelected()
    {
        // Draw collection radius in editor
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, collectionRadius);
    }
}
