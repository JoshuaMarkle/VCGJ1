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

    [Header("Debug")]
    public float debugVecLength = 0.5f;

    private Vector2 moveInput;
    private Transform[] wheels;

    void Awake() {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start() {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = new Vector3(0, -0.5f, 0);
        wheels = new Transform[] { frontLeft, frontRight, rearLeft, rearRight };
    }

    void Update() {

		// Get input
        moveInput = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));

		// Turn the wheels
        frontLeft.localRotation = frontRight.localRotation =
            Quaternion.Lerp(frontLeft.localRotation,
                            Quaternion.Euler(0, maxSteerAngle * moveInput.x, 0),
                            Time.deltaTime * steerSpeed);
    }

    void FixedUpdate() {

		// Add Suspension + Friction
        foreach (Transform wheel in wheels) {
            ApplySuspension(wheel);
            ApplyLateralFriction(wheel);
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
}
