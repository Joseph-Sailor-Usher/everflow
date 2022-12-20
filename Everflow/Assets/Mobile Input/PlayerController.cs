using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private Rigidbody playerRb;
    public CapsuleCollider playerCapCol;

    //MOVE variables
    public float moveSpeed, moveSpeedRamp, airMoveSpeed, maxAirSpeed = 0.5f;
    //DRAG variables
    public float groundDrag = 0.8f, stopDrag = 0.8f, slideDrag = 0.95f, minTurnDragAngle = 20;
    //JUMP variables
    public float jumpForce = 10.0f, jumpCooldownTime = 1.0f, lastJumpTime;
    //DASH variables
    public float dashCooldownTime = 1.0f, lastDashTime, dashBoost = 3.0f, dashBoostHorizontal = 10.0f;
    //SLIDE variables
    public bool canSlide = false;
    public float slideBoost = 3.0f, slideVelocityRequirement = 3.0f;

    //MOVE utility variables
    private Vector3 flatVelocity = new Vector3(), flatDragGroundVelocity = new Vector3(), intendedMoveDirection = new Vector3(), movementInput = new Vector3();


    //STATE variables
    public bool isGrounded = false, isSliding = false, hasDashed = false;

    //INPUT variables
    public bool moveIntent = false, isLooking = false;
    public Vector2 lookVector, moveVector;

    //WALL RUN
    public bool canWallRun = true, wallNearby = false;
    public float wallRunDrag = 0.9f;
    private Vector3 yVelocityMod = new Vector3();

    //METHODS
    private void Start()
    {
        playerRb = GetComponent<Rigidbody>();
        playerCapCol = GetComponent<CapsuleCollider>();
        lastJumpTime = Time.time;
    }

    float difference;
    private void Update()
    {
        //Check if we can slide
        canSlide = Physics.Raycast(transform.position, Vector3.down, 2.4f, playerLayerMask);

        //MOVE
        if (moveIntent && isSliding == false)
        {
            movementInput = Vector3.zero;
            movementInput += moveVector.x * transform.right;
            movementInput += moveVector.y * transform.forward;
            flatVelocity.x = playerRb.velocity.x;
            flatVelocity.y = 0;
            flatVelocity.z = playerRb.velocity.z;
            difference += Mathf.Abs(Vector2.Angle(flatVelocity.normalized, movementInput.normalized));
            difference /= 2.0f;
            movementInput = movementInput.normalized;
            if(isGrounded)
            {
                float groundSpeed = Mathf.Max((moveSpeed - flatVelocity.magnitude), 0);
                playerRb.AddForce(groundSpeed * movementInput * Time.deltaTime, ForceMode.Acceleration);
            }
            else
            {
                float airSpeed = Mathf.Max((maxAirSpeed - flatVelocity.magnitude), 0);
                playerRb.AddForce(airSpeed * movementInput * Time.deltaTime, ForceMode.Acceleration);
            }
        }
    }
    private RaycastHit rayHit;
    public LayerMask playerLayerMask;
    private void FixedUpdate()
    {
        if (isSliding == false)
        {
            if (Physics.Raycast(transform.position + Vector3.up, Vector3.down, out rayHit, 1.4f, playerLayerMask))
            {
                if (rayHit.collider.CompareTag("Player") == false)
                {
                    isGrounded = true;
                }
                else
                {
                    isGrounded = false;
                }
            }
            else
            {
                isGrounded = false;
            }
        }
        if (isGrounded)
        {
            if (Time.time - lastDashTime >= dashCooldownTime)
            {
                hasDashed = false;
            }
        }

        //Drag
        flatDragGroundVelocity = playerRb.velocity;
        if (isSliding)
        {
            flatDragGroundVelocity.x *= slideDrag;
            flatDragGroundVelocity.z *= slideDrag;
        }
        else if (isGrounded && (moveIntent == false))
        {
            flatDragGroundVelocity.x *= stopDrag;
            flatDragGroundVelocity.z *= stopDrag;
        }
        else if (isGrounded)
        {
            flatDragGroundVelocity.x *= groundDrag;
            flatDragGroundVelocity.z *= groundDrag;
        }

        //APPLY DRAG
        playerRb.velocity = flatDragGroundVelocity;

        if (isSliding && playerRb.velocity.magnitude < slideVelocityRequirement)
        {
            StopSliding();
        }

        //WALL RUN
        if (isGrounded == false)
        {
            WallRun();
        }
    }
    public void Jump()
    {
        if (isGrounded && Time.time - lastJumpTime >= jumpCooldownTime)
        {
            if (isSliding)
                StopSliding();
            lastJumpTime = Time.time;
            playerRb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }
    public void Slide()
    {
        if (canSlide && isSliding == false && flatVelocity.magnitude >= slideVelocityRequirement)
        {
            isSliding = true;
            playerCapCol.height = 0.8f;
            playerRb.AddForce(transform.forward * slideBoost, ForceMode.Impulse);
            StartCoroutine("KeepSlidingSoundGoing");
        }
    }
    private IEnumerator KeepSlidingSoundGoing()
    {
        yield return new WaitForSeconds(0.25f);
        if (isSliding)
            StartCoroutine("KeepSlidingSoundGoing");
    }
    public void StopSliding()
    {
        isSliding = false;
        playerCapCol.height = 2;
    }
    public void Dash(bool leftOrRight)
    {
        if (hasDashed == false)
        {
            hasDashed = true;
            lastDashTime = Time.time;
            if (leftOrRight)
            {
                playerRb.AddForce(transform.right * dashBoostHorizontal, ForceMode.Impulse);
                playerRb.AddForce(transform.up * dashBoost, ForceMode.Impulse);
            }
            else
            {
                playerRb.AddForce(-transform.right * dashBoostHorizontal, ForceMode.Impulse);
                playerRb.AddForce(transform.up * dashBoost, ForceMode.Impulse);
            }
        }
    }

    private void WallRun()
    {
        if (playerRb.velocity.magnitude < 2.0f) return;
        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, -transform.right, out hit, 1.2f, playerLayerMask))
        {
            wallNearby = true;

            //ALLOW DASH AWAY
            if (Time.time - lastDashTime >= dashCooldownTime)
            {
                hasDashed = false;
            }
            playerRb.AddForce(-hit.normal);
        }
        else
        {
            wallNearby = false;
            if (Physics.Raycast(transform.position + Vector3.up * 0.5f, transform.right, out hit, 1.2f, playerLayerMask))
            {
                wallNearby = true;

                //ALLOW DASH AWAY
                if (Time.time - lastDashTime >= dashCooldownTime)
                {
                    hasDashed = false;
                }
                playerRb.AddForce(-hit.normal);
            }
            else
            {
                wallNearby = false;
            }
        }

        if (wallNearby)
        {
            yVelocityMod = playerRb.velocity;
            yVelocityMod.y *= wallRunDrag;
            playerRb.velocity = yVelocityMod;
        }
    }

    /*
    private IEnumerator StandUp()
    {
        yield return new WaitForSeconds(0.01f);
        if(playerCapCol.center.y > 1)
        {
            playerCapCol.center -= Vector3.up * 0.02f;
            StartCoroutine("StandUp");
        }
        if(playerCapCol.center.y < 1)
        {
            playerCapCol.center = Vector3.up;
        }
    }
    */
}
