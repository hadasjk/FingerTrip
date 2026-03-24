using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class FingerController : MonoBehaviour
{

    public enum RhythmJudgement
    {

        None,
        Perfect,
        Good,
        Bad,
        Fail

    }

    public UnityEngine.CharacterController controller;
    public Transform cameraRoot;

    public Animator HandAnimator;

    // 카메라 회전 관련
    public float mouseSensitivity = 3f;
    public float cameraMinPitch = -20f;
    public float cameraMaxPitch = 45f;

    private float cameraPitch = 0f;

    // 클릭 이동 관련
    public float baseStepDistance = 0.5f;
    public float stepDuration = 0.2f;
    public AnimationCurve stepSpeedCurve;

    public float[] moveSpeedMultipliers;
    public float[] jumpPowerMultipliers;

    private bool isStepping = false;
    private int successCount = 0;

    // 번갈아 클릭 관련
    // -1 : 아직 아무 버튼도 누르지 않음
    //  0 : 마지막에 왼쪽 버튼
    //  1 : 마지막에 오른쪽 버튼
    private int lastClickButton = -1;

    // 점프 관련
    public float baseJumpPower = 3f;
    public float extraJumpForwardFactor = 0.3f;
    private bool isJumping = false;

    // 물리 관련
    public float gravity = -9.81f;
    private Vector3 velocity;

    // 리듬 판정 관련
    [Header("Rhythm Settings")]
    public bool useRhythmSystem = true;     // 기존 이동 시스템때문에 만든 bool 변수인데, 나중에 쓸 일 있을수도 있으니 그냥 두겠음

    public float beatInterval = 0.5f;                 // 0.5초면 120 BPM 느낌
    public float rhythmStartDelay = 0.5f;             // 시작 후 첫 박자까지 대기

    [Header("Judgement Windows")]
    public float perfectWindow = 0.06f;
    public float goodWindow = 0.12f;
    public float badWindow = 0.20f;

    [Header("Step Bonus By Judgement")]
    public float perfectStepMultiplier = 1.0f;
    public float goodStepMultiplier = 0.85f;
    public float badStepMultiplier = 0.65f;

    [Header("Debug Rhythm")]
    public bool rhythmStarted = false;
    public int currentBeatIndex = 0;                  // 현재 진행 중인 박자 인덱스
    public int expectedButton = 0;                    // 0 = Left, 1 = Right
    public float currentBeatTime = 0f;                // 현재 기준 박자 시간
    public float timeToNextBeat = 0f;                 // 다음 박자까지 남은 시간
    public float lastInputOffset = 999f;              // 마지막 입력이 박자와 얼마나 차이났는지
    public RhythmJudgement lastJudgement = RhythmJudgement.None;

    private float rhythmStartTime = 0f;
    private int lastJudgedBeatIndex = -999;           // 이미 판정한 박자를 중복 판정하지 않기 위한 값

    public float RhythmStartTime => rhythmStartTime;
    public int LastJudgedBeatIndex => lastJudgedBeatIndex;
    public bool IsRhythmReady => useRhythmSystem && rhythmStarted;

    // 디버그 옵션
    public bool allowContinuousMove = false;
    public float continuousMoveSpeed = 3f;

    private void Reset()
    {

        controller = GetComponent<UnityEngine.CharacterController>();
        cameraRoot = GetComponentInChildren<Camera>()?.transform;

    }

    private void Awake()
    {

        if (controller == null)
        {

            controller = GetComponent<UnityEngine.CharacterController>();

        }

    }

    private void Start()
    {

        StartRhythm();

    }

    private void Update()
    {

        HandleMouseLook();

        HandleGravity();

        UpdateRhythmDebug();

        HandleInput();

        ApplyVelocity();

        UpdateHandAnimation();

    }

    private void StartRhythm()
    {

        rhythmStartTime = Time.time + rhythmStartDelay;
        rhythmStarted = true;

        currentBeatIndex = 0;
        expectedButton = 0;
        currentBeatTime = rhythmStartTime;
        timeToNextBeat = Mathf.Max(0f, rhythmStartTime - Time.time);

        lastJudgedBeatIndex = -999;
        lastJudgement = RhythmJudgement.None;
        lastInputOffset = 999f;

    }

    private void UpdateRhythmDebug()
    {

        if (!useRhythmSystem || !rhythmStarted)
        {

            return;

        }

        float now = Time.time;

        if (now < rhythmStartTime)
        {

            currentBeatIndex = 0;
            expectedButton = 0;
            currentBeatTime = rhythmStartTime;
            timeToNextBeat = rhythmStartTime - now;

            return;

        }

        float elapsed = now - rhythmStartTime;
        int nearestBeat = Mathf.Max(0, Mathf.RoundToInt(elapsed / beatInterval));

        currentBeatIndex = nearestBeat;
        currentBeatTime = rhythmStartTime + currentBeatIndex * beatInterval;
        expectedButton = currentBeatIndex % 2 == 0 ? 0 : 1;

        float nextBeatTime = currentBeatTime + beatInterval;
        timeToNextBeat = Mathf.Max(0f, nextBeatTime - now);

    }

    // 입력 처리
    private void HandleInput()
    {

        if (Input.GetMouseButtonDown(0))
        {

            OnMouseClick(0);

        }

        if (Input.GetMouseButtonDown(1))
        {

            OnMouseClick(1);

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

    // 카메라 상하/좌우 회전
    private void HandleMouseLook()
    {

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(Vector3.up, mouseX);

        cameraPitch -= mouseY;
        cameraPitch = Mathf.Clamp(cameraPitch, cameraMinPitch, cameraMaxPitch);

        if (cameraRoot != null)
        {

            cameraRoot.localEulerAngles = new Vector3(cameraPitch, 0f, 0f);

        }

    }

    // 점프 관련
    private void HandleGravity()
    {

        if (controller.isGrounded && velocity.y < 0f)
        {

            velocity = new Vector3(0f, -2f, 0f);

            isJumping = false;

        }

        velocity.y += gravity * Time.deltaTime;

    }

    // 디버그용 WASD 이동 로직
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

    private void ApplyVelocity()
    {

        controller.Move(velocity * Time.deltaTime);

    }

    // 애니메이션 상태 갱신
    private void UpdateHandAnimation()
    {

        if (HandAnimator == null)
        {

            return;

        }

        HandAnimator.SetBool("IsStepping", isStepping);
        HandAnimator.SetBool("IsJumping", isJumping);
        HandAnimator.SetBool("IsGrounded", controller != null && controller.isGrounded);

        float speedParam = 0f;

        if (isStepping && moveSpeedMultipliers != null && moveSpeedMultipliers.Length > 0)
        {

            int idx = Mathf.Clamp(successCount - 1, 0, moveSpeedMultipliers.Length - 1);

            speedParam = moveSpeedMultipliers[idx];

        }

        HandAnimator.SetFloat("StepSpeed", speedParam);

        // 필요하면 Animator 쪽에 판정 전달 가능
        // HandAnimator.SetInteger("RhythmJudgement", (int)lastJudgement);

    }

    // 리듬 판정 포함 클릭 처리
    private void OnMouseClick(int button)
    {

        if (!controller.isGrounded)
        {

            return;

        }

        if (isStepping)
        {

            return;

        }

        if (!useRhythmSystem)
        {

            HandleLegacyAlternateClick(button);
            return;

        }

        RhythmJudgement judgement = EvaluateRhythm(button);

        lastJudgement = judgement;
        lastClickButton = button;

        string buttonName = button == 0 ? "Left" : "Right";

        Debug.Log(
            $"[Rhythm] Button:{buttonName} | Judgement:{judgement} | " +
            $"Offset:{lastInputOffset:F3}s | Beat:{currentBeatIndex} | Expected:{(expectedButton == 0 ? "Left" : "Right")}"
        );

        switch (judgement)
        {

            case RhythmJudgement.Perfect:
                StartStep(judgement);
                break;

            case RhythmJudgement.Good:
                StartStep(judgement);
                break;

            case RhythmJudgement.Bad:
                StartStep(judgement);
                break;

            case RhythmJudgement.Fail:
                ResetCombo();
                break;

        }

    }

    // 기존 방식 (수정 전)
    private void HandleLegacyAlternateClick(int button)
    {

        if (lastClickButton == -1 || lastClickButton != button)
        {

            lastClickButton = button;
            lastJudgement = RhythmJudgement.Perfect;
            StartStep(RhythmJudgement.Perfect);

        }
        else
        {

            lastJudgement = RhythmJudgement.Fail;
            ResetCombo();

        }

    }

    private RhythmJudgement EvaluateRhythm(int button)
    {

        if (!rhythmStarted)
        {

            return RhythmJudgement.Fail;

        }

        float now = Time.time;

        if (now < rhythmStartTime)
        {

            lastInputOffset = 999f;
            return RhythmJudgement.Fail;

        }

        float elapsed = now - rhythmStartTime;
        int nearestBeatIndex = Mathf.Max(0, Mathf.RoundToInt(elapsed / beatInterval));
        float nearestBeatTime = rhythmStartTime + nearestBeatIndex * beatInterval;

        float offset = now - nearestBeatTime;
        float absOffset = Mathf.Abs(offset);

        lastInputOffset = offset;

        int beatExpectedButton = nearestBeatIndex % 2 == 0 ? 0 : 1;

        // 이미 판정한 같은 박자를 또 누르면 실패
        if (nearestBeatIndex == lastJudgedBeatIndex)
        {

            return RhythmJudgement.Fail;

        }

        // 이번 박자에 요구되는 버튼이 아니면 실패
        if (button != beatExpectedButton)
        {

            return RhythmJudgement.Fail;

        }

        RhythmJudgement result = RhythmJudgement.Fail;

        if (absOffset <= perfectWindow)
        {

            result = RhythmJudgement.Perfect;

        }
        else if (absOffset <= goodWindow)
        {

            result = RhythmJudgement.Good;

        }
        else if (absOffset <= badWindow)
        {

            result = RhythmJudgement.Bad;

        }
        else
        {

            result = RhythmJudgement.Fail;

        }

        if (result != RhythmJudgement.Fail)
        {

            lastJudgedBeatIndex = nearestBeatIndex;
            currentBeatIndex = nearestBeatIndex;
            currentBeatTime = nearestBeatTime;
            expectedButton = beatExpectedButton;

        }

        return result;

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

    // 한 걸음 이동
    private void StartStep(RhythmJudgement judgement)
    {

        isStepping = true;

        successCount++;

        float judgementMultiplier = GetJudgementStepMultiplier(judgement);

        float moveMultiplier = 1f;

        if (moveSpeedMultipliers != null && moveSpeedMultipliers.Length > 0)
        {

            int idx = Mathf.Clamp(successCount - 1, 0, moveSpeedMultipliers.Length - 1);
            moveMultiplier = moveSpeedMultipliers[idx];

        }

        float stepDistance = baseStepDistance * moveMultiplier * judgementMultiplier;

        Vector3 forward = transform.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos + forward * stepDistance;

        StartCoroutine(StepRoutine(startPos, targetPos));

        Debug.Log("Rhythm Result : " + judgement + " | Combo : " + successCount + " | StepDistance : " + stepDistance.ToString("F2"));

    }

    private float GetJudgementStepMultiplier(RhythmJudgement judgement)
    {

        switch (judgement)
        {

            case RhythmJudgement.Perfect:
                return perfectStepMultiplier;

            case RhythmJudgement.Good:
                return goodStepMultiplier;

            case RhythmJudgement.Bad:
                return badStepMultiplier;

            default:
                return 0f;

        }

    }

    // 한 걸음을 부드럽게 이동하는 코루틴
    private IEnumerator StepRoutine(Vector3 startPos, Vector3 targetPos)
    {

        float t = 0f;

        while (t < stepDuration)
        {

            float normalized = t / stepDuration;

            float curve = (stepSpeedCurve != null && stepSpeedCurve.keys.Length > 0)
                ? stepSpeedCurve.Evaluate(normalized)
                : normalized;

            Vector3 newPos = Vector3.Lerp(startPos, targetPos, curve);
            Vector3 delta = newPos - transform.position;

            controller.Move(delta);

            t += Time.deltaTime;
            yield return null;

        }

        controller.Move(targetPos - transform.position);

        isStepping = false;

    }

    private void StartJump()
    {

        isJumping = true;

        float jumpMultiplier = 1f;

        if (jumpPowerMultipliers != null && jumpPowerMultipliers.Length > 0)
        {

            int idx = Mathf.Clamp(successCount - 1, 0, jumpPowerMultipliers.Length - 1);
            jumpMultiplier = jumpPowerMultipliers[idx];

        }

        float jumpPower = baseJumpPower * jumpMultiplier;

        velocity.y = jumpPower;

        Vector3 forward = transform.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 jumpForwardOffset = forward * extraJumpForwardFactor;
        controller.Move(jumpForwardOffset);

    }

    public void ResetCombo()
    {

        successCount = 0;
        lastClickButton = -1;

        Debug.Log("Rhythm Result : Fail | Combo Reset");

    }

}