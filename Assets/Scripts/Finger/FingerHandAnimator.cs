using UnityEngine;

public class FingerHandAnimator : MonoBehaviour
{

    [Header("References")]
    public Animator handAnimator;

    [Header("State Names")]
    public string idleStateName = "Hand_Idle";
    public string leftStateName = "Hand_Walk_Left";
    public string rightStateName = "Hand_Walk_Right";

    [Header("Animation Timing")]
    [Tooltip("원본 클립 길이. 현재는 Left / Right 모두 0.5초.")]
    public float sourceClipLength = 0.5f;

    [Tooltip("애니메이션 속도가 너무 빨라지는 걸 막는 상한.")]
    public float maxAnimatorSpeed = 3.0f;

    [Tooltip("애니메이션 속도가 너무 느려지는 걸 막는 하한.")]
    public float minAnimatorSpeed = 0.5f;

    private bool idlePlayedAtStart = false;

    private void Start()
    {

        PlayInitialIdle();

    }

    public void PlayInitialIdle()
    {

        if (handAnimator == null)
        {

            return;

        }

        if (idlePlayedAtStart)
        {

            return;

        }

        handAnimator.speed = 1f;
        handAnimator.Play(idleStateName, 0, 0f);
        idlePlayedAtStart = true;

    }

    public void PlayStep(FingerStepSide side, float targetInterval)
    {

        if (handAnimator == null)
        {

            return;

        }

        float speed = 1f;

        if (targetInterval > 0.0001f)
        {

            speed = sourceClipLength / targetInterval;

        }

        speed = Mathf.Clamp(speed, minAnimatorSpeed, maxAnimatorSpeed);
        handAnimator.speed = speed;

        if (side == FingerStepSide.Left)
        {

            handAnimator.Play(leftStateName, 0, 0f);

        }
        else
        {

            handAnimator.Play(rightStateName, 0, 0f);

        }

    }

}