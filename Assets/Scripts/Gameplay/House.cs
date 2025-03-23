using UnityEngine;

public class House : MonoBehaviour
{
    [Header("Delivery Settings")]
    public float deliveryRange = 5f;
    public float maxDeliveryDuration = 20f;  // Maximum time allowed to complete delivery
    public float minTip = 10f;               // Minimum tip amount
    public float maxTip = 50f;               // Maximum tip amount

    [Header("Area Visual")]
    public GameObject areaPrefab;
    public float pulseSpeed = 2f;
    public float pulseAmount = 0.1f;
    public float spinSpeed = 10f;
    public float areaScale = 1f;

    [Header("Indicator")]
    public GameObject indicatorPrefab;  // Prefab for the indicator UI (should have an Indicator component)
    private GameObject indicator;       // Instance of the indicator
	public float warningThreshold = 10f;
	public AudioClip warningSound;
	private bool warningPassed = false;

    private Transform player;
    private float deliveryTimer = 0f;
    private bool isActive = false;

    private Transform areaInstance;
    private Vector3 baseScale;

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
		warningPassed = false;
    }

    private void Update()
    {
        if (!isActive || player == null) return;

        // Increase delivery timer as soon as the house is active.
        deliveryTimer += Time.deltaTime;

        // If the delivery takes too long, fail the delivery.
        if (deliveryTimer >= maxDeliveryDuration)
        {
            GameMaster.Instance.FailDelivery();
            Deactivate();
            return;
        }

        float distance = Vector3.Distance(transform.position, player.position);

        // If the player is within the delivery range, complete the delivery.
        if (distance <= deliveryRange)
        {
            CompleteDelivery();
        }

        // Update the indicator text with the time left formatted as MM:SS.
        if (indicator != null)
        {
            float timeLeft = GetTimeLeft();
            string formattedTime = FormatTime(timeLeft);
			
            // Assumes the indicator prefab has an Indicator component.
            Indicator indComp = indicator.GetComponent<Indicator>();
            if (indComp != null)
            {
                indComp.UpdateIndicatorText(formattedTime);
            }

			// Change the color
			if (timeLeft <= warningThreshold) {
				indComp.SetTextColor(Color.red);

				// Play sound
				if (!warningPassed)
					MusicManager.Instance.PlaySFX(warningSound);
				warningPassed = true;
			} else {
				indComp.SetTextColor(Color.white); // Reset if back to safe
			}
        }

        UpdateAreaPulse();
    }

    public void Activate()
    {
        isActive = true;
        deliveryTimer = 0f;

        // Spawn area visual if not already spawned.
        if (areaPrefab != null && areaInstance == null)
        {
            GameObject go = Instantiate(areaPrefab, transform.position, Quaternion.identity, transform);
            areaInstance = go.transform;
            baseScale = Vector3.one * deliveryRange * areaScale;
            areaInstance.localScale = baseScale;
        }
        // Spawn indicator if prefab provided.
        if (indicatorPrefab != null && indicator == null)
        {
            indicator = Instantiate(indicatorPrefab);

			// Find the UI Canvas (make sure your Canvas is tagged "UI")
			GameObject canvasObj = GameObject.FindGameObjectWithTag("UI");
			if (canvasObj != null)
			{
				// Parent the indicator to the Canvas. Setting worldPositionStays to false
				// makes it adopt the local coordinates of the Canvas.
				indicator.transform.SetParent(canvasObj.transform, false);
			}

            // Set the target for the indicator to this house.
            Indicator indComp = indicator.GetComponent<Indicator>();
            if (indComp != null)
            {
                indComp.target = transform;
            }
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
        if (indicator != null)
        {
            Destroy(indicator);
            indicator = null;
        }
    }

    // Returns whether this house is currently active.
    public bool IsActive()
    {
        return isActive;
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
        // Calculate tip based on delivery time.
        // A fast delivery (deliveryTimer near 0) gets near maxTip,
        // while a slow delivery (deliveryTimer near maxDeliveryDuration) gets near minTip.
        float normalizedTime = Mathf.Clamp01(deliveryTimer / maxDeliveryDuration);
        float tip = Mathf.Lerp(maxTip, minTip, normalizedTime);
        GameMaster.Instance.OnSuccessfulDelivery(tip);
        Deactivate();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, deliveryRange);
    }

    // Returns the remaining delivery time in seconds.
    public float GetTimeLeft()
    {
        return Mathf.Max(0f, maxDeliveryDuration - deliveryTimer);
    }

	// Formats time into a string "SS:MS"
	private string FormatTime(float seconds)
	{
		int secs = Mathf.FloorToInt(seconds);
		int millis = Mathf.FloorToInt((seconds - secs) * 1000f);
		return string.Format("{0:00}:{1:000}", secs, millis);
	}
}
