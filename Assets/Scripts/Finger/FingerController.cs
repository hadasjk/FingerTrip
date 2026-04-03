using UnityEngine;

public class FingerController : MonoBehaviour
{

    public CharacterController controller;
    public Transform cameraRoot;

    [Header("Separated Systems")]
    public FingerRhythmSystem rhythmSystem;
    public FingerHandAnimator handAnimatorController;

    [Header("Camera")]
    public float mouseSensitivity = 3f;
    public float cameraMinPitch = -20f;
    public float cameraMaxPitch = 45f;

    private float cameraPitch = 0f;

    [Header("Continuous Move")]
    [Tooltip("기본 전진 속도. 콤보 배율이 여기에 곱해진다.")]
    public float baseMoveSpeed = 1.0f;
    public float[] moveSpeedMultipliers;

    [Header("Jump")]
    public float baseJumpPower = 3f;
    public float extraJumpForwardFactor = 0.3f;
    public float[] jumpPowerMultipliers;

    [Header("Physics")]
    public float gravity = -9.81f;

    [Header("Debug")]
    public bool allowContinuousMove = false;
    public float continuousMoveSpeed = 3f;

    private Vector3 velocity;
    private bool isJumping = false;

    private bool isMovingForward = false;
    private float currentMoveSpeed = 0f;

    private void Reset()
    {

        controller = GetComponent<CharacterController>();
        cameraRoot = GetComponentInChildren<Camera>()?.transform;

        if (rhythmSystem == null)
        {

            rhythmSystem = GetComponent<FingerRhythmSystem>();

        }

        if (handAnimatorController == null)
        {

            handAnimatorController = GetComponent<FingerHandAnimator>();

        }

    }

    private void Awake()
    {

        if (controller == null)
        {

            controller = GetComponent<CharacterController>();

        }

        if (rhythmSystem == null)
        {

            rhythmSystem = GetComponent<FingerRhythmSystem>();

        }

        if (handAnimatorController == null)
        {

            handAnimatorController = GetComponent<FingerHandAnimator>();

        }

    }

    private void Start()
    {

        if (rhythmSystem != null)
        {

            rhythmSystem.Initialize();
            rhythmSystem.OnBeatStep += HandleBeatStepAnimation;
            rhythmSystem.OnStepSuccess += HandleStepSuccess;
            rhythmSystem.OnStepFail += HandleStepFail;
            rhythmSystem.OnComboReset += HandleComboReset;

        }

        if (handAnimatorController != null)
        {

            handAnimatorController.PlayInitialIdle();

        }

    }

    private void OnDestroy()
    {

        if (rhythmSystem != null)
        {

            rhythmSystem.OnBeatStep -= HandleBeatStepAnimation;
            rhythmSystem.OnStepSuccess -= HandleStepSuccess;
            rhythmSystem.OnStepFail -= HandleStepFail;
            rhythmSystem.OnComboReset -= HandleComboReset;

        }

    }

    private void Update()
    {

        HandleMouseLook();
        HandleGravity();
        HandleInput();
        HandleForwardMove();
        ApplyVelocity();

    }

    private void HandleInput()
    {

        if (Input.GetMouseButtonDown(0))
        {

            OnMouseClick(FingerStepSide.Left);

        }

        if (Input.GetMouseButtonDown(1))
        {

            OnMouseClick(FingerStepSide.Right);

        }

        if (Input.GetKeyDown(KeyCode.Space))
        {

            OnJumpKey();

        }

        if (allowContinuousMove)
        {

            HandleContinuousMoveInput();

        }

    }

    private void OnMouseClick(FingerStepSide side)
    {

        if (!controller.isGrounded)
        {

            return;

        }

        if (rhythmSystem == null)
        {

            return;

        }

        rhythmSystem.ProcessClick(side);

    }

    private void HandleBeatStepAnimation(FingerStepSide side, float beatInterval)
    {

        if (handAnimatorController != null)
        {

            handAnimatorController.PlayStep(side, beatInterval);

        }

    }

    private void HandleStepSuccess(FingerStepSide side, int comboCount, float nextInterval)
    {

        StartContinuousMove(comboCount);

        Debug.Log(
            $"[FingerStep] Success | Side:{side} | Combo:{comboCount} | NextInterval:{nextInterval:F4} | MoveSpeed:{currentMoveSpeed:F3}"
        );

    }

    private void HandleStepFail()
    {

        StopForwardMove();

        Debug.Log("[FingerStep] Fail | Stop Move | Combo Reset | Rhythm Continues");

    }

    private void HandleComboReset()
    {

        StopForwardMove();

    }

    private void StartContinuousMove(int comboCount)
    {

        float moveMultiplier = 1f;

        if (moveSpeedMultipliers != null && moveSpeedMultipliers.Length > 0)
        {

            int index = Mathf.Clamp(comboCount - 1, 0, moveSpeedMultipliers.Length - 1);
            moveMultiplier = moveSpeedMultipliers[index];

        }

        currentMoveSpeed = baseMoveSpeed * moveMultiplier;
        isMovingForward = true;

    }

    private void StopForwardMove()
    {

        isMovingForward = false;
        currentMoveSpeed = 0f;

    }

    private void HandleForwardMove()
    {

        if (!isMovingForward)
        {

            return;

        }

        if (!controller.isGrounded)
        {

            return;

        }

        Vector3 forward = transform.forward;
        forward.y = 0f;
        forward.Normalize();

        controller.Move(forward * currentMoveSpeed * Time.deltaTime);

    }

    private void OnJumpKey()
    {

        if (!controller.isGrounded)
        {

            return;

        }

        if (isJumping)
        {

            return;

        }

        StartJump();

    }

    private void StartJump()
    {

        isJumping = true;

        int comboCount = 0;

        if (rhythmSystem != null)
        {

            comboCount = rhythmSystem.comboCount;

        }

        float jumpMultiplier = 1f;

        if (jumpPowerMultipliers != null && jumpPowerMultipliers.Length > 0)
        {

            int index = Mathf.Clamp(comboCount - 1, 0, jumpPowerMultipliers.Length - 1);
            jumpMultiplier = jumpPowerMultipliers[index];

        }

        float jumpPower = baseJumpPower * jumpMultiplier;

        velocity.y = jumpPower;

        Vector3 forward = transform.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 jumpForwardOffset = forward * extraJumpForwardFactor;
        controller.Move(jumpForwardOffset);

        StopForwardMove();

        if (rhythmSystem != null)
        {

            rhythmSystem.ResetComboAndTimingAfterJump();

        }

        Debug.Log("[FingerStep] Jump | Combo Reset | Interval Reset To Base");

    }

    private void HandleMouseLook()
    {

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;

        transform.Rotate(Vector3.up, mouseX);

    }

    private void HandleGravity()
    {

        if (controller.isGrounded && velocity.y < 0f)
        {

            velocity = new Vector3(0f, -2f, 0f);
            isJumping = false;

        }

        velocity.y += gravity * Time.deltaTime;

    }

    private void ApplyVelocity()
    {

        controller.Move(velocity * Time.deltaTime);

    }

    private void HandleContinuousMoveInput()
    {

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 move = new Vector3(h, 0f, v);

        if (move.sqrMagnitude > 0.01f)
        {

            move = Quaternion.Euler(0f, transform.eulerAngles.y, 0f) * move;
            move.Normalize();

            controller.Move(move * continuousMoveSpeed * Time.deltaTime);

        }

    }

}