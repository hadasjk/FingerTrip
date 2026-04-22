using UnityEngine;

public class FingerRhythmSystem : MonoBehaviour
{

    public enum RhythmResult
    {

        None,
        Success,
        Fail

    }

    [Header("Timing Table")]
    public FingerTimingTable timingTable = new FingerTimingTable();

    [Header("Judgement")]
    [Range(0.01f, 0.49f)]
    public float successWindowRatio = 0.2f;

    [Header("Judgement Bias")]
    [Tooltip("조금 이른 입력을 추가로 허용하는 값(초)")]
    public float earlyPressBonus = 0.02f;

    [Header("Debug")]
    public bool rhythmStarted = false;
    public bool firstStartConsumed = false;

    public int comboCount = 0;
    public int totalSuccessCount = 0;

    public FingerStepSide currentBeatSide = FingerStepSide.Left;
    public FingerStepSide expectedSide = FingerStepSide.Left;

    public float currentBeatTime = 0f;
    public float currentBeatInterval = 0.5f;
    public float nextBeatTime = 0f;
    public float nextIntervalAfterCurrentBeat = 0.5f;
    public float currentWindow = 0.1f;

    public bool currentBeatSuccessful = false;
    public bool currentBeatLocked = false;

    public float timeSinceCurrentBeat = 0f;
    public float normalizedProgress = 0f;

    public RhythmResult lastResult = RhythmResult.None;

    public System.Action<FingerStepSide, float> OnBeatStep;
    public System.Action<FingerStepSide, int, float> OnStepSuccess;
    public System.Action OnStepFail;
    public System.Action OnComboReset;

    public void Initialize()
    {

        rhythmStarted = false;
        firstStartConsumed = false;

        comboCount = 0;
        totalSuccessCount = 0;

        currentBeatSide = FingerStepSide.Left;
        expectedSide = FingerStepSide.Left;

        currentBeatTime = 0f;
        currentBeatInterval = timingTable != null ? timingTable.GetBaseInterval() : 0.5f;
        nextBeatTime = 0f;
        nextIntervalAfterCurrentBeat = currentBeatInterval;
        currentWindow = GetWindow(currentBeatInterval);

        currentBeatSuccessful = false;
        currentBeatLocked = false;

        timeSinceCurrentBeat = 0f;
        normalizedProgress = 0f;

        lastResult = RhythmResult.None;

    }

    private void Update()
    {

        UpdateDebugValues();

        if (!rhythmStarted)
        {

            return;

        }

        AdvanceBeatIfNeeded();

    }

    public void ProcessClick(FingerStepSide clickedSide)
    {

        if (!firstStartConsumed)
        {

            StartFromIdle(clickedSide);
            return;

        }

        if (!rhythmStarted)
        {

            return;

        }

        if (currentBeatLocked)
        {

            return;

        }

        float now = Time.time;
        float offset = now - currentBeatTime;
        float window = GetWindow(currentBeatInterval);

        float earlyLimit = -(window + earlyPressBonus);
        float lateLimit = window;

        if (clickedSide != expectedSide)
        {

            RegisterFailure();
            return;

        }

        if (offset < earlyLimit || offset > lateLimit)
        {

            RegisterFailure();
            return;

        }

        RegisterSuccess(clickedSide);

    }

    private void StartFromIdle(FingerStepSide firstSide)
    {

        firstStartConsumed = true;
        rhythmStarted = true;

        comboCount = 1;
        totalSuccessCount = 1;

        currentBeatSide = firstSide;
        expectedSide = firstSide;

        currentBeatInterval = timingTable.GetIntervalBySuccessCount(0);
        nextIntervalAfterCurrentBeat = currentBeatInterval;

        currentBeatTime = Time.time;
        nextBeatTime = currentBeatTime + currentBeatInterval;
        currentWindow = GetWindow(currentBeatInterval);

        currentBeatSuccessful = true;
        currentBeatLocked = true;

        lastResult = RhythmResult.Success;

        OnBeatStep?.Invoke(currentBeatSide, currentBeatInterval);
        OnStepSuccess?.Invoke(currentBeatSide, comboCount, currentBeatInterval);

    }

    private void RegisterSuccess(FingerStepSide clickedSide)
    {

        currentBeatSuccessful = true;
        currentBeatLocked = true;

        comboCount++;
        totalSuccessCount++;

        nextIntervalAfterCurrentBeat = timingTable.GetIntervalBySuccessCount(comboCount - 1);

        lastResult = RhythmResult.Success;

        OnStepSuccess?.Invoke(clickedSide, comboCount, nextIntervalAfterCurrentBeat);

    }

    private void RegisterFailure()
    {

        currentBeatSuccessful = false;
        currentBeatLocked = true;

        ResetComboOnly();

        lastResult = RhythmResult.Fail;

        OnStepFail?.Invoke();

    }

    private void RegisterMissTimeout()
    {

        currentBeatSuccessful = false;
        currentBeatLocked = true;

        ResetComboOnly();

        lastResult = RhythmResult.Fail;

        OnStepFail?.Invoke();

    }

    private void ResetComboOnly()
    {

        comboCount = 0;
        nextIntervalAfterCurrentBeat = timingTable.GetBaseInterval();
        OnComboReset?.Invoke();

    }

    private void AdvanceBeatIfNeeded()
    {

        while (Time.time >= nextBeatTime)
        {

            if (!currentBeatSuccessful)
            {

                RegisterMissTimeout();

            }

            FingerStepSide nextSide = GetOpposite(currentBeatSide);

            currentBeatSide = nextSide;
            expectedSide = nextSide;

            currentBeatTime = nextBeatTime;
            currentBeatInterval = nextIntervalAfterCurrentBeat;
            currentWindow = GetWindow(currentBeatInterval);

            nextBeatTime = currentBeatTime + currentBeatInterval;

            currentBeatSuccessful = false;
            currentBeatLocked = false;

            OnBeatStep?.Invoke(currentBeatSide, currentBeatInterval);

        }

    }

    private void UpdateDebugValues()
    {

        if (!rhythmStarted)
        {

            timeSinceCurrentBeat = 0f;
            normalizedProgress = 0f;
            return;

        }

        timeSinceCurrentBeat = Time.time - currentBeatTime;

        if (currentBeatInterval <= 0.0001f)
        {

            normalizedProgress = 0f;
        }
        else
        {

            normalizedProgress = Mathf.Clamp01(timeSinceCurrentBeat / currentBeatInterval);

        }

        currentWindow = GetWindow(currentBeatInterval);

    }

    public float GetWindow(float interval)
    {

        return Mathf.Max(0.001f, interval * successWindowRatio);

    }

    public FingerStepSide GetOpposite(FingerStepSide side)
    {

        return side == FingerStepSide.Left ? FingerStepSide.Right : FingerStepSide.Left;

    }

    public void ResetComboAndTimingAfterJump()
    {

        comboCount = 0;
        nextIntervalAfterCurrentBeat = timingTable.GetBaseInterval();

        if (rhythmStarted)
        {

            currentBeatSuccessful = false;
            currentBeatLocked = true;

        }

        lastResult = RhythmResult.Fail;

        OnComboReset?.Invoke();

    }

}

