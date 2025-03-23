using UnityEngine;

public class MoneySpawner : MonoBehaviour
{
    [Header("Money Spawn Settings")]
    public GameObject moneyPrefab;       // Assign your money prefab here.
    public float spawnInterval = 5f;       // Time in seconds between spawns.
    public float spawnRadius = 100f;       // Money spawns within this radius around the player.

    private Transform player;
    private float timer;

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;

        timer = spawnInterval;
    }

    void Update()
    {
        if (player == null) return;

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            SpawnMoney();
            timer = spawnInterval;
        }
    }

    void SpawnMoney()
    {
        // Generate a random point within a circle (on the XZ plane).
        Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
        Vector3 spawnPosition = player.position + new Vector3(randomCircle.x, 0f, randomCircle.y);
        
        // Optionally, adjust spawnPosition.y if you want the money to spawn at a specific height.
        Instantiate(moneyPrefab, spawnPosition, Quaternion.identity);
    }
}
