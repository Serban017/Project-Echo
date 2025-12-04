using UnityEngine;

public class TargetMovment : MonoBehaviour
{

    public bool shouldMove, shouldRotate; 

    public float moveSpeed, rotateSpeed;
    
    
    
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (shouldMove)
        {
            transform.position += new Vector3(moveSpeed, 0f, 0f) * Time.deltaTime;
        }
    }
}
