using UnityEngine;
using System.Collections.Generic;

public class GameMaster : MonoBehaviour
{
    public static GameMaster Instance;

    [Header("Player Stats")]
    public int cash = 10;             // Now tracking cents as well
    public int pizzasInCar = 0;
    public int carCapacity = 3;
    public int policeStars = 0;
    public float maxHunger = 1f;
    public float hunger = 1f;
    public float hungerDrainRate = 1f;    // per minute
    public bool alive = true;

    [Header("Delivery Queue Settings")]
    public int maxOrders = 5;                   // Maximum number of active orders
    public float baseOrderInterval = 10f;         // Base time between new orders (seconds)
    public float timeBetweenOrderVariance = 2f;   // +/- seconds variance
    private float orderTimer = 0f;
    private List<House> activeDeliveries = new List<House>();

    [Header("Difficulty Settings")]
    // During gameplay phase, use gameplayTime to drive police star increases.
    public float difficultyStarThreshold = 30f;   // Every this many seconds of gameplay, add a star
    private float gameplayTime = 0f;             

    [Header("Police System")]
    public GameObject policePrefab;
    public float policeSpawnDistance = 60f;

    [Header("Audio")]
    public AudioClip eatPizzaSound;
    public AudioClip deliveredPizzaSound;
    public AudioClip diedSound;

    [Header("Misc")]
    public float slowMoAmount = 0.2f;

    [Header("References")]
    public Transform player;

    // New flag to indicate we're in the starting (tutorial) phase.
    private bool startingPhase = true;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj) player = playerObj.transform;
        }
        Restart();
    }

    private void Update()
    {
        // Manage time scale (slow-mo if player isn’t alive).
        if (alive) Time.timeScale = 1;
        else Time.timeScale = slowMoAmount;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        // Example input for "eating" a pizza (restores hunger) if desired.
        if (Input.GetButtonDown("Jump") || Input.GetButtonDown("Submit") || Input.GetMouseButtonDown(1))
            EatPizza();

        HandleHunger(Time.deltaTime);
        UI.Instance?.UpdateHUD();

        // In the starting phase, check if the player has at least one pizza
        // but no active order. If so, automatically add a delivery order.
        if (startingPhase)
        {
            if (pizzasInCar > 0 && activeDeliveries.Count == 0)
            {
                StartInitialDelivery();
            }
            // During starting phase, we do not spawn additional orders or scale difficulty.
            return;
        }
        else
        {
            // Gameplay phase: update gameplay time.
            gameplayTime += Time.deltaTime;

            // Increase police pressure based on gameplay time.
            int targetStars = Mathf.FloorToInt(gameplayTime / difficultyStarThreshold);
            if (targetStars > policeStars)
            {
                int diff = targetStars - policeStars;
                policeStars = targetStars;
                for (int i = 0; i < diff; i++)
                {
                    SpawnPoliceUnit();
                }
            }

            // Spawn new orders (if under maxOrders) using gameplay time to adjust intervals.
            if (activeDeliveries.Count < maxOrders)
            {
                orderTimer -= Time.deltaTime;
                if (orderTimer <= 0f)
                {
                    House newOrder = GetRandomHouseNotActive();
                    if (newOrder != null)
                    {
                        activeDeliveries.Add(newOrder);
                        newOrder.Activate();
                    }
                    float adjustedInterval = Mathf.Max(2f, baseOrderInterval - gameplayTime * 0.05f);
                    orderTimer = adjustedInterval + Random.Range(-timeBetweenOrderVariance, timeBetweenOrderVariance);
                }
            }
        }
    }

    private void HandleHunger(float deltaTime)
    {
        if (!alive) return;

        float hungerLoss = hungerDrainRate / 60f * deltaTime;
        hunger -= hungerLoss;

        if (hunger < 0f)
        {
            alive = false;
            MusicManager.Instance.PlaySFX(diedSound);
            UI.Instance?.ShowGameOverScreen("Died of Hunger");
        }
    }

    // Returns a random House that is not currently active in deliveries.
    private House GetRandomHouseNotActive()
    {
        List<House> availableHouses = new List<House>();
        House[] allHouses = FindObjectsOfType<House>(); // Alternatively, cache this list if houses don’t change.
        foreach (House h in allHouses)
        {
            if (!activeDeliveries.Contains(h))
                availableHouses.Add(h);
        }
        if (availableHouses.Count == 0)
            return null;
        return availableHouses[Random.Range(0, availableHouses.Count)];
    }

    // Called by a House when the player completes a delivery.
    // The tip is normally calculated based on how quickly the player reached the house.
    public void OnSuccessfulDelivery(float tip)
    {
        MusicManager.Instance.PlaySFX(deliveredPizzaSound);
        if (startingPhase)
        {
            // In starting phase, override the tip to award exactly $12.
            cash += 12;
            pizzasInCar--;
            // End the starting phase and transition to gameplay.
            startingPhase = false;
            gameplayTime = 0f;
            activeDeliveries.Clear();
        }
        else
        {
            cash += Mathf.CeilToInt(tip);
            pizzasInCar--;
            // Remove any delivered orders.
            for (int i = activeDeliveries.Count - 1; i >= 0; i--)
            {
                if (!activeDeliveries[i].IsActive())
                {
                    activeDeliveries.RemoveAt(i);
                }
            }
        }
    }

    // Called by a House when the delivery times out.
    public void FailDelivery() {
        UI.Instance?.ShowGameOverScreen("Too slow! Failed to make your delivery");
    }

    private void SpawnPoliceUnit()
    {
        if (policePrefab == null || player == null) return;
        Vector3 randomDir = Random.onUnitSphere;
        randomDir.y = 0;
        Vector3 spawnPos = player.position + randomDir.normalized * policeSpawnDistance;
        Instantiate(policePrefab, spawnPos, Quaternion.identity);
    }

    public void EatPizza()
    {
        // If the player "eats" a pizza, restore hunger.
        // (This method may be used differently during pickup vs. consumption.)
        if (pizzasInCar > 0)
        {
            MusicManager.Instance.PlaySFX(eatPizzaSound);
            pizzasInCar--;
            hunger = maxHunger;
        }
    }

    public void CatchPlayer()
    {
        if (!alive) return;

        alive = false;
        MusicManager.Instance.PlaySFX(diedSound);
        UI.Instance?.ShowGameOverScreen("The Police Took Your Pizza!");
    }

    public void DiedToWater()
    {
        if (!alive) return;

        alive = false;
        MusicManager.Instance.PlaySFX(diedSound);
        UI.Instance?.ShowGameOverScreen("You can't drive there!");
    }

    public void Restart()
    {
        cash = 10;
        hunger = maxHunger;
        gameplayTime = 0f;
        orderTimer = baseOrderInterval;
        activeDeliveries.Clear();
        alive = true;
        // Begin in the starting phase.
        startingPhase = true;
    }

    // Called externally (for example, when the player picks up their first pizza)
    // to ensure an order is added during the starting phase.
    public void StartInitialDelivery()
    {
        if (startingPhase && activeDeliveries.Count == 0)
        {
            House startingOrder = GetRandomHouseNotActive();
            if (startingOrder != null)
            {
                activeDeliveries.Add(startingOrder);
                startingOrder.Activate();
            }
        }
    }
}
