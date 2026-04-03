using UnityEngine;

public class GameManager : MonoBehaviour
{

    public GameObject clearPanel;

    [Header("게임 관리")]
    public int maxScore = 2;
    public int currentScore = 0;
    public float timeLimit = 600f;     // 아직 사용 X

    private void Update()
    {

        CheckGameClear();

    }

    private void CheckGameClear()
    {

        if (currentScore >= maxScore)
        {

            clearPanel.SetActive(true);

        }

    }

}