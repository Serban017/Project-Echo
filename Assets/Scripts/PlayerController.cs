using UnityEngine;

public class PlayerController : MonoBehaviour
{

    public static PlayerController instance;

    public float moveSpeed, gravityModifier, jumpForce, runSpeed = 15f;
    public float sneakSpeed = 2f;
    public CharacterController characterController;

    private Vector3 moveInput;
    public bool isSneaking { get; private set; }

    public Transform cameraTrans;

    public float mouseSensitivity;
    public bool invertX; 
    public bool invertY;

    private bool canJump, canDoubleJump;
    public Transform groundCheckPoint;
    public LayerMask whatIsGround;
  
    public Animator anim;

    public GameObject bullet;

    public Transform firePoint;

    private void Awake()
    {
        instance = this;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {


        Vector3 flatMove = new Vector3(moveInput.x, 0f, moveInput.z);
        float speed = flatMove.magnitude;
        anim.SetFloat("moveSpeed", speed);
        

      
        float yStore = moveInput.y;

        Vector3 verticalMovement = transform.forward * Input.GetAxisRaw("Vertical");
        Vector3 horizontalMovment = transform.right * Input.GetAxisRaw("Horizontal");

        moveInput = horizontalMovment + verticalMovement;
        moveInput.Normalize();
        
        if (Input.GetKey(KeyCode.LeftControl))
        {
            isSneaking = true;
            moveInput = moveInput * sneakSpeed;
        }
        else if (Input.GetKey(KeyCode.LeftShift))
        {
            isSneaking = false;
            moveInput = moveInput * runSpeed;
        }
        else
        {
            isSneaking = false;
            moveInput = moveInput * moveSpeed;
        }

        moveInput.y = yStore;

        if (characterController.isGrounded)
        {
            moveInput.y = -1f; 

        }
        else
        {
            
            moveInput.y += Physics.gravity.y * gravityModifier * Time.deltaTime;
        }

        canJump = Physics.OverlapSphere(groundCheckPoint.position, .25f, whatIsGround).Length > 0;

        //Jumping 

        if (Input.GetKeyDown(KeyCode.Space) && canJump)
        {
            moveInput.y = jumpForce;
            canDoubleJump = true;
        }else if(canDoubleJump && Input.GetKeyDown(KeyCode.Space))
        {
            moveInput.y = jumpForce;
            canDoubleJump = false;
        }


        characterController.Move(moveInput * Time.deltaTime);


        //Camera rotation 

        Vector2 mouseInput = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * mouseSensitivity;

        if (invertX)
        {
            mouseInput.x = -mouseInput.x;
        }

        if (invertY) { 
        
           mouseInput.y = -mouseInput.y;
        }

        transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y + mouseInput.x, transform.rotation.eulerAngles.z);

        cameraTrans.rotation = Quaternion.Euler(cameraTrans.rotation.eulerAngles + new Vector3(-mouseInput.y, 0f, 0f));

        //Handle Shooting 

        if (Input.GetMouseButtonDown(0)) { 


            RaycastHit hit;

            if (Physics.Raycast(cameraTrans.position, cameraTrans.forward, out hit, 50f))
            {
                if(Vector3.Distance(cameraTrans.position, hit.point) > 2)
                {
                    firePoint.LookAt(hit.point);
                }
                
            }
            else { 
            
                firePoint.LookAt(cameraTrans.position + (cameraTrans.forward * 30f));

            }
            
            Instantiate(bullet, firePoint.position, firePoint.rotation);

        }

        anim.SetFloat("moveSpeed", flatMove.magnitude);
        anim.SetBool("onGround", canJump);
    }
}
