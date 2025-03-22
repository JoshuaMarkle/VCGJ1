using UnityEngine;

public class GameMaster : MonoBehaviour
{
    public static GameMaster instance;

    [Header("Player Stats")]
    public float playerHunger = 100f;
    public int cash = 0;
    public int pizzasInCar = 0;
    public int policeStars = 0;

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);

        DontDestroyOnLoad(gameObject);
    }
}
