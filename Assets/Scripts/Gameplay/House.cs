using UnityEngine;

public class House : MonoBehaviour
{
    [Header("Delivery Settings")]
    public float deliveryRange = 5f;
    public float maxDeliveryDuration = 20f;  // Maximum time allowed to complete delivery
    public float minTip = 10f;               // Minimum tip amount
    public float maxTip = 50f;               // Maximum tip amount
    public bool timed = true;                // If false, delivery never fails due to time

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

        // Only update timer if this house is timed.
        if (timed)
        {
            deliveryTimer += Time.deltaTime;

            // If the delivery takes too long, fail the delivery.
            if (deliveryTimer >= maxDeliveryDuration)
            {
                GameMaster.Instance.FailDelivery();
                Deactivate();
                return;
            }
        }

        float distance = Vector3.Distance(transform.position, player.position);

        // If the player is within the delivery range, complete the delivery.
        if (distance <= deliveryRange && GameMaster.Instance.pizzasInCar > 0)
        {
            CompleteDelivery();
        }

        // Update the indicator text with the remaining time.
        if (indicator != null)
        {
            float timeLeft = GetTimeLeft();
            string formattedTime = FormatTime(timeLeft);

            // Assumes the indicator prefab has an Indicator component.
            Indicator indComp = indicator.GetComponent<Indicator>();
            if (indComp != null)
            {
                indComp.UpdateIndicatorText(formattedTime, Vector3.Distance(transform.position, player.position));
            }

			if (timed)
			{
				float percent = Mathf.Clamp01(GetTimeLeft() / maxDeliveryDuration);

				// Interpolate from red (0%) to yellow (50%) to green (100%)
				Color timeColor;
				if (percent > 0.5f)
				{
					float t = (percent - 0.5f) * 2f;
					timeColor = Color.Lerp(Color.yellow, Color.green, t);
				}
				else
				{
					float t = percent * 2f;
					timeColor = Color.Lerp(Color.red, Color.yellow, t);
				}

				indComp.SetTextColor(timeColor);

				if (!warningPassed && percent <= warningThreshold / maxDeliveryDuration)
				{
					MusicManager.Instance.PlaySFX(warningSound);
					warningPassed = true;
				}
			}
			else
			{
				indComp.SetTextColor(Color.green); // Infinite time = always green
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
        float tip;
        // If not timed, award the maximum tip.
        if (!timed)
        {
            tip = maxTip;
        }
        else
        {
            // Calculate tip based on delivery time.
            float normalizedTime = Mathf.Clamp01(deliveryTimer / maxDeliveryDuration);
            tip = Mathf.Lerp(maxTip, minTip, normalizedTime);
        }

        GameMaster.Instance.OnSuccessfulDelivery(tip);
        Deactivate();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, deliveryRange);
    }

    // Returns the remaining delivery time in seconds. If not timed, returns Infinity.
    public float GetTimeLeft()
    {
        if (!timed)
            return Mathf.Infinity;
        return Mathf.Max(0f, maxDeliveryDuration - deliveryTimer);
    }

    // Formats time into a string "SS:MS". If time is Infinity, returns "∞".
    private string FormatTime(float seconds)
    {
        if (float.IsInfinity(seconds))
            return "";
        int secs = Mathf.FloorToInt(seconds);
        return string.Format("{0:00}", secs);
    }
}
