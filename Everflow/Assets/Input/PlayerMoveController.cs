using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using Unity.VisualScripting;

public class PlayerMoveController : MonoBehaviour
{
    //References
    public Rigidbody playerRb;
    private PlayerInput playerControls;

    //State variables
    public bool isGrounded, isSliding;
    public Vector2 intendedMoveVector;

    //Private state variables
    private Vector3 processedMovementInput, flatVelocity;
    public float distanceToGround, groundNormalSlope;
    private Vector3 flatDragGroundVelocity;
    private Vector3 groundNormal;
    //Customizable variables
    public float groundSpeed = 7.0f, airSpeed = 7.0f, relSpeed;
    public float maximumGroundSlope = 60.0f;
    public float jumpForce, lastJumpTime, jumpCooldownTime, maxDistanceFromGroundToJump = 1.5f;
    public float slideDrag, stopDrag, groundDrag;

    //Input system variables
    private InputAction move, jump;
    void Awake()
    {
        playerControls = new();
        if (playerRb == null) playerRb = GetComponent<Rigidbody>();
        move = playerControls.FindAction("Move");
        jump = playerControls.FindAction("Jump");
        jump.performed += Jump;
        move.performed += MoveEventListener;
    }
    private void OnEnable()
    {
        playerControls.Enable();
    }
    private void OnDisable()
    {
        playerControls.Disable();
        jump.performed -= Jump;
        move.performed -= MoveEventListener;
    }

    private RaycastHit rayDownHit;
    public LayerMask playerLayerMask;
    private void FixedUpdate()
    {
        //If there is something below us, we can jump again
        isGrounded = Physics.Raycast(transform.position + Vector3.up, Vector3.down, out rayDownHit, 1000.0f, playerLayerMask);
        groundNormal = rayDownHit.normal;
        distanceToGround = rayDownHit.distance;
        groundNormalSlope = Vector3.Angle(groundNormal, Vector3.up);
        if(distanceToGround > maxDistanceFromGroundToJump || groundNormalSlope > maximumGroundSlope)
        {
            isGrounded = false;
        }
        ApplyDrag();
    }
    private void Update()
    {
        //Read the input into our vector2
        intendedMoveVector = playerControls.Player.Move.ReadValue<Vector2>();
        if(isSliding == false)
            Move();
    }
    public void MoveEventListener(InputAction.CallbackContext context)
    {
        Move();
        //Debug.Log("Moving");
    }
    public void Move()
    {
        processedMovementInput = Vector3.zero;
        processedMovementInput += intendedMoveVector.x * transform.right;
        processedMovementInput += intendedMoveVector.y * transform.forward;
        flatVelocity.x = playerRb.velocity.x;
        flatVelocity.y = 0;
        flatVelocity.z = playerRb.velocity.z;
        processedMovementInput = processedMovementInput.normalized;
        if (isGrounded)
        {
            relSpeed = Mathf.Max(groundSpeed - flatVelocity.magnitude, 0);
            playerRb.AddForce(relSpeed * processedMovementInput * Time.deltaTime * 100, ForceMode.Acceleration);
        }
        else
        {
            relSpeed = Mathf.Max(airSpeed - flatVelocity.magnitude, 0);
            playerRb.AddForce(relSpeed * processedMovementInput * Time.deltaTime * 100, ForceMode.Acceleration);
        }
    }
    //Do nothing within the jumpCooldown period and grounded
    //Apply force in the direction of the ground normal
    public void Jump(InputAction.CallbackContext context)
    {
        //Debug.Log("Jump");
        //if cooling down
        if (distanceToGround > maxDistanceFromGroundToJump || (Time.time - lastJumpTime) < jumpCooldownTime) return;
        //else jump
        lastJumpTime = Time.time;

        //If angle from up to groundNormal is too steep
        if (groundNormalSlope > maximumGroundSlope)
        {
            //apply groundNormal force
            playerRb.AddForce(groundNormal * jumpForce, ForceMode.Impulse);
            return;
        }
        //Apply upward force
        playerRb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        if(Vector3.Dot(transform.forward, groundNormal) >= 0)
        {
            playerRb.AddForce(Vector3.up * jumpForce / 2, ForceMode.Impulse);
        }
    }
    public void ApplyDrag()
    {
        flatDragGroundVelocity = playerRb.velocity;
        if (isGrounded)
        {
            if (isSliding)
            {
                Debug.Log("Slide drag");
                flatDragGroundVelocity.x *= slideDrag;
                flatDragGroundVelocity.z *= slideDrag;
            }
            else if ((intendedMoveVector.magnitude < 0.01f))
            {
                Debug.Log("stop drag");
                flatDragGroundVelocity.x *= stopDrag;
                flatDragGroundVelocity.z *= stopDrag;
            }
            else
            {
                Debug.Log("walk drag");
                flatDragGroundVelocity.x *= groundDrag;
                flatDragGroundVelocity.z *= groundDrag;
            }
            playerRb.velocity = flatDragGroundVelocity;
        }
    }
}
