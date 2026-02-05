using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Visual-based enemy with crowd simulation - detects player through sight and coordinates with other agents
/// </summary>
public class VisualOnlyEnemyCrowd : CrowdAgent
{
    [Header("Visual Detection")]
    public float visionDistance = 15f;
    public float fieldOfViewAngle = 120f;
    public LayerMask obstacleMask;
    
    [Header("Chase Behavior")]
    public float distanceToLose = 20f;
    public float distanceToStop = 2f;
    public float moveSpeed = 4.5f;
    public float keepChasingTime = 5f;
    
    [Header("Combat")]
    public GameObject bullet;
    public Transform firePoint;
    public float fireRate = 0.3f;
    public float waitBetweenShots = 0.5f;
    public float timeToShoot = 1f;
    
    [Header("Crowd Combat Settings")]
    public float flockingInfluenceNormal = 0.4f;
    public float flockingInfluenceCombat = 0.2f;
    public float surroundPlayerRadius = 6f;
    public bool useCoverSeekingBehavior = true;
    
    private bool chasing;
    private Vector3 targetPoint, startPoint;
    private float chaseCounter;
    private float fireCount, shootWaitCounter, shootTimeCounter;
    
    public Animator anim;

    protected override void Start()
    {
        base.Start();
        
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();
        
        if (anim == null)
            anim = GetComponent<Animator>();

        startPoint = transform.position;
        shootTimeCounter = timeToShoot;
        shootWaitCounter = waitBetweenShots;
        fireCount = fireRate;

        if (agent != null)
        {
            agent.speed = moveSpeed;
            agent.stoppingDistance = 0.5f;
        }
    }

    void Update()
    {
        if (PlayerController.instance == null)
            return;

        targetPoint = PlayerController.instance.transform.position;
        targetPoint.y = transform.position.y;

        if (!chasing)
        {
            HandleIdleState();
        }
        else
        {
            HandleChaseState();
        }
    }

    private void HandleIdleState()
    {
        if (CanSeePlayer())
        {
            chasing = true;
            isInCombat = true;
            shootTimeCounter = timeToShoot;
            shootWaitCounter = waitBetweenShots;
            
            // Share target with nearby agents
            ShareTargetInformation(PlayerController.instance.transform.position);
        }
        // Check if we received shared target from another agent
        else if (sharedTargetPosition.HasValue && Time.time - lastTargetShareTime < 5f)
        {
            chasing = true;
            isInCombat = true;
            
            // Move towards shared target to investigate
            if (agent.isOnNavMesh)
            {
                agent.destination = sharedTargetPosition.Value;
                anim.SetBool("isMoving", true);
            }
        }

        if (chaseCounter > 0)
        {
            chaseCounter -= Time.deltaTime;
            if (chaseCounter <= 0)
            {
                if (agent.isOnNavMesh)
                    agent.destination = startPoint;
                isInCombat = false;
            }
        }

        if (agent.isOnNavMesh && agent.remainingDistance < 0.25f)
            anim.SetBool("isMoving", false);
        else
            anim.SetBool("isMoving", true);
    }

    private void HandleChaseState()
    {
        // Share target information continuously
        if (CanSeePlayer())
        {
            ShareTargetInformation(PlayerController.instance.transform.position);
        }

        // Calculate target position with crowd tactics
        Vector3 targetPos = CalculateTargetPosition();

        if (agent.isOnNavMesh)
        {
            float distanceToTarget = Vector3.Distance(transform.position, targetPos);
            
            if (distanceToTarget > distanceToStop)
            {
                // Apply flocking while moving
                Vector3 directionToTarget = (targetPos - transform.position).normalized;
                ApplyFlockingForces(directionToTarget, flockingInfluenceNormal);
            }
            else
            {
                agent.destination = transform.position;
            }
        }

        // Lose player if can't see them and too far
        if (!CanSeePlayer() || Vector3.Distance(transform.position, targetPoint) > distanceToLose)
        {
            chasing = false;
            isInCombat = false;
            chaseCounter = keepChasingTime;
            return;
        }

        // Handle shooting with coordination
        HandleShooting();
    }

    private Vector3 CalculateTargetPosition()
    {
        Vector3 baseTarget = PlayerController.instance.transform.position;
        
        // Try to surround the player with other agents
        if (CrowdManager.instance != null && useFormations)
        {
            var neighbors = CrowdManager.instance.GetNeighbors(transform.position, neighborhoodRadius, this);
            int myIndex = 0;
            
            // Calculate my index among nearby agents chasing the same target
            foreach (var neighbor in neighbors)
            {
                if (neighbor.isInCombat && 
                    Vector3.Distance(neighbor.transform.position, baseTarget) < 
                    Vector3.Distance(transform.position, baseTarget))
                {
                    myIndex++;
                }
            }

            // Calculate surround position
            int totalAgents = Mathf.Max(neighbors.FindAll(n => n.isInCombat).Count + 1, 4);
            float angle = (360f / totalAgents) * myIndex;
            Vector3 offset = Quaternion.Euler(0, angle, 0) * Vector3.forward * surroundPlayerRadius;
            Vector3 surroundPos = baseTarget + offset;
            surroundPos.y = transform.position.y;

            // Blend between direct approach and surround position
            return Vector3.Lerp(baseTarget, surroundPos, formationStrength);
        }

        return baseTarget;
    }

    private void HandleShooting()
    {
        if (shootWaitCounter > 0)
        {
            // Waiting before shooting
            shootWaitCounter -= Time.deltaTime;
            
            if (shootWaitCounter <= 0)
            {
                shootTimeCounter = timeToShoot;
            }
            
            anim.SetBool("isMoving", true);
        }
        else
        {
            if (PlayerController.instance.gameObject.activeInHierarchy)
            {
                // Shooting burst time
                shootTimeCounter -= Time.deltaTime;
                
                if (shootTimeCounter > 0)
                {
                    fireCount -= Time.deltaTime;
                    
                    if (fireCount <= 0)
                    {
                        fireCount = fireRate; // reset timer
                        
                        firePoint.LookAt(PlayerController.instance.transform.position);
                        
                        // Check the angle of the player
                        Vector3 targetDir = PlayerController.instance.transform.position - transform.position;
                        float angle = Vector3.SignedAngle(targetDir, transform.forward, Vector3.up);
                        
                        if (Mathf.Abs(angle) < 30f)
                        {
                            Instantiate(bullet, firePoint.position, firePoint.rotation);
                            anim.SetTrigger("fireShot");
                        }
                        else
                        {
                            shootWaitCounter = waitBetweenShots;
                            shootTimeCounter = timeToShoot;
                            fireCount = fireRate;
                        }
                    }
                    
                    if (agent.isOnNavMesh)
                        agent.destination = transform.position;
                }
                else
                {
                    // Reset after shot burst finishes
                    shootWaitCounter = waitBetweenShots;
                    shootTimeCounter = timeToShoot;
                    fireCount = fireRate; // Reset fire interval to avoid leftover values
                }
            }
            
            anim.SetBool("isMoving", false);
        }
    }

    private bool CanSeePlayer()
    {
        if (PlayerController.instance == null)
            return false;

        Vector3 directionToPlayer = PlayerController.instance.transform.position - transform.position;
        float distanceToPlayer = directionToPlayer.magnitude;

        if (distanceToPlayer > visionDistance)
            return false;

        float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);
        
        if (angleToPlayer > fieldOfViewAngle / 2f)
            return false;

        // Check line of sight
        RaycastHit hit;
        Vector3 rayStart = transform.position + Vector3.up;
        Vector3 rayDirection = directionToPlayer.normalized;
        
        if (Physics.Raycast(rayStart, rayDirection, out hit, distanceToPlayer))
        {
            if (hit.transform == PlayerController.instance.transform || 
                hit.transform.IsChildOf(PlayerController.instance.transform))
            {
                return true;
            }
            return false;
        }

        return true;
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, visionDistance);

        // Draw field of view cone
        Vector3 leftBoundary = Quaternion.Euler(0, -fieldOfViewAngle / 2f, 0) * transform.forward * visionDistance;
        Vector3 rightBoundary = Quaternion.Euler(0, fieldOfViewAngle / 2f, 0) * transform.forward * visionDistance;

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + leftBoundary);
        Gizmos.DrawLine(transform.position, transform.position + rightBoundary);
    }
}
