using UnityEngine;

public class ArrowPointer : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Transform pizzaTarget; // A transform above the pizza shop
    public Camera mainCamera;

    [Header("In-View Behavior")]
    public Vector3 shopOffset = new Vector3(0, 2f, 0);
    public float oscillationAmplitude = 0.5f;
    public float oscillationSpeed = 2f;

    [Header("Out-of-View Behavior")]
    public float followOffsetY = 3f;
    public float orbitRadius = 4f;
    public float orbitHeight = 2f;
    public float spinSpeed = 180f; // Degrees per second on Z

    [Header("Transition Settings")]
    public float transitionSpeed = 5f; // Lerp speed

    private bool isTargetVisible;
    private float oscillationTimer;

    void Start()
    {
        if (!mainCamera) mainCamera = Camera.main;
    }

    void Update()
    {
        if (pizzaTarget == null || player == null || mainCamera == null) return;

        // Check if pizzaTarget is visible in the camera's viewport
        Vector3 viewportPoint = mainCamera.WorldToViewportPoint(pizzaTarget.position);
        isTargetVisible = viewportPoint.z > 0 &&
                          viewportPoint.x > 0 && viewportPoint.x < 1 &&
                          viewportPoint.y > 0 && viewportPoint.y < 1;

        if (isTargetVisible)
        {
            UpdateAboveShop();
        }
        else
        {
            UpdateAroundPlayer();
        }
    }

    void UpdateAboveShop()
    {
        oscillationTimer += Time.deltaTime * oscillationSpeed;
        float yOffset = Mathf.Sin(oscillationTimer) * oscillationAmplitude;

        Vector3 targetPos = pizzaTarget.position + shopOffset + new Vector3(0, yOffset, 0);
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * transitionSpeed);
        transform.rotation = Quaternion.Lerp(transform.rotation, pizzaTarget.rotation, Time.deltaTime * transitionSpeed);
    }

    void UpdateAroundPlayer()
    {
        // Position the arrow around the player, elevated
        Vector3 dirToPizza = (pizzaTarget.position - player.position).normalized;
        Vector3 offset = -dirToPizza * orbitRadius + Vector3.up * orbitHeight;

        Vector3 targetPos = player.position + offset;
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * transitionSpeed);

		// Look at pizza shop
		Quaternion baseLookRotation = Quaternion.LookRotation(dirToPizza, Vector3.up);

		// Add spin offset around Z axis
		float spinAngle = Time.time * spinSpeed;
		Quaternion spinOffset = Quaternion.Euler(0f, 0f, spinAngle);

		// Combine both rotations
		Quaternion finalRotation = baseLookRotation * spinOffset;
		transform.rotation = Quaternion.Lerp(transform.rotation, finalRotation, Time.deltaTime * transitionSpeed);
    }
}
