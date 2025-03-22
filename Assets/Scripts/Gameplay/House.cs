using UnityEngine;

public class House : MonoBehaviour
{
    [Header("Delivery Settings")]
    public float deliveryRange = 5f;
    public float deliveryTime = 2f;

    [Header("Area Visual")]
    public GameObject areaSpritePrefab;
    public float pulseSpeed = 2f;
    public float pulseAmount = 0.1f;
	public float spinSpeed = 10f;

    private Transform player;
    private float deliveryTimer = 0f;
    private bool isActive = false;

    private Transform areaInstance;
    private Vector3 baseScale;

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    private void Update()
    {
        if (!isActive || player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= deliveryRange)
        {
            deliveryTimer += Time.deltaTime;

            if (deliveryTimer >= deliveryTime)
            {
                CompleteDelivery();
                deliveryTimer = 0f;
            }
        }
        else
        {
            deliveryTimer = 0f;
        }

        UpdateAreaPulse();
    }

    public void Activate()
    {
        isActive = true;

        if (areaSpritePrefab != null && areaInstance == null)
        {
            GameObject go = Instantiate(areaSpritePrefab, transform.position, Quaternion.identity, transform);
            areaInstance = go.transform;
            baseScale = Vector3.one * deliveryRange * 2f;
            areaInstance.localScale = baseScale;
        }
    }

    public void Deactivate()
    {
        isActive = false;
        deliveryTimer = 0f;

        if (areaInstance != null)
        {
            Destroy(areaInstance.gameObject);
            areaInstance = null;
        }
    }

    private void UpdateAreaPulse()
    {
        if (areaInstance == null) return;

        float scaleOffset = Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
        areaInstance.localScale = baseScale + Vector3.one * scaleOffset;
        areaInstance.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.Self);
    }

	private void CompleteDelivery()
	{
		if (GameMaster.Instance.pizzasInCar > 0)
		{
			GameMaster.Instance.OnSuccessfulDelivery();
			Deactivate();
		}
		else
		{
			Debug.Log("🚫 No pizza to deliver.");
		}
	}

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, deliveryRange);
    }
}
