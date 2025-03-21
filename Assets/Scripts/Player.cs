using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    public static Player Instance;
    private Rigidbody rb;

    public Transform frontLeft, frontRight, rearLeft, rearRight;
    public bool frontWheelDrive = true, rearWheelDrive = true;

    public float restLength = 0.5f, springStrength = 3000f, dampingStrength = 800f;
    public float maxSlipSpeed = 1f, lateralFrictionMultiplier = 5f, driveForce = 2000f, brakeForce = 4000f, topSpeed = 15f;
    public float maxSteerAngle = 15f, steerSpeed = 3f;
    public float debugVecLength = 0.5f;
    public AnimationCurve torqueCurve = AnimationCurve.Linear(0, 1, 1, 1);
    public AnimationCurve tractionCurve = AnimationCurve.EaseInOut(0, 1, 0.2f, 1);

    private Vector2 moveInput;
    private Transform[] wheels;

    void Awake()
    {
        if (Instance == null) Instance = this; else Destroy(gameObject);
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = new Vector3(0, -0.5f, 0);
        wheels = new Transform[] { frontLeft, frontRight, rearLeft, rearRight };
    }

    void Update()
    {
        moveInput = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        frontLeft.localRotation = frontRight.localRotation = Quaternion.Lerp(frontLeft.localRotation,
            Quaternion.Euler(0, maxSteerAngle * moveInput.x, 0), Time.deltaTime * steerSpeed);
    }

    void FixedUpdate()
    {
        foreach (Transform wheel in wheels) {
            ApplySuspension(wheel);
            ApplyLateralFriction(wheel);
        }

        if (frontWheelDrive) { ApplyDriveForce(frontLeft); ApplyDriveForce(frontRight); }
        if (rearWheelDrive) { ApplyDriveForce(rearLeft); ApplyDriveForce(rearRight); }
    }

    void ApplySuspension(Transform wheel)
    {
        if (Physics.Raycast(wheel.position, -wheel.up, out RaycastHit hit, restLength * 2))
        {
            float offset = restLength - hit.distance;
            float vel = Vector3.Dot(rb.GetPointVelocity(wheel.position), wheel.up);
            Vector3 force = (offset * springStrength - vel * dampingStrength) * wheel.up;
            rb.AddForceAtPosition(force, wheel.position);
            Debug.DrawRay(wheel.position, force * debugVecLength, Color.green);
        }
    }

    void ApplyLateralFriction(Transform wheel)
    {
        Vector3 vel = rb.GetPointVelocity(wheel.position);
        float lateralSpeed = Vector3.Dot(vel, wheel.right);
        float slipPercent = Mathf.Clamp01(Mathf.Abs(lateralSpeed) / maxSlipSpeed);
        Vector3 frictionForce = (-lateralSpeed / Time.fixedDeltaTime) * wheel.right * tractionCurve.Evaluate(slipPercent) * lateralFrictionMultiplier;
        rb.AddForceAtPosition(frictionForce, wheel.position);
        Debug.DrawRay(wheel.position, frictionForce * debugVecLength, Color.red);
    }

    void ApplyDriveForce(Transform wheel)
    {
        Vector3 forward = wheel.forward;
        float speedPercent = Mathf.Clamp01(Vector3.Dot(rb.linearVelocity, forward) / topSpeed);
        Vector3 force = forward * moveInput.y * driveForce * torqueCurve.Evaluate(speedPercent);
        rb.AddForceAtPosition(force, wheel.position);
        Debug.DrawRay(wheel.position, force * debugVecLength, Color.blue);

        if (moveInput.y == 0)
        {
            Vector3 brake = -forward * Vector3.Dot(rb.linearVelocity, forward) * brakeForce;
            rb.AddForceAtPosition(brake, wheel.position);
            Debug.DrawRay(wheel.position, brake * debugVecLength, Color.cyan);
        }
    }
}
