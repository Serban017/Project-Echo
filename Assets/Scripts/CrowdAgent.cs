using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Base class for agents participating in crowd simulation with flocking behavior
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public abstract class CrowdAgent : MonoBehaviour
{
    [Header("Crowd Behavior Settings")]
    [Range(0f, 5f)]
    public float cohesionWeight = 1f; // Stay close to group
    
    [Range(0f, 5f)]
    public float separationWeight = 2f; // Avoid crowding neighbors
    
    [Range(0f, 5f)]
    public float alignmentWeight = 1f; // Move in same direction as neighbors
    
    public float separationRadius = 3f; // Personal space
    public float neighborhoodRadius = 10f; // How far to look for neighbors
    
    [Header("Coordination Settings")]
    public bool shareTargetInfo = true; // Share target location with nearby agents
    public float targetSharingRadius = 15f;
    public bool coordinateAttacks = true; // Coordinate attack timing
    
    [Header("Formation Settings")]
    public bool useFormations = true;
    public float formationStrength = 0.5f; // How strongly to maintain formation
    
    protected NavMeshAgent agent;
    protected Vector3 desiredVelocity;
    public bool isInCombat = false;
    
    // Shared information
    protected Vector3? sharedTargetPosition;
    protected float lastTargetShareTime;

    protected virtual void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        
        // Register with crowd manager
        if (CrowdManager.instance != null)
        {
            CrowdManager.instance.RegisterAgent(this);
        }
    }

    protected virtual void OnDestroy()
    {
        // Unregister from crowd manager
        if (CrowdManager.instance != null)
        {
            CrowdManager.instance.UnregisterAgent(this);
        }
    }

    /// <summary>
    /// Calculate flocking behavior and return steering direction
    /// </summary>
    protected Vector3 CalculateFlockingBehavior()
    {
        if (CrowdManager.instance == null)
            return Vector3.zero;

        var neighbors = CrowdManager.instance.GetNeighbors(transform.position, neighborhoodRadius, this);
        
        if (neighbors.Count == 0)
            return Vector3.zero;

        Vector3 cohesion = CalculateCohesion(neighbors);
        Vector3 separation = CalculateSeparation(neighbors);
        Vector3 alignment = CalculateAlignment(neighbors);

        // Combine behaviors with weights
        Vector3 steeringForce = Vector3.zero;
        steeringForce += cohesion * cohesionWeight;
        steeringForce += separation * separationWeight;
        steeringForce += alignment * alignmentWeight;

        return steeringForce;
    }

    /// <summary>
    /// Cohesion: steer towards the average position of neighbors
    /// </summary>
    private Vector3 CalculateCohesion(System.Collections.Generic.List<CrowdAgent> neighbors)
    {
        if (neighbors.Count == 0)
            return Vector3.zero;

        Vector3 centerOfMass = Vector3.zero;
        foreach (var neighbor in neighbors)
        {
            centerOfMass += neighbor.transform.position;
        }
        centerOfMass /= neighbors.Count;

        Vector3 direction = centerOfMass - transform.position;
        direction.y = 0; // Keep on ground plane
        return direction.normalized;
    }

    /// <summary>
    /// Separation: steer away from neighbors that are too close
    /// </summary>
    private Vector3 CalculateSeparation(System.Collections.Generic.List<CrowdAgent> neighbors)
    {
        Vector3 steer = Vector3.zero;
        int count = 0;

        foreach (var neighbor in neighbors)
        {
            float distance = Vector3.Distance(transform.position, neighbor.transform.position);
            
            if (distance > 0 && distance < separationRadius)
            {
                Vector3 diff = transform.position - neighbor.transform.position;
                diff.y = 0; // Keep on ground plane
                diff = diff.normalized / distance; // Weight by distance
                steer += diff;
                count++;
            }
        }

        if (count > 0)
        {
            steer /= count;
        }

        return steer;
    }

    /// <summary>
    /// Alignment: steer towards the average heading of neighbors
    /// </summary>
    private Vector3 CalculateAlignment(System.Collections.Generic.List<CrowdAgent> neighbors)
    {
        if (neighbors.Count == 0)
            return Vector3.zero;

        Vector3 averageDirection = Vector3.zero;
        foreach (var neighbor in neighbors)
        {
            if (neighbor.agent != null)
            {
                averageDirection += neighbor.agent.velocity;
            }
        }
        
        if (neighbors.Count > 0)
        {
            averageDirection /= neighbors.Count;
        }

        averageDirection.y = 0; // Keep on ground plane
        return averageDirection.normalized;
    }

    /// <summary>
    /// Share target information with nearby agents
    /// </summary>
    protected void ShareTargetInformation(Vector3 targetPosition)
    {
        if (!shareTargetInfo || CrowdManager.instance == null)
            return;

        sharedTargetPosition = targetPosition;
        lastTargetShareTime = Time.time;

        // Broadcast to nearby agents
        var neighbors = CrowdManager.instance.GetNeighbors(transform.position, targetSharingRadius, this);
        foreach (var neighbor in neighbors)
        {
            neighbor.ReceiveSharedTarget(targetPosition);
        }
    }

    /// <summary>
    /// Receive shared target information from another agent
    /// </summary>
    public void ReceiveSharedTarget(Vector3 targetPosition)
    {
        sharedTargetPosition = targetPosition;
        lastTargetShareTime = Time.time;
    }

    /// <summary>
    /// Apply flocking forces to the agent's movement
    /// </summary>
    protected void ApplyFlockingForces(Vector3 primaryDirection, float flockingInfluence = 0.3f)
    {
        if (agent == null || !agent.isOnNavMesh)
            return;

        Vector3 flockingForce = CalculateFlockingBehavior();
        
        // Blend primary direction with flocking behavior
        Vector3 finalDirection = primaryDirection.normalized * (1f - flockingInfluence) + 
                                flockingForce.normalized * flockingInfluence;
        
        // Apply to movement
        Vector3 targetPosition = transform.position + finalDirection * 2f;
        targetPosition.y = transform.position.y;
        
        // Validate the position is on the NavMesh before setting destination
        NavMeshHit hit;
        if (NavMesh.SamplePosition(targetPosition, out hit, 2f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }

    /// <summary>
    /// Check if agent should coordinate attack with nearby agents
    /// </summary>
    protected bool ShouldAttackNow()
    {
        if (!coordinateAttacks || CrowdManager.instance == null)
            return true;

        var neighbors = CrowdManager.instance.GetNeighbors(transform.position, separationRadius * 2, this);
        
        // Count how many neighbors are currently in combat
        int combatNeighbors = 0;
        foreach (var neighbor in neighbors)
        {
            if (neighbor.isInCombat)
                combatNeighbors++;
        }

        // Attack if at least one neighbor is also in combat, or if alone
        return neighbors.Count == 0 || combatNeighbors > 0;
    }

    /// <summary>
    /// Get formation position for this agent
    /// </summary>
    protected Vector3 GetFormationPosition(Vector3 leaderPosition, int agentIndex)
    {
        if (!useFormations)
            return leaderPosition;

        // Simple formation: arrange agents in a grid behind the leader
        int row = agentIndex / 3;
        int col = agentIndex % 3 - 1; // -1, 0, 1

        Vector3 offset = new Vector3(col * 2f, 0, -row * 2f);
        return leaderPosition + offset;
    }

    protected virtual void OnDrawGizmosSelected()
    {
        // Draw neighborhood radius
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, neighborhoodRadius);

        // Draw separation radius
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, separationRadius);

        // Draw target sharing radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, targetSharingRadius);

        // Draw shared target position
        if (sharedTargetPosition.HasValue)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, sharedTargetPosition.Value);
            Gizmos.DrawWireSphere(sharedTargetPosition.Value, 0.5f);
        }
    }
}
