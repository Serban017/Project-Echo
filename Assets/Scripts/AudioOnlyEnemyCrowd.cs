using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Audio-based enemy with crowd simulation - detects player through sound and coordinates with other agents
/// </summary>
public class AudioOnlyEnemyCrowd : CrowdAgent
{
    [Header("Audio Detection")]
    public float hearingDistanceWalk = 8f;
    public float hearingDistanceRun = 20f;
    
    [Header("Chase Behavior")]
    public float distanceToLose = 25f;
    public float distanceToStop = 2f;
    public float moveSpeed = 4.5f;
    public float keepChasingTime = 8f;
    
    [Header("Combat")]
    public GameObject bullet;
    public Transform firePoint;
    public float fireRate = 0.3f;
    public float waitBetweenShots = 0.5f;
    public float timeToShoot = 1f;
    
    [Header("Crowd Combat Settings")]
    public float flockingInfluenceNormal = 0.4f; // How much flocking affects movement when not shooting
    public float flockingInfluenceCombat = 0.2f; // Reduced influence during combat
    public float surroundPlayerRadius = 5f; // Try to surround player at this distance
    
    private bool chasing;
    private Vector3 targetPoint, startPoint;
    private Vector3 lastKnownPosition;
    private bool hasLastKnownPosition = false;
    private float chaseCounter;
    private float fireCount, shootWaitCounter, shootTimeCounter;
    private bool playerIsMoving = false;
    private bool playerIsRunning = false;
    private Vector3 lastPlayerPosition;
    
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

        if (PlayerController.instance != null)
            lastPlayerPosition = PlayerController.instance.transform.position;

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

        DetectPlayerMovement();
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

        lastPlayerPosition = PlayerController.instance.transform.position;
    }

    private void HandleIdleState()
    {
        if (CanHearPlayer())
        {
            chasing = true;
            isInCombat = true;
            hasLastKnownPosition = true;
            lastKnownPosition = PlayerController.instance.transform.position;
            shootTimeCounter = timeToShoot;
            shootWaitCounter = waitBetweenShots;
            
            // Share target with nearby agents
            ShareTargetInformation(lastKnownPosition);
        }
        // Check if we received shared target from another agent
        else if (sharedTargetPosition.HasValue && Time.time - lastTargetShareTime < 5f)
        {
            chasing = true;
            isInCombat = true;
            hasLastKnownPosition = true;
            lastKnownPosition = sharedTargetPosition.Value;
        }

        if (chaseCounter > 0)
        {
            chaseCounter -= Time.deltaTime;
            if (chaseCounter <= 0)
            {
                if (agent.isOnNavMesh)
                    agent.destination = startPoint;
                hasLastKnownPosition = false;
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
        // Update last known position if we can hear player
        if (CanHearPlayer())
        {
            lastKnownPosition = PlayerController.instance.transform.position;
            hasLastKnownPosition = true;
            chaseCounter = keepChasingTime;
            ShareTargetInformation(lastKnownPosition);
        }

        // Move towards last known position with flocking behavior
        if (hasLastKnownPosition)
        {
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
        }

        // Check if should lose player
        if (Vector3.Distance(transform.position, targetPoint) > distanceToLose)
        {
            chasing = false;
            isInCombat = false;
            chaseCounter = keepChasingTime;
            hasLastKnownPosition = false;
            return;
        }

        // If can't hear player, start losing them
        if (!CanHearPlayer())
        {
            chaseCounter -= Time.deltaTime;
            if (chaseCounter <= 0)
            {
                chasing = false;
                isInCombat = false;
                hasLastKnownPosition = false;
                return;
            }
        }

        // Handle shooting with coordination
        HandleShooting();
    }

    private Vector3 CalculateTargetPosition()
    {
        // Try to surround the player with other agents
        if (CrowdManager.instance != null && useFormations)
        {
            var neighbors = CrowdManager.instance.GetNeighbors(transform.position, neighborhoodRadius, this);
            int myIndex = 0;
            
            // Calculate my index among nearby agents
            foreach (var neighbor in neighbors)
            {
                if (Vector3.Distance(neighbor.transform.position, lastKnownPosition) < 
                    Vector3.Distance(transform.position, lastKnownPosition))
                {
                    myIndex++;
                }
            }

            // Calculate surround position
            float angle = (360f / Mathf.Max(neighbors.Count + 1, 4)) * myIndex;
            Vector3 offset = Quaternion.Euler(0, angle, 0) * Vector3.forward * surroundPlayerRadius;
            Vector3 surroundPos = lastKnownPosition + offset;
            surroundPos.y = transform.position.y;

            // Blend between direct approach and surround position
            return Vector3.Lerp(lastKnownPosition, surroundPos, formationStrength);
        }

        return lastKnownPosition;
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

    private void DetectPlayerMovement()
    {
        if (PlayerController.instance == null)
            return;

        Vector3 currentPlayerPosition = PlayerController.instance.transform.position;
        float distanceMoved = Vector3.Distance(currentPlayerPosition, lastPlayerPosition);
        float movementSpeed = distanceMoved / Time.deltaTime;

        playerIsMoving = movementSpeed > 0.1f;
        playerIsRunning = movementSpeed > 10f;
    }

    private bool CanHearPlayer()
    {
        if (PlayerController.instance == null || !playerIsMoving)
            return false;

        float distanceToPlayer = Vector3.Distance(transform.position, PlayerController.instance.transform.position);
        
        if (playerIsRunning)
            return distanceToPlayer <= hearingDistanceRun;
        else
            return distanceToPlayer <= hearingDistanceWalk;
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, hearingDistanceWalk);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, hearingDistanceRun);
        
        if (hasLastKnownPosition && chasing)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, lastKnownPosition);
            Gizmos.DrawWireSphere(lastKnownPosition, 0.5f);
        }
    }
}
