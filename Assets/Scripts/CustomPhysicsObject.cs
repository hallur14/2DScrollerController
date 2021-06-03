using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class CustomPhysicsObject : MonoBehaviour
{
    /* Private & protected variables */
    protected Rigidbody2D rb2D;
    protected Collider2D col2D;
    private ContactFilter2D contactFilter2D;

    private RaycastHit2D[] hitBuffer = new RaycastHit2D[16];
    private List<RaycastHit2D> hitBufferList = new List<RaycastHit2D>();

    public List<GameObject> ignoreGameObjectList = new List<GameObject>();

    [SerializeField] protected Vector2 velocity;
    protected Vector2 targetVelocity;
    private Vector2 groundNormal;

    [SerializeField] protected bool isGrounded;

    /* Public variables */
    public bool useGravity = true;
    public float gravityModifier = 5f;
    public float minMoveDistance = 0.001f;
    [Tooltip("Needs to be at least 10% of the objects maximum speed")]
    public float skinWidth = 0.2f;
    public float minGroundNormalY = 0.65f;
    public float maxFallSpeed = -40f;

    protected virtual void UpdatePhysics()
    {

    }

    void Start()
    {
        rb2D = GetComponent<Rigidbody2D>();
        col2D = GetComponent<Collider2D>();

        contactFilter2D.useTriggers = false;
        contactFilter2D.SetLayerMask(Physics2D.GetLayerCollisionMask(gameObject.layer));
    }

    void Update()
    {
        targetVelocity = Vector2.zero;
        UpdatePhysics();
    }

    void FixedUpdate()
    {
        velocity = useGravity ? velocity + gravityModifier * Physics2D.gravity * Time.deltaTime : new Vector2(velocity.x, -0.8f);
        velocity.x = targetVelocity.x;

        /* Limit how fast you can fall */
        if (velocity.y < maxFallSpeed)
            velocity.y = maxFallSpeed;

        Vector2 deltaPosition = velocity * Time.deltaTime;

        Vector2 groundDirection = Vector2.right;
        if(isGrounded)
            groundDirection = new Vector2(groundNormal.y, -groundNormal.x);
        Debug.DrawRay(transform.position, groundDirection, Color.yellow);

        isGrounded = false;

        /* Move on X-axis first */
        Vector2 movement = groundDirection * deltaPosition.x;
        Move(movement, false);

        /* Then move on Y-Axis */
        movement = Vector2.up * deltaPosition.y;
        Move(movement, true);
    }

    void Move(Vector2 movement, bool moveOnY)
    {
        float distance = movement.magnitude;

        if (distance > minMoveDistance)
        {
            int collisionCount = col2D.Cast(movement, contactFilter2D, hitBuffer, distance + skinWidth, true);

            hitBufferList.Clear();

            for (int i = 0; i < collisionCount; i++)
            {
                /*Ignore collisions with objects on the ignore list.*/
                if (!ignoreGameObjectList.Contains(hitBuffer[i].collider.gameObject))
                    hitBufferList.Add(hitBuffer[i]);
            }

            foreach (RaycastHit2D hit in hitBufferList)
            {
                Vector2 currentNormal = hit.normal;

                if (currentNormal.y > minGroundNormalY)
                {
                    isGrounded = true;

                    if (moveOnY)
                    {
                        groundNormal = currentNormal;
                        currentNormal.x = 0;
                    }
                }

                float projection = Vector2.Dot(velocity, currentNormal);

                if (projection < 0)
                {
                    velocity = velocity - projection * currentNormal;
                }

                float modifiedDistance = hit.distance - skinWidth;
                distance = modifiedDistance < distance ? modifiedDistance : distance;
            }
        }

        rb2D.position = rb2D.position + movement.normalized * distance;
    }

    public void AddToIgnoreList(GameObject g)
    {
        if(!ignoreGameObjectList.Contains(g))
            ignoreGameObjectList.Add(g);
    }

    public void RemoveFromIgnoreList(GameObject g)
    {
        if(ignoreGameObjectList.Contains(g))
            ignoreGameObjectList.Remove(g);
    }
}
