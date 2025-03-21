using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    public static Player Instance;
    private Rigidbody rb;

    [Header("Wheel Transforms")]
    public Transform frontLeft;
	public Transform frontRight;
	public Transform rearLeft;
	public Transform rearRight;

    [Header("Drive Settings")]
    public bool frontWheelDrive = true;
    public bool rearWheelDrive = true;

    [Header("Suspension Settings")]
    public float restLength = 0.5f;
    public float springStrength = 3000f;
    public float dampingStrength = 800f;

    [Header("Friction & Driving Settings")]
    public float maxSlipSpeed = 1f;
    public float lateralFrictionMultiplier = 5f;
    public float driveForce = 2000f;
    public float brakeForce = 4000f;
    public float topSpeed = 15f;

    [Header("Steering")]
    public float maxSteerAngle = 15f;
    public float steerSpeed = 3f;

    [Header("Curves")]
    public AnimationCurve torqueCurve = AnimationCurve.Linear(0, 1, 1, 1);
    public AnimationCurve tractionCurve = AnimationCurve.EaseInOut(0, 1, 0.2f, 1);

    [Header("Particles")]
    public Gradient trailGradient;
    public TrailRenderer[] wheelTrails;
    public ParticleSystem[] wheelSmokes;

    [Header("Debug")]
    public float debugVecLength = 0.5f;

    private Vector2 moveInput;
	private bool recover;

    private Transform[] wheels;

    void Awake() {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start() {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = new Vector3(0, -0.5f, 0);
        wheels = new Transform[] { frontLeft, frontRight, rearLeft, rearRight };
        trailGradient = new Gradient();

        wheelTrails = new TrailRenderer[wheels.Length];
        wheelSmokes = new ParticleSystem[wheels.Length];

        for (int i = 0; i < wheels.Length; i++) {
            wheelTrails[i] = wheels[i].GetComponentInChildren<TrailRenderer>();
            wheelSmokes[i] = wheels[i].GetComponentInChildren<ParticleSystem>();
        }
    }

    void Update() {

		// Get move input
        moveInput = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));

		// Turn the wheels
        frontLeft.localRotation = frontRight.localRotation =
            Quaternion.Lerp(frontLeft.localRotation,
                            Quaternion.Euler(0, maxSteerAngle * moveInput.x, 0),
                            Time.deltaTime * steerSpeed);

		// Recover car
		recover = Input.GetKeyDown(KeyCode.R);
		if (recover)
			RecoverIfFlipped();
    }

    void FixedUpdate() {

        // Update wheels
        for (int i = 0; i < wheels.Length; i++) {
            Transform wheel = wheels[i];
            ApplySuspension(wheel);
            ApplyLateralFriction(wheel);

            // Effects
            HandleDriftEffects(wheel, wheelTrails[i], wheelSmokes[i]);
        }

        // Front wheel drive
        if (frontWheelDrive) {
            ApplyDriveForce(frontLeft);
            ApplyDriveForce(frontRight);
        }

        // Rear wheel drive
        if (rearWheelDrive) {
            ApplyDriveForce(rearLeft);
            ApplyDriveForce(rearRight);
        }
    }

	// Suspension
    void ApplySuspension(Transform wheel) {

		// Point ray down from wheel (check if hit ground)
        if (Physics.Raycast(wheel.position, -wheel.up, out RaycastHit hit, restLength * 2)) {

			// Calculate suspension force
            float offset = restLength - hit.distance;
            float vel = Vector3.Dot(rb.GetPointVelocity(wheel.position), wheel.up);
            Vector3 force = (offset * springStrength - vel * dampingStrength) * wheel.up;

			// Add suspension force to rigidbody
            rb.AddForceAtPosition(force, wheel.position);
            Debug.DrawRay(wheel.position, force * debugVecLength, Color.green);
        }
    }

	// Lateral Friction
    void ApplyLateralFriction(Transform wheel) {

        Vector3 vel = rb.GetPointVelocity(wheel.position);
        float lateralSpeed = Vector3.Dot(vel, wheel.right);
        float slipPercent = Mathf.Clamp01(Mathf.Abs(lateralSpeed) / maxSlipSpeed);

		// Calculate friction force (perpendicular to the wheel)
        Vector3 frictionForce = (-lateralSpeed / Time.fixedDeltaTime) * wheel.right *
                                tractionCurve.Evaluate(slipPercent) * lateralFrictionMultiplier;

		// Add friction force to rigidbody
        rb.AddForceAtPosition(frictionForce, wheel.position);
        Debug.DrawRay(wheel.position, frictionForce * debugVecLength, Color.red);
    }

	// Drive Forward
    void ApplyDriveForce(Transform wheel) {

		// Calculate drive force
        Vector3 forward = wheel.forward;
        float speedPercent = Mathf.Clamp01(Vector3.Dot(rb.linearVelocity, forward) / topSpeed);
        Vector3 force = forward * moveInput.y * driveForce * torqueCurve.Evaluate(speedPercent);

		// Add drive force to rigidbody
        rb.AddForceAtPosition(force, wheel.position);
        Debug.DrawRay(wheel.position, force * debugVecLength, Color.blue);

		// Brake car if not actively moving
        if (moveInput.y == 0) {
            Vector3 brake = -forward * Vector3.Dot(rb.linearVelocity, forward) * brakeForce;
            rb.AddForceAtPosition(brake, wheel.position);
            Debug.DrawRay(wheel.position, brake * debugVecLength, Color.cyan);
        }
    }

	// Flip car back over
	public void RecoverIfFlipped() {
		// Check if the car is upside down
		if (Vector3.Dot(transform.up, Vector3.up) < 0.1f && rb.linearVelocity.magnitude < 1f)
		{
			// Reset rotation: keep yaw (Y axis) but reset pitch and roll
			Vector3 euler = transform.eulerAngles;
			transform.rotation = Quaternion.Euler(0, euler.y, 0);

			// Slightly raise the car to prevent collision with the ground
			transform.position += Vector3.up * 1.5f;

			// Reset velocity
			rb.linearVelocity = Vector3.zero;
			rb.angularVelocity = Vector3.zero;
		}
	}

    // Drift Particles
    private void HandleDriftEffects(Transform wheel, TrailRenderer trail, ParticleSystem smoke)
    {
        Vector3 velocity = rb.GetPointVelocity(wheel.position);
        float lateralSpeed = Vector3.Dot(velocity, wheel.right);

        float slipPercent = Mathf.Clamp01(Mathf.Abs(lateralSpeed) / maxSlipSpeed);

        // Enable trail and smoke only when drifting significantly
        bool isDrifting = slipPercent > 0.3f;

        // Trail logic
        if (trail != null)
        {
            if (isDrifting && !trail.emitting)
                trail.emitting = true;
            else if (!isDrifting && trail.emitting)
                trail.emitting = false;

            // Set trail alpha based on drift amount
            trailGradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0.0f) },
                new GradientAlphaKey[] { new GradientAlphaKey(slipPercent, 0.0f), new GradientAlphaKey(0, 1.0f) }
            );
            trail.colorGradient = trailGradient;
        }

        // Smoke logic
        if (smoke != null)
        {
            var emission = smoke.emission;
            emission.rateOverTime = slipPercent * 50f;

            if (isDrifting && !smoke.isPlaying)
                smoke.Play();
            else if (!isDrifting && smoke.isPlaying)
                smoke.Stop();
        }
    }
}
