using SturfeeVPS.SDK;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInputController : SceneSingleton<PlayerInputController>
{
    [Header("UI")]
    public FixedJoystick MovementJoystick;
    public FixedJoystick LookJoystick;

    [Header("Refs")]
    // References
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Transform cameraRigBase;
    [SerializeField] private Transform normalPositionHelper;
    [SerializeField] private Transform zoomPositionHelper;
    [SerializeField] private CharacterController characterController;

    [Header("Player Settings")]
    // Player settings
    [SerializeField] private float cameraSensitivity;
    [SerializeField] private float moveSpeed;
    [SerializeField] private float moveInputDeadZone;
    [SerializeField] private float pitchClampMin = -45f;
    [SerializeField] private float pitchClampMax = 45f;

    [SerializeField] private float pitchClampMinMax = -75f;
    [SerializeField] private Vector3 cameraInitPos = new Vector3(0, 3.75f, -3);
    [SerializeField] private Vector3 cameraZoomPos = new Vector3(0, 3.75f, -1);

    private float vSpeed = 0; // current vertical velocity
    private float gravity = 9.8f;

    // Camera control
    //private Vector2 lookInput;
    private float cameraPitch;

    // Player movement
    private Vector2 moveTouchStartPosition;
    //private Vector2 moveInput;

    private void Update()
    {
        //valueText.text = "Current Value: " + variableJoystick.Direction;

#if UNITY_EDITOR

        if (UnityEditor.EditorApplication.isRemoteConnected)
        {
            //// Handles input
            //GetTouchInput();
            ////MyLogger.Log(" Touch");
        }
        else
        {
            GetKeyboardInput();
        }

#else
        //GetTouchInput();
#endif

        LookAround();
        Move();

        //if (rightFingerId != -1)
        //{
        //    // Ony look around if the right finger is being tracked
        //    //MyLogger.Log("Rotating");
        //    LookAround();
        //}

        //if (leftFingerId != -1)
        //{
        //    // Ony move if the left finger is being tracked
        //    //MyLogger.Log("Moving");
        //    Move();
        //}
    }

    public bool IsActive()
    {
        return LookJoystick.IsActive || MovementJoystick.IsActive;
    }

    void GetKeyboardInput()
    {
        // left & right rotation
        Vector3 inputRotation = new Vector3(0, Input.GetAxis("Horizontal"), 0);

        // forward & backward direction
        Vector3 direction = transform.TransformDirection(Vector3.forward);
        float forwardInput = Input.GetAxis("Vertical");
        Vector3 inputPosition = direction * forwardInput;

        //transform.Translate(inputPosition);
        transform.Rotate(inputRotation * 3.5f);
        characterController.SimpleMove(inputPosition * 3.5f);
    }

    void LookAround()
    {

        //var currentPitch = cameraPitch - LookJoystick.Direction.y;
        //if (currentPitch < pitchClampMin)
        //{
        //    cameraTransform.localPosition = zoomPositionHelper.localPosition;
        //}
        //else
        //{
        //    cameraTransform.localPosition = normalPositionHelper.localPosition;
        //}

        // vertical (pitch) rotation

        //cameraPitch = Mathf.Clamp(cameraPitch - lookInput.y, -90f, 90f);
        cameraPitch = Mathf.Clamp(cameraPitch - LookJoystick.Direction.y, pitchClampMin, pitchClampMax);
        cameraRigBase.localRotation = Quaternion.Euler(cameraPitch, 0, 0);

        // horizontal (yaw) rotation
        //transform.Rotate(transform.up, lookInput.x);
        transform.Rotate(transform.up, LookJoystick.Direction.x);
    }

    void Move()
    {
        // Don't move if the touch delta is shorter than the designated dead zone
        //if (moveInput.sqrMagnitude <= moveInputDeadZone) return;
        if (MovementJoystick.Direction.sqrMagnitude <= moveInputDeadZone) return;

        // Multiply the normalized direction by the speed
        Vector2 movementDirection = MovementJoystick.Direction.normalized * moveSpeed * Time.deltaTime;
        // Move relatively to the local transform's direction
        var vel = transform.right * movementDirection.x + transform.forward * movementDirection.y;

        // handle jumping?
        //if (controller.isGrounded)
        //{
        //    vSpeed = 0; // grounded character has vSpeed = 0...
        //    if (Input.GetKeyDown("space"))
        //    { // unless it jumps:
        //        vSpeed = jumpSpeed;
        //    }
        //}

        // apply gravity acceleration to vertical speed:
        vSpeed -= gravity * Time.deltaTime;
        vel.y = vSpeed; // include vertical speed in vel

        characterController.Move(vel);
    }
}
