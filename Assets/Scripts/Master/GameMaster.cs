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
	public Transform spawnPos;

    [Header("Delivery Queue Settings")]
    public int maxOrders = 5;                   // Maximum number of active orders
    public float baseOrderInterval = 10f;         // Base time between new orders (seconds)
    public float timeBetweenOrderVariance = 2f;   // +/- seconds variance
    private float orderTimer = 0f;
    private List<House> activeDeliveries = new List<House>();

	[Header("Delivery Time Scaling")]
	[Tooltip("Minimum time for any delivery regardless of distance")]
	public float minDeliveryTime = 30f;
	[Tooltip("How many seconds per meter of distance")]
	public float deliveryTimePerMeter = 0.2f;

    [Header("Difficulty Settings")]
    // During gameplay phase, use gameplayTime to drive police star increases.
    public float difficultyStarThreshold = 30f;   // Every this many seconds of gameplay, add a star
    private float gameplayTime = 0f;             

	[Header("Police System")]
	public GameObject policePrefab;
	public float policeSpawnDistance = 60f;
	public int policePerStar = 2; // How many spawn per star
	public int maxPolice = 10;    // Global cap
	private int currentPoliceCount = 0;

    [Header("Audio")]
    public AudioClip eatPizzaSound;
    public AudioClip deliveredPizzaSound;
    public AudioClip diedSound;

    [Header("Misc")]
    public float slowMoAmount = 0.2f;

    [Header("References")]
    public Transform player;

    // New fields for arrow pointer guidance in the starting phase.
    [Header("Arrow Pointer")]
    public GameObject arrowPointerPrefab;  // Assign your arrow pointer prefab here.
    public Transform pizzaShopTarget;      // Reference to the transform above the pizza shop.
    private ArrowPointer arrowPointerInstance;

    // Flag for the starting (tutorial) phase.
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
        // Manage time scale (normal if alive, slow-mo if dead)
        if (alive) Time.timeScale = 1;
        else Time.timeScale = slowMoAmount;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        // Allow "eating" a pizza to restore hunger.
        if (Input.GetButtonDown("Jump") || Input.GetButtonDown("Submit") || Input.GetMouseButtonDown(1))
            EatPizza();

		if (!startingPhase)
			HandleHunger(Time.deltaTime);
        UI.Instance?.UpdateHUD();

        // -------------------------------
        // Starting Phase Logic
        // -------------------------------
        if (startingPhase)
        {
            // Ensure an arrow pointer exists.
            if (arrowPointerInstance == null && arrowPointerPrefab != null)
            {
                GameObject arrowObj = Instantiate(arrowPointerPrefab);
                arrowPointerInstance = arrowObj.GetComponent<ArrowPointer>();
                // Initially point the arrow to the pizza shop.
                if (pizzaShopTarget != null)
                {
                    arrowPointerInstance.pizzaTarget = pizzaShopTarget;
                }

            }

            // Once the player has picked up a pizza and an order is queued,
            // update the arrow pointer to point to the first active order.
            if (pizzasInCar > 0 && activeDeliveries.Count == 0)
            {
    			StartInitialDelivery();

				if (arrowPointerInstance)
					arrowPointerInstance.pizzaTarget = activeDeliveries[0].transform;
            }
            // Do not process gameplay-phase logic while in starting phase.
            return;
        }

        // -------------------------------
        // Gameplay Phase Logic
        // -------------------------------
        gameplayTime += Time.deltaTime;

		// Increase police pressure based on gameplay time.
		int targetStars = Mathf.FloorToInt(gameplayTime / difficultyStarThreshold);
		if (targetStars > policeStars)
		{
			int diff = targetStars - policeStars;
			policeStars = targetStars;

			int totalToSpawn = diff * policePerStar;
			for (int i = 0; i < totalToSpawn; i++)
			{
				if (currentPoliceCount >= maxPolice)
					break;

				SpawnPoliceUnit();
				currentPoliceCount++;
			}
		}

        // Spawn new orders if under the maximum.
        if (activeDeliveries.Count < maxOrders)
        {
            orderTimer -= Time.deltaTime;
            if (orderTimer <= 0f)
            {
                House newOrder = GetRandomHouseNotActive();
				if (newOrder != null)
				{
					float distanceToPlayer = Vector3.Distance(player.position, newOrder.transform.position);
					float scaledTime = Mathf.Max(minDeliveryTime, distanceToPlayer * deliveryTimePerMeter);
					newOrder.maxDeliveryDuration = scaledTime;

					activeDeliveries.Add(newOrder);
					newOrder.Activate();
				}
                float adjustedInterval = Mathf.Max(2f, baseOrderInterval - gameplayTime * 0.05f);
                orderTimer = adjustedInterval + Random.Range(-timeBetweenOrderVariance, timeBetweenOrderVariance);
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

    // Returns a random House that is not currently active.
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
    // The tip is normally calculated based on delivery speed.
    public void OnSuccessfulDelivery(float tip)
    {
        MusicManager.Instance.PlaySFX(deliveredPizzaSound);
        if (startingPhase)
        {
            // In starting phase, override the tip and award exactly $12.
            cash += 12;
            pizzasInCar--;
            // End the starting phase and transition to gameplay.
            startingPhase = false;
            gameplayTime = 0f;
            activeDeliveries.Clear();
            // Remove the arrow pointer.
            if (arrowPointerInstance != null)
            {
                Destroy(arrowPointerInstance.gameObject);
                arrowPointerInstance = null;
            }
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

    public void SpawnPoliceUnit()
    {
        if (policePrefab == null || player == null) return;
        Vector3 randomDir = Random.onUnitSphere;
        randomDir.y = 0;
        Vector3 spawnPos = player.position + randomDir.normalized * policeSpawnDistance;
        Instantiate(policePrefab, spawnPos, Quaternion.identity);
    }

    public void EatPizza()
    {
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
		// player.position = spawnPos.position;
        cash = 10;
        hunger = maxHunger;
        gameplayTime = 0f;
        orderTimer = baseOrderInterval;
        activeDeliveries.Clear();
        alive = true;
        startingPhase = true;

		Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        // Create the arrow pointer for the starting phase.
        if (startingPhase && arrowPointerPrefab != null)
        {
            if (arrowPointerInstance != null)
            {
                Destroy(arrowPointerInstance.gameObject);
            }
            GameObject arrowObj = Instantiate(arrowPointerPrefab);
            arrowPointerInstance = arrowObj.GetComponent<ArrowPointer>();
            if (pizzaShopTarget != null)
            {
                arrowPointerInstance.pizzaTarget = pizzaShopTarget;
            }
        }
    }

    // Called externally (for example, when the player picks up their first pizza)
    // to force a delivery order into the queue during the starting phase.
	public void StartInitialDelivery()
	{
		if (startingPhase && activeDeliveries.Count == 0)
		{
			House startingOrder = GetRandomHouseNotActive();
			if (startingOrder != null)
			{
				startingOrder.timed = false;
				activeDeliveries.Add(startingOrder);
				startingOrder.Activate();
			}
		}
	}
}
