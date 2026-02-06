using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the collection of numbers and code verification
/// Add this to a GameObject in your scene (e.g., GameManager)
/// </summary>
public class CodeManager : MonoBehaviour
{
    [Header("Code Settings")]
    [Tooltip("Total numbers the player needs to collect")]
    public int totalNumbersToCollect = 4;
    
    [Header("Collected Code (Read Only)")]
    [SerializeField] private List<int> collectedNumbers = new List<int>();
    
    [Header("Debug")]
    public bool showDebugInfo = true;

    void Start()
    {
        collectedNumbers = new List<int>();
        
        if (showDebugInfo)
        {
            Debug.Log($"[CodeManager] Initialized. Player needs to collect {totalNumbersToCollect} numbers.");
        }
    }

    /// <summary>
    /// Called when a number is collected by the player
    /// </summary>
    public void CollectNumber(int number)
    {
        if (collectedNumbers.Count >= totalNumbersToCollect)
        {
            Debug.LogWarning("[CodeManager] Already collected all numbers!");
            return;
        }

        collectedNumbers.Add(number);
        
        Debug.Log($"[CodeManager] Collected number: {number} ({collectedNumbers.Count}/{totalNumbersToCollect})");
        Debug.Log($"[CodeManager] Current code sequence: {GetCodeString()}");
        
        // Check if all numbers collected
        if (AreAllNumbersCollected())
        {
            Debug.Log($"[CodeManager] ✓ ALL NUMBERS COLLECTED! Final code: {GetCodeString()}");
        }
    }

    /// <summary>
    /// Get the current code as a string
    /// </summary>
    public string GetCodeString()
    {
        string code = "";
        foreach (int number in collectedNumbers)
        {
            code += number.ToString();
        }
        return code;
    }

    /// <summary>
    /// Get the collected numbers list (copy)
    /// </summary>
    public List<int> GetCollectedNumbers()
    {
        return new List<int>(collectedNumbers);
    }

    /// <summary>
    /// Check if all numbers are collected
    /// </summary>
    public bool AreAllNumbersCollected()
    {
        return collectedNumbers.Count >= totalNumbersToCollect;
    }

    /// <summary>
    /// Verify if entered code matches the collected sequence
    /// </summary>
    public bool VerifyCode(string enteredCode)
    {
        string correctCode = GetCodeString();
        bool isCorrect = enteredCode.Trim() == correctCode;
        
        if (showDebugInfo)
        {
            Debug.Log($"[CodeManager] Code verification - Entered: '{enteredCode}', Correct: '{correctCode}', Match: {isCorrect}");
        }
        
        return isCorrect;
    }

    /// <summary>
    /// Reset the collected numbers (for restart/new game)
    /// </summary>
    public void ResetCode()
    {
        collectedNumbers.Clear();
        Debug.Log("[CodeManager] Code reset.");
    }

    /// <summary>
    /// Get hint for player (shows how many numbers collected)
    /// </summary>
    public string GetProgressHint()
    {
        return $"Numbers collected: {collectedNumbers.Count}/{totalNumbersToCollect}";
    }
}
