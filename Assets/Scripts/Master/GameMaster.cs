using UnityEngine;
using System.Collections.Generic;

public class GameMaster : MonoBehaviour
{
    public static GameMaster Instance;

    [Header("Player Stats")]
    public int cash = 10;             // Now tracking cents as well
    public int pizzasInCar = 0;
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
    public float difficultyTime = 0f;             // Time since round start in seconds
    public float difficultyIncreaseRate = 1f;       // How quickly difficulty rises (units per second)
    public float difficultyStarThreshold = 30f;     // Every this many difficulty units, add police stars
    public int difficultyStarIncrease = 1;
    private float nextStarDifficulty = 30f;         // Next threshold for police star increase

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
		if (alive) Time.timeScale = 1;
		else Time.timeScale = slowMoAmount;
		Time.fixedDeltaTime = 0.02f * Time.timeScale;

		// Input
		// if (InputSystem.Instance.jumping || InputSystem.Instance.attacking)
		// 	EatPizza();
		if (Input.GetButtonDown("Jump") || Input.GetButtonDown("Submit") || Input.GetMouseButtonDown(1))
			EatPizza();

        // Increase difficulty over time.
        difficultyTime += Time.deltaTime * difficultyIncreaseRate;
        HandleHunger(Time.deltaTime);
        UI.Instance?.UpdateHUD();

        // If we haven't reached max active orders, spawn new orders.
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
                float adjustedInterval = Mathf.Max(2f, baseOrderInterval - difficultyTime * 0.05f);
                orderTimer = adjustedInterval + Random.Range(-timeBetweenOrderVariance, timeBetweenOrderVariance);
            }
        }

        // Increase police pressure based on difficulty.
        if (difficultyTime >= nextStarDifficulty)
        {
            policeStars += difficultyStarIncrease;
            SpawnPoliceUnit();
            nextStarDifficulty += difficultyStarThreshold;
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

    // Called by a House when the player completes a delivery successfully.
    // The tip is calculated based on how quickly the player reached that house.
    public void OnSuccessfulDelivery(float tip)
    {
        MusicManager.Instance.PlaySFX(deliveredPizzaSound);
		cash += Mathf.CeilToInt(tip);
        pizzasInCar--;

        // Remove the delivered house from active orders.
        // (Assumes the House deactivates itself on completion.)
        for (int i = activeDeliveries.Count - 1; i >= 0; i--)
        {
            if (!activeDeliveries[i].IsActive())
            {
                activeDeliveries.RemoveAt(i);
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
		difficultyTime = 0f;
		nextStarDifficulty = difficultyStarThreshold;
		activeDeliveries.Clear();
		alive = true;
        orderTimer = baseOrderInterval;
	}
}
