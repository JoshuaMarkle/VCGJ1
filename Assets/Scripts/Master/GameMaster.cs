using UnityEngine;
using System.Collections.Generic;

public class GameMaster : MonoBehaviour
{
    public static GameMaster instance;

    [Header("Player Stats")]
    public float playerHunger = 100f;
    public int cash = 0;
    public int pizzasInCar = 0;
    public int policeStars = 0;

    [Header("Deliveries")]
    public List<House> allHouses = new List<House>();
    public House currentDelivery;

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);

        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // 🔍 Automatically find all House scripts in the scene
        allHouses = new List<House>(FindObjectsOfType<House>());

        if (allHouses.Count == 0)
            Debug.LogWarning("GameMaster: No House scripts found in the scene!");

        AssignRandomDelivery();
    }

    public void AssignRandomDelivery()
    {
        if (allHouses.Count == 0) return;

        // Choose a random house that is not already the current delivery
        House randomHouse;
        do
        {
            randomHouse = allHouses[Random.Range(0, allHouses.Count)];
        } while (randomHouse == currentDelivery && allHouses.Count > 1);

        if (currentDelivery != null)
            currentDelivery.Deactivate();

        currentDelivery = randomHouse;
        currentDelivery.Activate();
    }
}
