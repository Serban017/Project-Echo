using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Portal/Level exit that requires code entry before allowing passage
/// </summary>
public class LevelExit : MonoBehaviour
{
    [Header("Level Settings")]
    public string nextLevel;
    
    [Header("Code Requirement")]
    [Tooltip("If true, player must enter code before portal works")]
    public bool requiresCode = true;
    
    [Header("Interaction Settings")]
    public float interactionDistance = 3f;
    public KeyCode interactionKey = KeyCode.E;
    
    [Header("Portal Control")]
    private bool isUnlocked = false;
    private bool isPlayerInRange = false;
    private GameObject playerInTrigger = null;
    
    [Header("References")]
    public CodeEntryUI codeEntryUI; // Assign in inspector
    
    void Start()
    {
        // Find CodeEntryUI if not assigned
        if (codeEntryUI == null)
        {
            codeEntryUI = FindObjectOfType<CodeEntryUI>();
            if (codeEntryUI == null && requiresCode)
            {
                Debug.LogError("[LevelExit] CodeEntryUI not found! Assign it in the inspector or add it to the scene.");
            }
        }
        
        Debug.Log($"[LevelExit] Initialized. Requires code: {requiresCode}, Unlocked: {isUnlocked}");
    }

    void Update()
    {
        if (!requiresCode || isUnlocked)
        {
            // Portal is always accessible
            return;
        }
        
        // Check for player in range
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogWarning("[LevelExit] Player not found! Make sure your player has the 'Player' tag.");
            return;
        }
        
        float distance = Vector3.Distance(transform.position, player.transform.position);
        bool wasInRange = isPlayerInRange;
        isPlayerInRange = distance <= interactionDistance;
        
        // Debug when player enters range
        if (isPlayerInRange && !wasInRange)
        {
            Debug.Log($"[LevelExit] Player in range! Distance: {distance:F2}. Press E to interact.");
        }
        
        // Handle interaction
        if (isPlayerInRange && Input.GetKeyDown(interactionKey))
        {
            Debug.Log("[LevelExit] E key pressed!");
            TryOpenCodeEntry();
        }
    }

    void TryOpenCodeEntry()
    {
        CodeManager codeManager = FindObjectOfType<CodeManager>();
        
        if (codeManager == null)
        {
            Debug.LogError("[LevelExit] CodeManager not found in scene!");
            return;
        }
        
        // Check if all numbers collected
        if (!codeManager.AreAllNumbersCollected())
        {
            int collected = codeManager.GetCollectedNumbers().Count;
            int total = codeManager.totalNumbersToCollect;
            Debug.Log($"[LevelExit] Cannot access portal - only {collected}/{total} numbers collected");
            return;
        }
        
        // Open code entry UI
        if (codeEntryUI != null)
        {
            Debug.Log("[LevelExit] Opening code entry UI...");
            codeEntryUI.OpenCodeEntry(OnCodeCorrect);
        }
        else
        {
            Debug.LogError("[LevelExit] CodeEntryUI reference is missing!");
        }
    }

    void OnCodeCorrect()
    {
        Debug.Log("[LevelExit] ✓ Correct code entered! Portal unlocked!");
        isUnlocked = true;
        
        // If player is still in trigger, load next level
        if (playerInTrigger != null)
        {
            LoadNextLevel();
        }
    }

    void LoadNextLevel()
    {
        if (string.IsNullOrEmpty(nextLevel))
        {
            Debug.LogWarning("[LevelExit] No next level specified!");
            return;
        }
        
        Debug.Log($"[LevelExit] Loading next level: {nextLevel}");
        SceneManager.LoadScene(nextLevel);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            playerInTrigger = other.gameObject;
            
            // If code not required or already unlocked, load immediately
            if (!requiresCode || isUnlocked)
            {
                LoadNextLevel();
            }
            else
            {
                Debug.Log("[LevelExit] Player entered portal trigger - code required!");
            }
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Player")
        {
            playerInTrigger = null;
        }
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw interaction radius
        Gizmos.color = isUnlocked ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
    }
}
