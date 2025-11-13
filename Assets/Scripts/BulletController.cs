using UnityEngine;

public class BulletController : MonoBehaviour
{

    public float moveSpeed, lifeTime;

    public Rigidbody theRB; 
    
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        theRB.linearVelocity = transform.forward * moveSpeed;

        lifeTime -= Time.deltaTime;


        if (lifeTime <= 0)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Destroy(gameObject);
    }

}
