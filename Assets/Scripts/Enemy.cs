using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Enemy : MonoBehaviour
{
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

    [Header("AI Timing")]
    public float reverseDecisionCooldown = 1f;

    private Rigidbody rb;
    private float steerInput;
    private float throttleInput;
    private float reverseDecisionTimer = 0f;
    private bool reversing = false;

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

        Vector3 dirToTarget = (predictedPlayerPosition - transform.position).normalized;
        float distanceToTarget = Vector3.Distance(transform.position, predictedPlayerPosition);
        float dot = Vector3.Dot(transform.forward, dirToTarget);
        float angleToTarget = Vector3.SignedAngle(transform.forward, dirToTarget, Vector3.up);

        // Update reverse decision cooldown
        reverseDecisionTimer -= Time.deltaTime;

        // Steering input
        steerInput = Mathf.Clamp(angleToTarget / 45f, -1f, 1f);

        // Decide forward or reverse
        if (reverseDecisionTimer <= 0f)
        {
            // Only flip direction after cooldown
            if (dot < 0.1f && distanceToTarget > reverseDistance)
            {
                reversing = true;
            }
            else
            {
                reversing = false;
            }

            reverseDecisionTimer = reverseDecisionCooldown;
        }

        throttleInput = reversing ? -1f : 1f;

        // Smart braking
        if (distanceToTarget < 20f && GetSpeed() > 15f)
        {
            throttleInput = -1f;
        }

        // Update wheels
        UpdateWheel(frontLeftWheel);
        UpdateWheel(frontRightWheel);
        UpdateWheel(rearLeftWheel);
        UpdateWheel(rearRightWheel);

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
}
