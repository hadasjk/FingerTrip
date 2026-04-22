using UnityEngine;

public class CollectableItem : MonoBehaviour
{

    private GameManager gameManager;

    private void Start()
    {

        gameManager = FindFirstObjectByType<GameManager>();
    
    }

    private void OnTriggerEnter(Collider other)
    {

        if (other.CompareTag("Player"))
        {

            ScoreUpdate(1);

            Destroy(gameObject);

        }

    }

    private void ScoreUpdate(int point)
    {

        gameManager.currentScore += point;

    }

}

