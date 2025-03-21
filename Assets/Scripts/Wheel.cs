using UnityEngine;

public class Wheel : MonoBehaviour
{
    [Header("Wheel Setup")]
    public WheelCollider wheelCollider;
    public Transform wheelTransform;
    public ParticleSystem driftSmoke;
    public bool isSteeringWheel;
    public bool isDrivingWheel;

    [Header("Drifting")]
    public float driftSlipThreshold = 0.2f;
    public float smokeRateMultiplier = 300f;

    private int driftIndex = -1;

    public void ApplySteering(float steerAngle)
    {
        if (isSteeringWheel)
            wheelCollider.steerAngle = steerAngle;
    }

    public void ApplyThrottle(float motorTorque)
    {
        if (isDrivingWheel)
        {
            wheelCollider.motorTorque = motorTorque;
            wheelCollider.brakeTorque = 0f;
        }
    }

    public void ApplyBrake(float brakeForce)
    {
        if (isDrivingWheel)
        {
            wheelCollider.motorTorque = 0f;
            wheelCollider.brakeTorque = brakeForce;
        }
    }

    public void UpdateDrift()
    {
        if (wheelCollider.GetGroundHit(out WheelHit hit))
        {
            float slip = Mathf.Abs(hit.sidewaysSlip);
            var emission = driftSmoke.emission;

            if (slip > driftSlipThreshold)
            {
                emission.rateOverTime = slip * smokeRateMultiplier;
                if (!driftSmoke.isPlaying) driftSmoke.Play();
            }
            else
            {
                emission.rateOverTime = 0f;
                if (driftSmoke.isPlaying) driftSmoke.Stop();
            }
        }
    }

    public void UpdateVisual()
    {
        wheelCollider.GetWorldPose(out Vector3 pos, out Quaternion rot);
        wheelTransform.position = pos;
        wheelTransform.rotation = rot;
    }
}
