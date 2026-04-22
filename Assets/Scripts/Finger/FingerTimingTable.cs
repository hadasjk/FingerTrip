using UnityEngine;

[System.Serializable]
public class FingerTimingTable
{

    [Tooltip("성공 횟수별 기준 주기(초). 첫 값은 0.5초 권장.")]
    public float[] successIntervals =
    {
        0.5f,
        0.45f,
        0.405f,
        0.3645f,
        0.328125f,
        0.295f,
        0.2655f,
        0.2385f,
        0.2145f,
        0.1935f,
        0.174f,
        0.1565f,
        0.141f,
        0.127f
    };

    public float GetIntervalBySuccessCount(int successCount)
    {

        if (successIntervals == null || successIntervals.Length == 0)
        {

            return 0.5f;

        }

        int index = Mathf.Clamp(successCount, 0, successIntervals.Length - 1);

        return successIntervals[index];

    }

    public float GetBaseInterval()
    {

        return GetIntervalBySuccessCount(0);

    }

}

