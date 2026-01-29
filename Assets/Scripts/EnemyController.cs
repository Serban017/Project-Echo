using UnityEngine;
using UnityEngine.AI;

public class EnemyController : MonoBehaviour
{

    private bool chasing;
    public float distanceToChase = 10f, distanceToLose = 15f, distanceToStop = 2f;
 

    private Vector3 targetPoint, startPoint;

    public NavMeshAgent agent;

    public float keepChasingTime = 5f;
    private float chaseCounter;

    public GameObject bullet;
    public Transform firePoint;

    public float fireRate, waitBetweenShots = 0.5f, timeToShoot = 1f;
    private float fireCount, shootWaitCounter, shootTimeCounter; 

    public Animator anim;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        startPoint = transform.position;

        shootTimeCounter = timeToShoot;

        shootWaitCounter = waitBetweenShots;

        fireCount = fireRate;
    }


    // Update is called once per frame
    void Update()
    {

        targetPoint = PlayerController.instance.transform.position;

        targetPoint.y = transform.position.y;

        if (!chasing)
        {
            if (Vector3.Distance(transform.position, targetPoint) < distanceToChase)
            {
                chasing = true;

                shootTimeCounter = timeToShoot;
                shootWaitCounter = waitBetweenShots;
            }


            if (chaseCounter > 0)
            {
                chaseCounter -= Time.deltaTime;

                if (chaseCounter <= 0)
                {

                    agent.destination = startPoint;

                }
            }

            if (agent.remainingDistance < 0.25f)
            {
                anim.SetBool("isMoving", false);
            }
            else
            {
                anim.SetBool("isMoving", true);
            }

        }
        else
        {

            //transform.LookAt(targetPoint);

            //theRB.linearVelocity = transform.forward * moveSpeed;

            if (Vector3.Distance(transform.position, targetPoint) > distanceToStop)
            {

                agent.destination = targetPoint;
            }
            else
            {
                agent.destination = transform.position;
            }

            if (Vector3.Distance(transform.position, targetPoint) > distanceToLose)
            {

                chasing = false;

                chaseCounter = keepChasingTime;

            }

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

                            //check the angle of the player
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
    }
    
}
