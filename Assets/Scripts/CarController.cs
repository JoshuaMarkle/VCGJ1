using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CarController : MonoBehaviour
{
    [Header("Wheel References")]
    public WheelCollider frontLeftWheelCollider;
    public WheelCollider frontRightWheelCollider;
    public WheelCollider rearLeftWheelCollider;
    public WheelCollider rearRightWheelCollider;

    public Transform frontLeftTransform;
    public Transform frontRightTransform;
    public Transform rearLeftTransform;
    public Transform rearRightTransform;

    [Header("Car Settings")]
    public float maxMotorTorque = 1500f;
    public float maxSteerAngle = 25f;
    public Transform centerOfMass;

    [Header("Drifting")]
    public float driftSlipThreshold = 0.2f;
    public float smokeRateMultiplier = 300f;

    [Header("Drift Effects")]
    public ParticleSystem frontLeftSmoke;
    public ParticleSystem frontRightSmoke;
    public ParticleSystem rearLeftSmoke;
    public ParticleSystem rearRightSmoke;

	[Header("SFX")]
    public AudioSource driftSound;
    public AudioSource engineSound;

    private Rigidbody rb;
    private float steerInput;
    private float throttleInput;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (centerOfMass) rb.centerOfMass = centerOfMass.localPosition;
    }

    private void Update()
    {
        // Get input
        steerInput = Input.GetAxis("Horizontal");
        throttleInput = Input.GetAxis("Vertical");

        // Update visuals
        UpdateWheelVisuals(frontLeftWheelCollider, frontLeftTransform);
        UpdateWheelVisuals(frontRightWheelCollider, frontRightTransform);
        UpdateWheelVisuals(rearLeftWheelCollider, rearLeftTransform);
        UpdateWheelVisuals(rearRightWheelCollider, rearRightTransform);
    }

    private void FixedUpdate()
    {
        // Apply motor torque
        rearLeftWheelCollider.motorTorque = throttleInput * maxMotorTorque;
        rearRightWheelCollider.motorTorque = throttleInput * maxMotorTorque;

        // Apply steering to front wheels
        float steer = steerInput * maxSteerAngle;
        frontLeftWheelCollider.steerAngle = steer;
        frontRightWheelCollider.steerAngle = steer;

        // Check and apply drift effects
        HandleDrift(frontLeftWheelCollider, frontLeftSmoke);
        HandleDrift(frontRightWheelCollider, frontRightSmoke);
        HandleDrift(rearLeftWheelCollider, rearLeftSmoke);
        HandleDrift(rearRightWheelCollider, rearRightSmoke);
    }

    private void HandleDrift(WheelCollider wheel, ParticleSystem smoke)
    {
        if (wheel.GetGroundHit(out WheelHit hit))
        {
            float slipAmount = Mathf.Abs(hit.sidewaysSlip);
            var emission = smoke.emission;

            if (slipAmount > driftSlipThreshold)
            {
                emission.rateOverTime = slipAmount * smokeRateMultiplier;
                if (!smoke.isPlaying) smoke.Play();
                if (!driftSound.isPlaying) driftSound.Play();
                driftSound.volume = Mathf.Clamp(slipAmount, 0.2f, 1f);
            }
            else
            {
                emission.rateOverTime = 0f;
                if (smoke.isPlaying) smoke.Stop();
                driftSound.volume = 0f;
            }
        }
    }

    private void UpdateWheelVisuals(WheelCollider collider, Transform wheelTransform)
    {
        Vector3 pos;
        Quaternion rot;
        collider.GetWorldPose(out pos, out rot);
        wheelTransform.position = pos;
        wheelTransform.rotation = rot;
    }
}
