using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.EnhancedTouch;

public class PlayerMoveController : MonoBehaviour
{
    //References
    public Rigidbody playerRb;
    private PlayerInput playerControls;
    public Transform neck;

    //State variables
    public bool isGrounded, isSliding;
    public Vector2 intendedMoveVector, intendedLookVector;
    public float lookSensitivity = 100.0f;
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

    // Create a touchscreen device.
    private UnityEngine.InputSystem.EnhancedTouch.Finger lookFinger;
    private int touchesCount = 0;
    private bool looking = false;
    private PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
    //Input system variables
    private InputAction move, jump;
    void Awake()
    {
        touchesCount = 0;
        EnhancedTouchSupport.Enable();
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
        intendedMoveVector.x *= intendedMoveVector.x * intendedMoveVector.x;
        intendedMoveVector.y *= intendedMoveVector.y * intendedMoveVector.y;
        if (isSliding == false)
            Move();

        //Check if we're beginning to look around if we aren't looking and there's a new touch
        if(looking == false && UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches.Count > touchesCount)
        {
            UpdateLookTouch();
            touchesCount = UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches.Count;
        }
        //End the look touch if it ends
        if(lookFinger != null && lookFinger.isActive == false)
        {
            looking = false;
            transformRotCache = transform.rotation;
            neckCache = neck.rotation;
            touchesCount = UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches.Count;
        }
        if (looking)
        {
            Look();
        }
    }
    public void MoveEventListener(InputAction.CallbackContext context)
    {
        Move();
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
    private Quaternion neckCache, transformRotCache;
    private List<RaycastResult> raycastResults = new List<RaycastResult>();
    public void UpdateLookTouch()
    {
        //If we aren't already looking
        if (looking == false)
        {
            //Check if this is a look touch
            foreach (var touch in UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches)
            {
                //If this is a new  touch
                if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
                {
                    //Check if it hit any UI elements
                    eventDataCurrentPosition.position = touch.screenPosition;
                    EventSystem.current.RaycastAll(eventDataCurrentPosition, raycastResults);
                    if (raycastResults.Count == 0)
                    {
                        lookFinger = touch.finger;
                        looking = true;
                    }
                }
            }
            raycastResults.Clear();
        }
    }
    public void Look()
    {
        Debug.Log(lookFinger.currentTouch.screenPosition.x + " " + lookFinger.currentTouch.screenPosition.y + " from "
            + lookFinger.currentTouch.startScreenPosition.x + " " + lookFinger.currentTouch.startScreenPosition.y);
        intendedLookVector = lookFinger.currentTouch.screenPosition - lookFinger.currentTouch.startScreenPosition;
        intendedLookVector.y = Mathf.Clamp(intendedLookVector.y, -80.0f, 80.0f);
        transform.rotation = Quaternion.Euler(0, transformRotCache.eulerAngles.y + intendedLookVector.x, 0);
        neck.localRotation = Quaternion.Euler(neckCache.eulerAngles.x - intendedLookVector.y, 0, 0);
    }
    //Do nothing within the jumpCooldown period and grounded
    //Apply force in the direction of the ground normal
    public void Jump(InputAction.CallbackContext context)
    {
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
                flatDragGroundVelocity.x *= slideDrag;
                flatDragGroundVelocity.z *= slideDrag;
            }
            else if ((intendedMoveVector.magnitude < 0.01f))
            {
                flatDragGroundVelocity.x *= stopDrag;
                flatDragGroundVelocity.z *= stopDrag;
            }
            else
            {
                flatDragGroundVelocity.x *= groundDrag;
                flatDragGroundVelocity.z *= groundDrag;
            }
            playerRb.velocity = flatDragGroundVelocity;
        }
    }
}
