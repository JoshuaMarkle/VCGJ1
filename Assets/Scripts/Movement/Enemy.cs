using UnityEngine;
using UnityEngine.AI;

public enum PoliceDecision
{
    ChasingForward,
    Reversing,
    Stuck
}

[RequireComponent(typeof(Rigidbody))]
public class Enemy : MonoBehaviour
{
    private Rigidbody rb;
    private Rigidbody playerRb;

    [Header("Target")]
    public Transform player;
    public float predictionTime = 0.5f;
    public float reverseDistance = 5.0f;
    public float catchDistance = 1.0f;
    public float catchTime = 1.0f;
    public float minPlayerCatchSpeed = 2.0f;
    private float catchTimer = 0f;
    private bool hasCaughtPlayer = false;

    [Header("Car Settings")]
    public float moveSpeed = 1500f;
    public float maxSpeed = 100f;
    public float maxSteerAngle = 25f;
    public float minSteerAngle = 5f; // New: for effective steering at high speeds
    public float brakeForce = 3000f;
    public Transform centerOfMass;

    [Header("Engine Sound")]
    public AudioSource engineSound;
    public float minEnginePitch = 0.8f;
    public float maxEnginePitch = 2f;
    public float engineVolMin = 0.1f;
    public float engineVolMax = 0.5f;

    [Header("Wheels")]
    public Wheel frontLeftWheel;
    public Wheel frontRightWheel;
    public Wheel rearLeftWheel;
    public Wheel rearRightWheel;

    [Header("Stabilization Settings")]
    public float downForce = 1000f;
    public float flipTorque = 500f;
    public float flipDetectionAngle = 120f;
    public float timeToAutoFlip = 2f;
    public float autoFlipTorque = 10000f;
    public float autoFlipUpForce = 10000f;
    private float flippedTimer = 0f;

    [Header("Water Settings")]
    public float waterYThreshold = -10f;
    public AudioClip splashSound;

    [Header("Reverse Logic")]
    public float reverseDecisionCooldown = 1f;
    private float reverseDecisionTimer = 0f;

    [Header("Boid Separation")]
    public float separationRadius = 8f;
    public float separationStrength = 1.2f;
    public LayerMask enemyLayer;

    [Header("Obstacle Avoidance")]
    public LayerMask obstacleLayer;
    public Transform forwardRayPos, leftRayPos, rightRayPos;
    public float detectionDistance = 6f;
    public float avoidDuration = 1.5f;
    public float avoidSteerDirection = 1f;
    private bool isAvoidingObstacle = false;
    private float avoidTimer = 0f;

    [Header("NavMesh Pathfinding")]
    public float pathUpdateInterval = 0.5f;
    private float pathUpdateTimer = 0f;
    private NavMeshPath navPath;

    [Header("Police Decision State")]
    public PoliceDecision currentDecision = PoliceDecision.ChasingForward;
    private float stuckTimer = 0f;

    private float steerInput;
    private float throttleInput;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (centerOfMass) rb.centerOfMass = centerOfMass.localPosition;

        if (!player)
            player = GameObject.FindGameObjectWithTag("Player").transform;
        playerRb = player.GetComponent<Rigidbody>();

        navPath = new NavMeshPath();
    }

    void Update()
    {
        if (player == null) return;

        // New: Update engine sound and check for water death
        UpdateEngineSound();
        CheckWaterDeath();

        // Get predicted player position
        Vector3 playerVelocity = playerRb.linearVelocity;
        Vector3 predictedPlayerPosition = player.position + playerVelocity * predictionTime;

        // Update NavMesh path periodically
        pathUpdateTimer -= Time.deltaTime;
        if (pathUpdateTimer <= 0f)
        {
            NavMesh.CalculatePath(transform.position, predictedPlayerPosition, NavMesh.AllAreas, navPath);
            pathUpdateTimer = pathUpdateInterval;
        }

        // Determine path target: use second corner if available
        Vector3 pathTarget = predictedPlayerPosition;
        if (navPath != null && navPath.corners.Length > 1)
        {
            pathTarget = navPath.corners[1];
        }

        // --- Catch player logic ---
        float distanceToTarget = Vector3.Distance(transform.position, pathTarget);
        if (!hasCaughtPlayer && distanceToTarget <= catchDistance && playerRb.linearVelocity.magnitude < minPlayerCatchSpeed)
        {
            catchTimer += Time.deltaTime;
            if (catchTimer >= catchTime)
            {
                hasCaughtPlayer = true;
                GameMaster.Instance?.CatchPlayer();
            }
        }
        else
        {
            catchTimer = 0f;
        }

        // --- Boid-style separation ---
        Vector3 separationForce = Vector3.zero;
        Collider[] neighbors = Physics.OverlapSphere(transform.position, separationRadius, enemyLayer);
        foreach (Collider neighbor in neighbors)
        {
            if (neighbor.transform == transform) continue;
            Vector3 toSelf = transform.position - neighbor.transform.position;
            float dist = toSelf.magnitude;
            if (dist > 0f)
                separationForce += toSelf.normalized / dist;
        }

        // --- Obstacle avoidance ---
        if (isAvoidingObstacle)
        {
            avoidTimer -= Time.deltaTime;
            throttleInput = -1f; // reverse
            steerInput = avoidSteerDirection; // steer away
            if (avoidTimer <= 0f)
                isAvoidingObstacle = false;
            UpdateAllWheels();
            return;
        }
        else if (IsObstacleAhead())
        {
            isAvoidingObstacle = true;
            avoidTimer = avoidDuration;
            avoidSteerDirection = Random.value > 0.5f ? 1f : -1f;
            throttleInput = -1f;
            steerInput = avoidSteerDirection;
            UpdateAllWheels();
            return;
        }

        // --- Combine target direction with separation ---
        Vector3 dirToTarget = (pathTarget - transform.position).normalized;
        Vector3 combinedDirection = (dirToTarget + separationForce * separationStrength).normalized;

        float dot = Vector3.Dot(transform.forward, combinedDirection);
        float angleToTarget = Vector3.SignedAngle(transform.forward, combinedDirection, Vector3.up);

        // --- Decision Logic ---
        if (distanceToTarget <= reverseDistance || dot < 0.1f)
            currentDecision = PoliceDecision.Reversing;
        else
            currentDecision = PoliceDecision.ChasingForward;

        if (rb.linearVelocity.magnitude < 2f)
        {
            stuckTimer += Time.deltaTime;
            if (stuckTimer > 2f)
                currentDecision = PoliceDecision.Stuck;
        }
        else
        {
            stuckTimer = 0f;
        }

        // Set throttle based on decision state:
        throttleInput = (currentDecision == PoliceDecision.ChasingForward) ? 1f : -1f;

        // Steering input is determined by the angle to target.
        steerInput = Mathf.Clamp(angleToTarget / 45f, -1f, 1f);
        if (currentDecision == PoliceDecision.Reversing || currentDecision == PoliceDecision.Stuck)
            steerInput *= -1f;

        UpdateAllWheels();
    }

    void FixedUpdate()
    {
        float currentSpeedKph = rb.linearVelocity.magnitude * 3.6f;
        // Calculate effective steering angle: interpolates between maxSteerAngle and minSteerAngle.
        float effectiveSteerAngle = Mathf.Lerp(maxSteerAngle, minSteerAngle, currentSpeedKph / maxSpeed);
        float steer = steerInput * effectiveSteerAngle;
        bool isAccelerating = Mathf.Abs(throttleInput) > 0.05f;
        float torque = isAccelerating && currentSpeedKph < maxSpeed ? throttleInput * moveSpeed : 0f;

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

        ApplyStabilization();
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

    bool IsObstacleAhead()
    {
        Vector3 forward = transform.forward;
        if (Physics.Raycast(forwardRayPos.position, forward, detectionDistance, obstacleLayer))
            return true;

        Vector3 left = Quaternion.AngleAxis(-30, Vector3.up) * forward;
        Vector3 right = Quaternion.AngleAxis(30, Vector3.up) * forward;
        if (Physics.Raycast(leftRayPos.position, left, detectionDistance * 0.8f, obstacleLayer)) return true;
        if (Physics.Raycast(rightRayPos.position, right, detectionDistance * 0.8f, obstacleLayer)) return true;

        return false;
    }

    private void ApplyStabilization()
    {
        Vector3 force = -transform.up * downForce * rb.linearVelocity.magnitude;
        rb.AddForce(force);

        float angle = Vector3.Angle(Vector3.up, transform.up);
        if (angle > flipDetectionAngle)
        {
            flippedTimer += Time.fixedDeltaTime;
            Vector3 flipDirection = Vector3.Cross(transform.up, Vector3.up);
            rb.AddTorque(flipDirection * flipTorque);
            if (flippedTimer >= timeToAutoFlip)
            {
                rb.AddTorque(transform.right * autoFlipTorque);
                rb.AddForce(Vector3.up * autoFlipUpForce);
            }
        }
        else
        {
            flippedTimer = 0f;
        }
    }

    private void UpdateEngineSound()
    {
        float speedPercent = rb.linearVelocity.magnitude / 100f;
        float throttleEffect = Mathf.Abs(throttleInput);
        float targetPitch = Mathf.Lerp(minEnginePitch, maxEnginePitch, throttleEffect + speedPercent);
        engineSound.pitch = Mathf.Lerp(engineSound.pitch, targetPitch, Time.deltaTime * 5f);

        float targetVolume = Mathf.Lerp(engineVolMin, engineVolMax, throttleEffect + speedPercent);
        engineSound.volume = Mathf.Lerp(engineSound.volume, targetVolume, Time.deltaTime * 5f);
    }

    private void CheckWaterDeath()
    {
        if (transform.position.y < waterYThreshold)
        {
            if (splashSound != null)
                AudioSource.PlayClipAtPoint(splashSound, transform.position);
            rb.linearVelocity = Vector3.zero;

			GameMaster.Instance.SpawnPoliceUnit();
			Destroy(gameObject);
        }
    }
}
