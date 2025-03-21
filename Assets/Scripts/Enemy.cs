using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Enemy : MonoBehaviour
{
    private Rigidbody rb;

    [Header("Target")]
    public Transform player;
    public float predictionTime = 0.5f;
    public float reverseDistance = 5.0f;

    [Header("Car Settings")]
    public float moveSpeed = 1500f;
    public float maxSpeed = 100f;
    public float maxSteerAngle = 25f;
    public float brakeForce = 3000f;
    public Transform centerOfMass;

    [Header("Engine Sound")]
    public AudioSource engineSound;
    public float minEnginePitch = 0.8f;
    public float maxEnginePitch = 2f;

    [Header("Wheels")]
    public Wheel frontLeftWheel;
    public Wheel frontRightWheel;
    public Wheel rearLeftWheel;
    public Wheel rearRightWheel;

    [Header("Reverse Logic")]
    public float reverseDecisionCooldown = 1f;

    private float reverseDecisionTimer = 0f;
    private bool reversing = false;

    [Header("Boid Separation")]
    public float separationRadius = 8f;
    public float separationStrength = 1.2f;
    public LayerMask enemyLayer;

	[Header("Obstacle Avoidance")]
	public Transform forwardRayPos, leftRayPos, rightRayPos;
	public float detectionDistance = 6f;
	public float avoidDuration = 1.5f;
	public float avoidSteerDirection = 1f;

	private bool isAvoidingObstacle = false;
	private float avoidTimer = 0f;

    private float steerInput;
    private float throttleInput;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (centerOfMass) rb.centerOfMass = centerOfMass.localPosition;
    }

    void Update()
    {
        if (player == null) return;

        Vector3 playerVelocity = player.GetComponent<Rigidbody>()?.linearVelocity ?? Vector3.zero;
        Vector3 predictedPlayerPosition = player.position + playerVelocity * predictionTime;

        // Boid-style separation
        Vector3 separationForce = Vector3.zero;
        Collider[] neighbors = Physics.OverlapSphere(transform.position, separationRadius, enemyLayer);

        foreach (Collider neighbor in neighbors)
        {
            if (neighbor.transform == transform) continue;
            Vector3 toSelf = transform.position - neighbor.transform.position;
            float distance = toSelf.magnitude;

            if (distance > 0f)
                separationForce += toSelf.normalized / distance;
        }


		// Obstacle avoidance check
		if (isAvoidingObstacle)
		{
			avoidTimer -= Time.deltaTime;

			throttleInput = -1f; // reverse
			steerInput = avoidSteerDirection; // turn away

			if (avoidTimer <= 0f)
			{
				isAvoidingObstacle = false;
			}

			UpdateAllWheels();
			UpdateEngineSound();
			return;
		}
		else if (IsObstacleAhead())
		{
			isAvoidingObstacle = true;
			avoidTimer = avoidDuration;

			// Randomize direction a bit to make AI less robotic
			avoidSteerDirection = Random.value > 0.5f ? 1f : -1f;

			throttleInput = -1f;
			steerInput = avoidSteerDirection;

			UpdateAllWheels();
			UpdateEngineSound();
			return;
		}

        // Combine chase direction with avoidance
        Vector3 dirToTarget = (predictedPlayerPosition - transform.position).normalized;
        Vector3 combinedDirection = (dirToTarget + separationForce * separationStrength).normalized;

        float distanceToTarget = Vector3.Distance(transform.position, predictedPlayerPosition);
        float dot = Vector3.Dot(transform.forward, combinedDirection);
        float angleToTarget = Vector3.SignedAngle(transform.forward, combinedDirection, Vector3.up);

        // Update reverse decision cooldown
        reverseDecisionTimer -= Time.deltaTime;

        // Steering input
        steerInput = Mathf.Clamp(angleToTarget / 45f, -1f, 1f);

        // Decide forward or reverse
        if (reverseDecisionTimer <= 0f)
        {
            if (dot < 0.1f && distanceToTarget <= reverseDistance)
            {
                reversing = true;
            }
            else
            {
                reversing = false;
            }

            reverseDecisionTimer = reverseDecisionCooldown;
        }

        // Don't reverse if too far
        if (dot < 0.1f && distanceToTarget > reverseDistance)
        {
            throttleInput = 0f;
            frontLeftWheel.ApplySteering(0f);
            frontRightWheel.ApplySteering(0f);
            rearLeftWheel.ApplyBrake(brakeForce);
            rearRightWheel.ApplyBrake(brakeForce);
            return;
        }

        throttleInput = reversing ? -1f : 1f;

        // Update wheels + engine
        UpdateAllWheels();
        UpdateEngineSound();
    }

    void FixedUpdate()
    {
        float steer = steerInput * maxSteerAngle;
        float currentSpeedKph = rb.linearVelocity.magnitude * 3.6f;

        bool isAccelerating = Mathf.Abs(throttleInput) > 0.05f;
        float torque = isAccelerating && currentSpeedKph < maxSpeed
            ? throttleInput * moveSpeed
            : 0f;

        frontLeftWheel.ApplySteering(steer);
        frontRightWheel.ApplySteering(steer);

        if (isAccelerating)
        {
            rearLeftWheel.ApplyThrottle(torque);
            rearRightWheel.ApplyThrottle(torque);
        }
        else
        {
            rearLeftWheel.ApplyBrake(brakeForce);
            rearRightWheel.ApplyBrake(brakeForce);
        }
    }

	void UpdateAllWheels()
	{
		UpdateWheel(frontLeftWheel);
		UpdateWheel(frontRightWheel);
		UpdateWheel(rearLeftWheel);
		UpdateWheel(rearRightWheel);
	}

    void UpdateWheel(Wheel wheel)
    {
        wheel.UpdateVisual();
        wheel.UpdateDrift();
    }

    float GetSpeed()
    {
        return rb.linearVelocity.magnitude * 3.6f;
    }

    void UpdateEngineSound()
    {
        float speedPercent = rb.linearVelocity.magnitude / 100f;
        float throttleEffect = Mathf.Abs(throttleInput);

        float targetPitch = Mathf.Lerp(minEnginePitch, maxEnginePitch, throttleEffect + speedPercent);
        engineSound.pitch = Mathf.Lerp(engineSound.pitch, targetPitch, Time.deltaTime * 5f);
        engineSound.volume = Mathf.Clamp01(throttleEffect + speedPercent);
    }

	bool IsObstacleAhead()
	{
		Vector3 forward = transform.forward;

		// Main ray
		if (Physics.Raycast(forwardRayPos.position, transform.forward, detectionDistance))
			return true;

		// Side rays
		Vector3 left = Quaternion.AngleAxis(-30, Vector3.up) * forward;
		Vector3 right = Quaternion.AngleAxis(30, Vector3.up) * forward;

		if (Physics.Raycast(leftRayPos.position, left, detectionDistance * 0.8f)) return true;
		if (Physics.Raycast(rightRayPos.position, right, detectionDistance * 0.8f)) return true;

		return false;
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.red;

		Vector3 forward = transform.forward;
		Gizmos.DrawLine(forwardRayPos.position, forwardRayPos.position + forward * detectionDistance);

		Vector3 left = Quaternion.AngleAxis(-30, Vector3.up) * forward;
		Vector3 right = Quaternion.AngleAxis(30, Vector3.up) * forward;
		Gizmos.DrawLine(leftRayPos.position, leftRayPos.position + left * detectionDistance * 0.8f);
		Gizmos.DrawLine(rightRayPos.position, rightRayPos.position + right * detectionDistance * 0.8f);
	}
}
