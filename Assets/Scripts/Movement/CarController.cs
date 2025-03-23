using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CarController : MonoBehaviour
{
    [Header("Car Settings")]
    public float moveSpeed = 1500f;
    public float maxSpeed = 100f;
    public float maxSteerAngle = 25f;
	public float minSteerAngle = 5f;
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
		// steerInput = InputSystem.Instance.moveDir.x;
		// throttleInput = InputSystem.Instance.moveDir.y;

        // Update visuals and drift on each wheel
        UpdateWheel(frontLeftWheel);
        UpdateWheel(frontRightWheel);
        UpdateWheel(rearLeftWheel);
        UpdateWheel(rearRightWheel);

        UpdateEngineSound();

		CheckWaterDeath();
    }

    private void FixedUpdate()
    {
		float currentSpeedKph = rb.linearVelocity.magnitude * 3.6f;

        // Calculate effective steer angle based on current speed.
        // At 0 km/h, effective angle = maxSteerAngle.
        // At maxSpeed, effective angle = minSteerAngle.
        float effectiveSteerAngle = Mathf.Lerp(maxSteerAngle, minSteerAngle, currentSpeedKph / maxSpeed);
        float steer = steerInput * effectiveSteerAngle;

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

		// Stabilize
		ApplyStabilization();
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

	private void ApplyStabilization()
	{
		// Downforce
		Vector3 force = -transform.up * downForce * rb.linearVelocity.magnitude;
		rb.AddForce(force);

		// Check angle between car up and world up
		float angle = Vector3.Angle(Vector3.up, transform.up);

		if (angle > flipDetectionAngle)
		{
			flippedTimer += Time.fixedDeltaTime;

			// Passive flip assistance while upside down
			Vector3 flipDirection = Vector3.Cross(transform.up, Vector3.up);
			rb.AddTorque(flipDirection * flipTorque);

			// Stronger flip if stuck upside down for too long
			if (flippedTimer >= timeToAutoFlip)
			{
				rb.AddTorque(transform.right * autoFlipTorque);
				rb.AddForce(Vector3.up * autoFlipUpForce);
			}
		}
		else
		{
			flippedTimer = 0f; // Reset if not flipped
		}
	}

	private void CheckWaterDeath()
	{
		if (transform.position.y < waterYThreshold)
		{
			GameMaster.Instance.DiedToWater();

			// Play splash sound
			if (splashSound != null) {
				AudioSource.PlayClipAtPoint(splashSound, transform.position);
			}

			// Inform GameMaster
			GameMaster.Instance?.CatchPlayer();

			// Optional: disable car movement or visuals
			rb.linearVelocity = Vector3.zero;
		}
	}
}
