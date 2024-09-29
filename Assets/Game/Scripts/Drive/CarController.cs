using CustomInspector;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// called from CarBrainAgent, controls the car
/// </summary>
public class CarController : MonoBehaviour, TrackGenerator.ITrackObserver
{
    #region serialized
    [Title("References")]
    [SerializeField, SelfFill] Rigidbody rb;
    [SerializeField, SelfFill] CarStatistics carStatistics;
    [SerializeField, ForceFill] Chassis chassis;


    [Title("Physics")]
    [Hook(nameof(ApplyCenterOfMass))]
    [SerializeField] Vector3 centerOfMass;
    [SerializeField, Min(0)] float acceleration = 500;
    [SerializeField, Min(0)] float brake = 2000;
    [SerializeField, Range(0, 90)] float maxSteeringAngle = 45;

    public float CurrentAcceleration => currentAcceleration;
    [SerializeField, ReadOnly, Range(-1, 1)] float currentAcceleration = 0;
    public float CurrentSteering => currentSteering;
    [SerializeField, ReadOnly, Range(-1, 1)] float currentSteering = 0;
    #endregion

    #region non-serialized

    public static List<CarController> allCars = new();

    Vector3 startPosition = Vector3.zero;
    Quaternion startRotation = Quaternion.identity;

    public List<ICarEvents> carEventsObs = new();

    float timeStuck = 0;
    #endregion

    #region drive input
    /// <summary>
    /// Range [-1, 1]
    /// </summary>
    public void SetAcceleration(float vInput)
    {
        Debug.Assert(vInput >= -1 && vInput <= 1, $"Value expected to be in range [-1, 1], but was '{vInput}'");
        currentAcceleration = vInput;

        float motorTorque = 0;
        float brakeTorque = 0;

        if (carStatistics.CurrentCompletion >= 1) //brake after reached finish line
            vInput = -1;

        if (vInput > 0)
        {
            motorTorque = acceleration * vInput;
        }
        else if (vInput < 0)
        {
            brakeTorque = brake * -vInput;
        }

        chassis.wheel_rearRight.WheelCollider.motorTorque = motorTorque;
        chassis.wheel_rearLeft.WheelCollider.motorTorque = motorTorque;

        chassis.wheel_frontRight.WheelCollider.brakeTorque = brakeTorque;
        chassis.wheel_frontLeft.WheelCollider.brakeTorque = brakeTorque;
        chassis.wheel_rearRight.WheelCollider.brakeTorque = brakeTorque;
        chassis.wheel_rearLeft.WheelCollider.brakeTorque = brakeTorque;

    }
    /// <summary>
    /// Range [-1, 1]
    /// </summary>
    public void SetSteering(float hInput)
    {
        Debug.Assert(hInput >= -1 && hInput <= 1, "Steering expected to be in range [-1, 1]");

        currentSteering = hInput;
        chassis.wheel_frontLeft.WheelCollider.steerAngle = maxSteeringAngle * currentSteering;
        chassis.wheel_frontRight.WheelCollider.steerAngle = maxSteeringAngle * currentSteering;
    }
    #endregion

    #region start end input
    public void RequestStartRun()
    {
        //disable wheel physics and drift-trails
        foreach (var wheel in chassis.AllWheels())
        {
            wheel.gameObject.SetActive(false);
        }

        //reset car
        this.transform.SetPositionAndRotation(startPosition, startRotation);
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        //activate wheel physics and drift-trails
        foreach (var wheel in chassis.AllWheels())
        {
            wheel.gameObject.SetActive(true);
            wheel.Trail.Clear();
        }

        foreach (ICarEvents obs in carEventsObs)
        {
            obs.OnRunStart();
        }
    }
    public void RequestEndRun(bool doNotLearn = false) //if end run is not agents fault
    {
        foreach (ICarEvents obs in carEventsObs)
        {
            obs.OnRunEnd(doNotLearn);
        }
    }
    #endregion

    #region unity events
    private void OnCollisionStay(Collision collision)
    {
        if (collision.collider.CompareTag("Floor")) //if touched grass or flipped around
        {
            timeStuck += Time.deltaTime;
            if (timeStuck > 3)
                RequestEndRun();
        }
    }
    private void OnCollisionExit(Collision collision)
    {
        if (collision.collider.CompareTag("Floor"))
            timeStuck = 0;
    }
    private void Start()
    {
        allCars.Add(this);
        TrackGenerator.Instance.trackObserver.Add(this);
        startPosition = this.transform.position;
        startRotation = this.transform.rotation;

        carEventsObs.Add(carStatistics);
        ApplyCenterOfMass();
    }
    private void OnDestroy()
    {
        allCars.Remove(this);
        TrackGenerator.Instance.trackObserver.Remove(this);
    }
    private void OnDrawGizmos()
    {
        Gizmos.DrawSphere(this.transform.TransformPoint(centerOfMass), .2f);
    }
    #endregion

    #region interfaces
    void TrackGenerator.ITrackObserver.OnTrackChanged()
    {
        RequestEndRun(true);
    }
    public interface ICarEvents
    {
        /// <summary>
        /// If car run ended
        /// <paramref name="doNotLearn"/>If we end, but it was not agents fault<paramref name="doNotLearn"/>
        /// </summary>
        abstract void OnRunEnd(bool doNotLearn);
        abstract void OnRunStart();
    }
    #endregion

    #region helpers
    void ApplyCenterOfMass() => rb.centerOfMass = centerOfMass;
    #endregion
}
