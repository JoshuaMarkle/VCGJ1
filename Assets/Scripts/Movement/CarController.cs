using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CarController : MonoBehaviour
{
    [Header("Car Settings")]
    public float moveSpeed = 1500f;
    public float maxSpeed = 100f;
    public float maxSteerAngle = 25f;
    public float brakeForce = 3000f;
    public Transform centerOfMass;

    [Header("SFX")]
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
        // Input
        steerInput = Input.GetAxis("Horizontal");
        throttleInput = Input.GetAxis("Vertical");

        // Update visuals and drift on each wheel
        UpdateWheel(frontLeftWheel);
        UpdateWheel(frontRightWheel);
        UpdateWheel(rearLeftWheel);
        UpdateWheel(rearRightWheel);

        UpdateEngineSound();
    }

    private void FixedUpdate()
    {
        float steer = steerInput * maxSteerAngle;
        float currentSpeedKph = rb.linearVelocity.magnitude * 3.6f;

        bool isAccelerating = Mathf.Abs(throttleInput) > 0.05f;
        float torque = isAccelerating && currentSpeedKph < maxSpeed
            ? throttleInput * moveSpeed
            : 0f;

        // Steer front wheels
        frontLeftWheel.ApplySteering(steer);
        frontRightWheel.ApplySteering(steer);

        // Drive or brake rear wheels
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

    private void UpdateWheel(Wheel wheel)
    {
        wheel.UpdateVisual();
        wheel.UpdateDrift();
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
}
