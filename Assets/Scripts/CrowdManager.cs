using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central manager for crowd simulation - tracks all enemies and provides neighborhood queries
/// </summary>
public class CrowdManager : MonoBehaviour
{
    public static CrowdManager instance;

    private List<CrowdAgent> allAgents = new List<CrowdAgent>();
    
    [Header("Crowd Settings")]
    public float neighborhoodRadius = 10f;
    public int maxNeighborsConsidered = 10;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Register an agent with the crowd manager
    /// </summary>
    public void RegisterAgent(CrowdAgent agent)
    {
        if (!allAgents.Contains(agent))
        {
            allAgents.Add(agent);
        }
    }

    /// <summary>
    /// Unregister an agent from the crowd manager
    /// </summary>
    public void UnregisterAgent(CrowdAgent agent)
    {
        allAgents.Remove(agent);
    }

    /// <summary>
    /// Get all neighbors within a radius of a position
    /// </summary>
    public List<CrowdAgent> GetNeighbors(Vector3 position, float radius, CrowdAgent exclude = null)
    {
        List<CrowdAgent> neighbors = new List<CrowdAgent>();
        
        foreach (CrowdAgent agent in allAgents)
        {
            if (agent == exclude || agent == null || !agent.enabled)
                continue;

            float distance = Vector3.Distance(position, agent.transform.position);
            if (distance <= radius)
            {
                neighbors.Add(agent);
            }
        }

        // Sort by distance and limit to max neighbors
        neighbors.Sort((a, b) => 
        {
            float distA = Vector3.Distance(position, a.transform.position);
            float distB = Vector3.Distance(position, b.transform.position);
            return distA.CompareTo(distB);
        });

        if (neighbors.Count > maxNeighborsConsidered)
        {
            neighbors = neighbors.GetRange(0, maxNeighborsConsidered);
        }

        return neighbors;
    }

    /// <summary>
    /// Get all agents in the crowd
    /// </summary>
    public List<CrowdAgent> GetAllAgents()
    {
        return new List<CrowdAgent>(allAgents);
    }

    /// <summary>
    /// Get the number of agents currently in the crowd
    /// </summary>
    public int GetAgentCount()
    {
        return allAgents.Count;
    }
}
