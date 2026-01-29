using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
public class CheckpointController : MonoBehaviour
{

    [Header("Unique name for this checkpoint in the scene")]
    public string checkpointName;

    private string Key =>
        SceneManager.GetActiveScene().name + "_checkpoint";

    private IEnumerator Start()
    {
        // Wait one frame so physics & player are initialized
        yield return null;

        if (!PlayerPrefs.HasKey(Key))
            yield break;

        if (PlayerPrefs.GetString(Key) != checkpointName)
            yield break;

        TeleportPlayer();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        PlayerPrefs.SetString(Key, checkpointName);
        PlayerPrefs.Save();
    }

    private void TeleportPlayer()
    {
        GameObject player = PlayerController.instance.gameObject;

        // Small vertical offset prevents floor clipping
        Vector3 spawnPos = transform.position + Vector3.up * 0.1f;

        // If using CharacterController
        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null)
        {
            cc.enabled = false;
            player.transform.position = spawnPos;
            cc.enabled = true;
            return;
        }

        // If using Rigidbody
        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.position = spawnPos;
            return;
        }

        // Fallback
        player.transform.position = spawnPos;
    }
}