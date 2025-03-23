using UnityEngine;

public class PizzaShop : MonoBehaviour
{
    [Header("Interaction")]
    public float buyRange = 5f;
    public float buyTime = 2f;
    public int pizzaPrice = 10;

    [Header("Area Sprite")]
    public GameObject areaPrefab;
    public float pulseSpeed = 2f;
    public float pulseAmount = 0.1f;
	public float spinSpeed = 5f;
	public float areaScale = 1f;

	private Transform areaTransform;

	[Header("Audio")]
	public AudioClip boughtPizzaSound;

    private float playerStayTimer = 0f;
    private Transform player;
    private Vector3 baseScale;

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (player == null)
            Debug.LogWarning("PizzaShop: No GameObject tagged 'Player' found.");

        if (areaPrefab != null)
        {
            areaTransform = Instantiate(areaPrefab, transform.position, Quaternion.identity, transform).transform;
            baseScale = Vector3.one * buyRange * areaScale;
            areaTransform.localScale = baseScale;
        }
    }

    private void Update()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= buyRange)
        {
            playerStayTimer += Time.deltaTime;

            if (playerStayTimer >= buyTime)
            {
                TryBuyPizza();
                playerStayTimer = 0f;
            }
        }
        else
        {
            playerStayTimer = 0f;
        }

        UpdateArea();
    }

    private void UpdateArea()
    {
        if (areaTransform == null) return;

        float scaleOffset = Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
        areaTransform.localScale = baseScale + Vector3.one * scaleOffset;

		areaTransform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.Self);
    }

    private void TryBuyPizza()
    {
        if (GameMaster.Instance.cash >= pizzaPrice)
        {
            GameMaster.Instance.cash -= pizzaPrice;
            GameMaster.Instance.pizzasInCar++;
			MusicManager.Instance.PlaySFX(boughtPizzaSound);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, buyRange);
    }
}
