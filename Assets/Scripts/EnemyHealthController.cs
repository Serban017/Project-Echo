using UnityEngine;

public class EnemyHealthController : MonoBehaviour
{

    public int currentHealth = 5; 
    


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void DamageEnemy(int damageAmount)
    {
        currentHealth -= damageAmount; 

        if(currentHealth < 0)
        {
            Destroy(gameObject);
        }
    }


}
