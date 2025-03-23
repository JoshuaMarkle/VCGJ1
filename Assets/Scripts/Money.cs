using UnityEngine;

public class Money : MonoBehaviour
{
    public int moneyValue = 1;            // Amount added to cash when collected.
    public float collectionDistance = 5f; // Distance at which the money is collected.
    public float despawnDistance = 200f;  // Distance at which the money despawns if the player is too far.
	public AudioClip moneySound;

    private Transform player;

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
    }

    void Update()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= collectionDistance)
        {
			MusicManager.Instance.PlaySFX(moneySound);
            if (GameMaster.Instance != null)
                GameMaster.Instance.cash += moneyValue;
            Destroy(gameObject);
        }
        else if (distance >= despawnDistance)
        {
            Destroy(gameObject);
        }
    }
}
