using UnityEngine;
using System.Collections.Generic;

public class GameMaster : MonoBehaviour
{
    public static GameMaster Instance;

    public Transform player;

	[Header("Game State")]
	public bool playerAlive = true;

    [Header("Player Stats")]
    public int cash = 0;
    public int pizzasInCar = 0;
    public int policeStars = 0;
	public float maxHunger = 1f;
    public float hunger = 1f;
    public float hungerDrainRate = 1f; // per minute

    [Header("Deliveries")]
    public List<House> allHouses = new List<House>();
    public House currentDelivery;
    private float deliveryStartTime;

    [Header("Police System")]
    public GameObject policePrefab;
    public float policeSpawnDistance = 60f;

    [Header("Audio")]
    public AudioClip eatPizzaSound;
    public AudioClip deliveredPizzaSound;
    public AudioClip diedSound;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj) player = playerObj.transform;
        }

        allHouses = new List<House>(FindObjectsOfType<House>());
        if (allHouses.Count == 0)
            Debug.LogWarning("GameMaster: No House scripts found in the scene!");

		hunger = maxHunger;

        AssignRandomDelivery();
    }

    public void Restart()
    {
		hunger = maxHunger;
        AssignRandomDelivery();
    }

    private void Update()
    {
        HandleHunger(Time.deltaTime);
        UI.Instance?.UpdateHUD(); // Keep UI updated

		if (Input.GetKeyDown(KeyCode.Space)) {
			EatPizza();
		}
    }

    private void HandleHunger(float deltaTime)
    {
        float hungerLoss = hungerDrainRate / 60f * deltaTime;
        hunger -= hungerLoss;

		// Player died of hunger
		if (playerAlive && hunger < 0) {
			playerAlive = false;
			MusicManager.Instance.PlaySFX(diedSound);
			UI.Instance?.ShowGameOverScreen("Died of Hunger lol");
		}
    }

    public void AssignRandomDelivery()
    {
        if (allHouses.Count == 0) return;

        House randomHouse;
        do
        {
            randomHouse = allHouses[Random.Range(0, allHouses.Count)];
        } while (randomHouse == currentDelivery && allHouses.Count > 1);

        if (currentDelivery != null)
            currentDelivery.Deactivate();

        currentDelivery = randomHouse;
        currentDelivery.Activate();
        deliveryStartTime = Time.time;
    }

    public void OnSuccessfulDelivery()
    {
		MusicManager.Instance.PlaySFX(deliveredPizzaSound);

        // 🎉 Increase cash with a tip
        float deliveryTime = Time.time - deliveryStartTime;
        int tip = Mathf.Clamp(Mathf.RoundToInt(50f - deliveryTime * 2f), 5, 50);
        cash += tip;
        pizzasInCar--;

        // 🚨 Add police stars
        policeStars++;
        SpawnPoliceUnit();

        // 🍕 Start next delivery if pizza left
		AssignRandomDelivery();
    }

    private void SpawnPoliceUnit()
    {
        if (policePrefab == null || player == null) return;

        Vector3 randomDir = Random.onUnitSphere;
        randomDir.y = 0;
        Vector3 spawnPos = player.position + randomDir.normalized * policeSpawnDistance;

        GameObject cop = Instantiate(policePrefab, spawnPos, Quaternion.identity);
    }

	private void EatPizza() {
		if (pizzasInCar > 0) {
			MusicManager.Instance.PlaySFX(eatPizzaSound);
			pizzasInCar--;
			hunger = maxHunger;
		}
	}

	public void CatchPlayer()
	{
		if (!playerAlive) return;

		playerAlive = false;
		MusicManager.Instance.PlaySFX(diedSound);
		UI.Instance?.ShowGameOverScreen("The Police Took You Pizza!");
	}
}
