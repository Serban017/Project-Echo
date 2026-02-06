using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Handles the UI for code entry at the portal
/// Attach this to the Canvas that contains the code entry UI
/// </summary>
public class CodeEntryUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject codeEntryPanel;
    public TMP_InputField codeInputField;
    public Button submitButton;
    public Button cancelButton;
    public TextMeshProUGUI feedbackText;
    public TextMeshProUGUI instructionText;
    
    [Header("Visual Feedback")]
    public Color correctColor = Color.green;
    public Color incorrectColor = Color.red;
    public float feedbackDisplayTime = 2f;
    
    [Header("Audio")]
    public AudioClip correctSound;
    public AudioClip incorrectSound;
    
    private bool isOpen = false;
    private AudioSource audioSource;
    private System.Action onCodeCorrect; // Callback when correct code is entered

    void Awake()
    {
        // FORCE hide panel immediately on awake (before Start)
        if (codeEntryPanel != null)
        {
            codeEntryPanel.SetActive(false);
        }
    }
    
    void Start()
    {
        // Setup audio
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        
        // Double-check panel is hidden
        if (codeEntryPanel != null)
        {
            codeEntryPanel.SetActive(false);
            Debug.Log("[CodeEntryUI] Panel hidden at start");
        }
        else
        {
            Debug.LogError("[CodeEntryUI] Code Entry Panel not assigned!");
        }
        
        // Setup button listeners
        if (submitButton != null)
        {
            submitButton.onClick.AddListener(OnSubmitCode);
        }
        if (cancelButton != null)
        {
            cancelButton.onClick.AddListener(CloseCodeEntry);
        }
        
        // Setup instruction text
        if (instructionText != null)
        {
            instructionText.text = "Enter the code from collected numbers\n(in order of collection)";
        }
    }

    void Update()
    {
        // Allow ESC to close panel
        if (isOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseCodeEntry();
        }
        
        // Allow Enter to submit
        if (isOpen && Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            OnSubmitCode();
        }
    }

    /// <summary>
    /// Open the code entry panel
    /// </summary>
    public void OpenCodeEntry(System.Action onCorrectCallback = null)
    {
        CodeManager codeManager = FindObjectOfType<CodeManager>();
        
        // Check if player has collected all numbers
        if (codeManager == null || !codeManager.AreAllNumbersCollected())
        {
            int collected = codeManager != null ? codeManager.GetCollectedNumbers().Count : 0;
            int total = codeManager != null ? codeManager.totalNumbersToCollect : 4;
            Debug.Log($"[CodeEntryUI] Cannot open - only {collected}/{total} numbers collected");
            return;
        }
        
        Debug.Log("[CodeEntryUI] Opening code entry panel");
        
        isOpen = true;
        onCodeCorrect = onCorrectCallback;
        
        // Show panel
        if (codeEntryPanel != null)
        {
            codeEntryPanel.SetActive(true);
        }
        
        // Clear and focus input field
        if (codeInputField != null)
        {
            codeInputField.text = "";
            codeInputField.Select();
            codeInputField.ActivateInputField();
        }
        
        // Clear feedback
        if (feedbackText != null)
        {
            feedbackText.text = "";
        }
        
        // Show cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        // Pause game (optional - comment out if you don't want this)
        Time.timeScale = 0f;
    }

    /// <summary>
    /// Close the code entry panel
    /// </summary>
    public void CloseCodeEntry()
    {
        isOpen = false;
        
        // Hide panel
        if (codeEntryPanel != null)
        {
            codeEntryPanel.SetActive(false);
        }
        
        // Lock cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        // Unpause game
        Time.timeScale = 1f;
        
        Debug.Log("[CodeEntryUI] Code entry panel closed");
    }

    void OnSubmitCode()
    {
        if (codeInputField == null) return;
        
        CodeManager codeManager = FindObjectOfType<CodeManager>();
        if (codeManager == null)
        {
            Debug.LogError("[CodeEntryUI] CodeManager not found!");
            return;
        }
        
        string enteredCode = codeInputField.text.Trim();
        
        if (string.IsNullOrEmpty(enteredCode))
        {
            ShowFeedback("Please enter a code!", incorrectColor);
            return;
        }
        
        // Verify code
        bool isCorrect = codeManager.VerifyCode(enteredCode);
        
        if (isCorrect)
        {
            OnCorrectCode();
        }
        else
        {
            OnIncorrectCode();
        }
    }

    void OnCorrectCode()
    {
        Debug.Log("[CodeEntryUI] ✓ CORRECT CODE!");
        
        ShowFeedback("ACCESS GRANTED!", correctColor);
        
        // Play correct sound
        if (correctSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(correctSound);
        }
        
        // Call the callback after a short delay
        StartCoroutine(CloseAfterDelay());
    }

    void OnIncorrectCode()
    {
        Debug.Log("[CodeEntryUI] ✗ INCORRECT CODE");
        
        ShowFeedback("INCORRECT CODE - Try again!", incorrectColor);
        
        // Play incorrect sound
        if (incorrectSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(incorrectSound);
        }
        
        // Clear input field
        if (codeInputField != null)
        {
            codeInputField.text = "";
            codeInputField.Select();
        }
    }

    void ShowFeedback(string message, Color color)
    {
        if (feedbackText != null)
        {
            feedbackText.text = message;
            feedbackText.color = color;
        }
    }

    System.Collections.IEnumerator CloseAfterDelay()
    {
        // Wait in unscaled time (works even when paused)
        yield return new WaitForSecondsRealtime(1.5f);
        
        CloseCodeEntry();
        
        // Trigger the callback (portal opens)
        if (onCodeCorrect != null)
        {
            onCodeCorrect.Invoke();
        }
    }
}
