using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class PlayerController2D : CustomPhysicsObject
{
    /* Private variables */
    protected InputMaster inputMaster;
    protected Vector2 inputVelocity = Vector2.zero;

    private bool jumpPressed;
    private bool jumpReleased = true;

    private float jumpHoldTimer = 0f;

    private float dashTimer = 4f;
    private int jumpCounter = 0;

    /* Public variables */
    public float jumpForce = 10f;
    public bool multiJump;
    [Range(2, 4)] public int extraJumps = 2;
    public float speed = 8f;
    public float acceleration = 20f;
    public float deacceleration = 20f;
    public float airAcceleration = 10f;
    public float airDeacceleration = 10f;
    public float hangTime = 0.1f; // How long you can hold the jump.
    public bool canDash = true;
    public float dashSpeed = 20f;
    public float dashCooldown = 1f;
    public float glideTime = 0.25f;

    ContactFilter2D movementCheckFilter;
    List<RaycastHit2D> hits = new List<RaycastHit2D>();


    private void Awake()
    {
        inputMaster = new InputMaster();
    }

    private void OnEnable()
    {
        inputMaster.Enable();
        
        inputMaster.PlayerInput.Jump.performed += _ => jumpPressed = true;
        inputMaster.PlayerInput.Jump.canceled += _ => jumpPressed = false;
        inputMaster.PlayerInput.Jump.canceled += _ => jumpReleased = true;
        inputMaster.PlayerInput.Dash.performed += _ => Dash();

        movementCheckFilter.useTriggers = false;
        movementCheckFilter.SetLayerMask((1 << 9));
    }

    private void OnDisable()
    {
        inputMaster.Disable();
    }

    protected override void UpdatePhysics()
    {
        Jump();
        Move();
        //Dash();

        Debug.DrawRay(transform.position, new Vector2(inputVelocity.x, 0));

        /* Don't apply velocity if walking into something unless it's being ignored */
        Physics2D.BoxCast(transform.position, new Vector2(GetComponent<BoxCollider2D>().size.x, GetComponent<BoxCollider2D>().size.y), 0, new Vector2(inputVelocity.x, 0), movementCheckFilter, hits, skinWidth);

        /* Ignore objects that have pass through */
        foreach (RaycastHit2D hit in hits)
        {
            if (!ignoreGameObjectList.Contains(hit.collider.gameObject))
                inputVelocity.x = 0;
        }
        
        targetVelocity.x = inputVelocity.x;

        dashTimer += Time.deltaTime;
    }

    void Dash()
    {
        if (canDash)
        {
            float inputX = inputMaster.PlayerInput.Move.ReadValue<float>(); // Get player input.

            if (dashTimer > dashCooldown && inputVelocity.x != 0)
            {
                if (inputX > 0)
                    inputVelocity.x = dashSpeed;
                else if (inputX < 0)
                    inputVelocity.x = -dashSpeed;

                dashTimer = 0;
                if(!isGrounded)
                    StartCoroutine(Glide());
            }
        }
    }

    IEnumerator Glide()
    {
        useGravity = false;
        yield return new WaitForSeconds(glideTime);
        useGravity = true;
    }

    void Move()
    {
        float inputX = inputMaster.PlayerInput.Move.ReadValue<float>(); // Get player input.

        /* Set friction based on input and position */
        float currentFriction = isGrounded ? (inputX != 0 ? acceleration : deacceleration) : (inputX != 0 ? airAcceleration : airDeacceleration);


        inputVelocity.x = Mathf.MoveTowards(inputVelocity.x, inputX * speed, currentFriction * Time.deltaTime);
    }

    void Jump()
    {
        if (jumpPressed)
        {
            /* Jump */
            if (jumpReleased && isGrounded)
            {
                jumpCounter = 1;
                velocity.y = jumpForce;
                jumpReleased = false;
                jumpHoldTimer = 0f;
            }
            /* Hang */
            else if (!jumpReleased && !isGrounded)
            {
                jumpHoldTimer += Time.deltaTime;
                if (jumpHoldTimer < hangTime)
                    velocity.y += jumpForce * Mathf.PI * Time.deltaTime;
            }
            /* Multi jump */
            else if (jumpReleased && !isGrounded && multiJump && jumpCounter < extraJumps)
            {
                jumpCounter++;
                velocity.y = jumpForce;
                jumpReleased = false;
                jumpHoldTimer = 0f;    
            }
        }
    }
}
