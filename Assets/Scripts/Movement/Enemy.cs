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
    public float brakeForce = 3000f;
    public Transform centerOfMass;

    [Header("Engine Sound")]
    public float minEnginePitch = 0.8f;
    public float maxEnginePitch = 2f;

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

        // --- New Decision Logic using PoliceDecision enum ---
        // Basic decision: if target is behind (low dot) or too close, reverse; otherwise, chase forward.
        if (distanceToTarget <= reverseDistance || dot < 0.1f)
            currentDecision = PoliceDecision.Reversing;
        else
            currentDecision = PoliceDecision.ChasingForward;

        // Check if the police car is stuck (low speed for a duration)
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

        // Set inputs based on decision state:
        if (currentDecision == PoliceDecision.ChasingForward)
            throttleInput = 1f;  // full forward
        else // Reversing or Stuck
            throttleInput = -1f; // full reverse

        // Steering input is based on angle difference (for both forward and reverse, reverse might invert steering)
        steerInput = Mathf.Clamp(angleToTarget / 45f, -1f, 1f);
        if (currentDecision == PoliceDecision.Reversing || currentDecision == PoliceDecision.Stuck)
        {
            // When reversing, flip steering to make the car turn appropriately.
            steerInput *= -1f;
        }

        // Update wheels with calculated inputs
        UpdateAllWheels();
    }

    void FixedUpdate()
    {
        float steer = steerInput * maxSteerAngle;
        float currentSpeedKph = rb.linearVelocity.magnitude * 3.6f;
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

    float GetSpeed()
    {
        return rb.linearVelocity.magnitude * 3.6f;
    }

    bool IsObstacleAhead()
    {
        Vector3 forward = transform.forward;

        // Main ray from forwardRayPos
        if (Physics.Raycast(forwardRayPos.position, forward, detectionDistance, obstacleLayer))
            return true;

        // Side rays from left and right positions
        Vector3 left = Quaternion.AngleAxis(-30, Vector3.up) * forward;
        Vector3 right = Quaternion.AngleAxis(30, Vector3.up) * forward;
		if (Physics.Raycast(leftRayPos.position, left, detectionDistance * 0.8f, obstacleLayer)) return true;
		if (Physics.Raycast(rightRayPos.position, right, detectionDistance * 0.8f, obstacleLayer)) return true;

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

    private void ApplyStabilization()
    {
        // Apply downforce proportional to speed
        Vector3 force = -transform.up * downForce * rb.linearVelocity.magnitude;
        rb.AddForce(force);

        // Auto-flip if flipped too much
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
}
