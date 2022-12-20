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
    private float dif;
    private Vector3 flatDragGroundVelocity;

    //Customizable variables
    public float groundSpeed = 7.0f, airSpeed = 7.0f, relSpeed;
    public float jumpForce, lastJumpTime, jumpCooldownTime, groundCheckDistance = 1.4f;
    public float slideDrag, stopDrag, groundDrag;

    //Input system variables
    private InputAction move, jump;
    void Awake()
    {
        playerControls = new();
        if (playerRb == null) playerRb = GetComponent<Rigidbody>();
        lastJumpTime = Time.time;
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
        isGrounded = Physics.Raycast(transform.position + Vector3.up, Vector3.down, out rayDownHit, groundCheckDistance, playerLayerMask);
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
        dif += Mathf.Abs(Vector2.Angle(flatVelocity.normalized, processedMovementInput)) / 2.0f;
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
    public void Jump(InputAction.CallbackContext context)
    {
        //Debug.Log("Jump");
        //RETURN if ungrounded or cooling down
        if (isGrounded == false || Time.time - lastJumpTime > jumpCooldownTime) return;
        lastJumpTime = Time.time;
        playerRb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }
    public void ApplyDrag()
    {
        flatDragGroundVelocity = playerRb.velocity;
        if (isSliding)
        {
            flatDragGroundVelocity.x *= slideDrag;
            flatDragGroundVelocity.z *= slideDrag;
        }
        else if (isGrounded && (intendedMoveVector.magnitude < 0.01f))
        {
            flatDragGroundVelocity.x *= stopDrag;
            flatDragGroundVelocity.z *= stopDrag;
        }
        else if (isGrounded)
        {
            flatDragGroundVelocity.x *= groundDrag;
            flatDragGroundVelocity.z *= groundDrag;
        }
        playerRb.velocity = flatDragGroundVelocity;
    }
}
