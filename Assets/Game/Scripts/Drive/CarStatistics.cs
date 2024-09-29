using CustomInspector;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// holds all information about the car. Can also pass informations
/// </summary>
public class CarStatistics : MonoBehaviour, CarController.ICarEvents
{
    [SerializeField, SelfFill] Rigidbody rb;
    [SerializeField, SelfFill] CarUIUpdater uiUpdater;
    [SerializeField, ForceFill] Chassis chassis;

    public TrackPart CurrentTrackPart => currentTrackPart;
    [SerializeField, ReadOnly] TrackPart currentTrackPart;

    public float CurrentSeconds { get; private set; }
    /// <summary>
    /// in meter per seconds
    /// </summary>
    public float CurrentSpeed => rb.velocity.magnitude;
    /// <summary>
    /// Total completion of whole track: Range [0,1]
    /// </summary>
    public float CurrentCompletion { get; private set; } = 0;

    Coroutine timeC;
    Coroutine requesstingEndC;

    public List<ICompletionObserver> completionObservers = new();

    #region get informations
    /// <summary>
    /// Percentage how much the car completed the current TrackPart
    /// </summary>
    /// <returns></returns>
    public float GetTrackPartPercent()
    {
        if (currentTrackPart == null)
            return 0;

        Vector3 targetDir = currentTrackPart.CurrentWorldEndPosition - currentTrackPart.transform.position;
        Vector3 currDir = currentTrackPart.CurrentWorldEndPosition - this.transform.position;
        return 1 - Mathf.Clamp01(currDir.magnitude / targetDir.magnitude);
    }
    /// <summary>
    /// Gives information, how much space to the right and left of the lane is. Range [-1, 1].
    /// </summary>
    /// <returns></returns>
    public float GetLaneXPos()
    {
        if (currentTrackPart == null)
            return 0;


        if (currentTrackPart.EndAngle == 0)
        {
            return currentTrackPart.transform.InverseTransformPoint(this.transform.position).x / (TrackPart.localTrackWidth / 2f);
        }
        else
        {
            float pivotDistance = (currentTrackPart.CurrentWorldCurvePivot - this.transform.position).magnitude;
            return (currentTrackPart.WorldCurveRadius - pivotDistance) / (currentTrackPart.TrackWidth / 2f);
        }
    }
    /// <summary>
    /// Relative rotation to direction of street
    /// </summary>
    /// <returns></returns>
    public float GetOnLaneRotation()
    {
        if (currentTrackPart == null)
            return 0;

        float onTrackPartRotation = (Quaternion.Inverse(currentTrackPart.transform.rotation) * this.transform.rotation).eulerAngles.y;

        float angle = onTrackPartRotation - GetTrackPartPercent() * currentTrackPart.EndAngle;
        if (angle > 180)
            return angle - 360;
        else
            return angle;
    }
    /// <summary>
    /// The shape ids of current and next trackParts
    /// </summary>
    public IEnumerable<int> GetNextTrackShapeIds(int amount)
    {
        if (amount < 1)
            return new int[0];

        return TrackGenerator.Instance.GeneratedTrackParts
                        .Select<TrackPart, int>(p => p.Shape_id)
                        .Concat(Enumerable.Repeat(0, count: amount)) // padding so that there are always minimum amount
                        .Skip(CurrentTrackPart != null ? CurrentTrackPart.TrackNumber : 0) // remove parts, that we already driven over
                        .Take(amount); // cap too far away parts
    }
    /// <summary>
    /// Shows, how much force makes the car drift
    /// </summary>
    public float GetSideSlip()
    {
        float sum = 0;
        foreach (WheelHandler wheel in chassis.AllWheels())
        {
            WheelHit wheelHit = wheel.LastGroundHit;
            sum += Mathf.Abs(wheelHit.sidewaysSlip);
        }
        return sum;
    }
    #endregion


    public void StopTime()
    {
        if (timeC != null)
            StopCoroutine(timeC);
    }

    #region unity events
    private void FixedUpdate()
    {
        //Speed
        uiUpdater.DisplaySpeed(CurrentSpeed);
        //Completion
        if (currentTrackPart != null)
        {
            float startPercent = (float)currentTrackPart.TrackNumber / (float)(TrackGenerator.TracksGenerated - 1); //-1 because last part (finish line) is already finish
            float endPercent = (float)(currentTrackPart.TrackNumber + 1f) / (float)(TrackGenerator.TracksGenerated - 1);

            CurrentCompletion = Mathf.Lerp(startPercent, endPercent, GetTrackPartPercent());
            uiUpdater.SetCompletion(CurrentCompletion);
        }
    }
    private void OnEnable()
    {
        timeC = StartCoroutine(UpdateTimer());
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out TrackPart tp))
        {
            currentTrackPart = tp;
            foreach (var obs in completionObservers)
                obs.OnNewTrackPartReached();
        }
        else if (other.TryGetComponent(out FinishLine _))
        {
            //reached finish line
            ShowHighscores.Instance.AddScore(CurrentSeconds);
            currentTrackPart = null;
            StopTime();
            CurrentCompletion = 1;
            uiUpdater.SetCompletion(1);

            foreach (var obs in completionObservers)
                obs.OnNewTrackPartReached();

            if (requesstingEndC != null)
                StopCoroutine(requesstingEndC);
            requesstingEndC = StartCoroutine(W());
            IEnumerator W()
            {
                yield return new WaitForSeconds(3);
                foreach (var obs in completionObservers)
                    obs.OnEndReached();
            }
        }
    }
    #endregion



    #region interfaces
    void CarController.ICarEvents.OnRunStart()
    {
        currentTrackPart = null;
        CurrentCompletion = 0;
        uiUpdater.SetCompletion(0);
        if (!this.enabled)
        {
            Debug.LogError("Could not start timer, due to object not being active");
            return;
        }
        if (timeC != null)
            StopCoroutine(timeC);
        timeC = StartCoroutine(UpdateTimer());
    }
    void CarController.ICarEvents.OnRunEnd(bool doNotLearn)
    {
        if (requesstingEndC != null)
            StopCoroutine(requesstingEndC);
    }
    public interface ICompletionObserver
    {
        abstract void OnNewTrackPartReached();
        abstract void OnEndReached();
    }
    #endregion
    IEnumerator UpdateTimer()
    {
        CurrentSeconds = 0;
        uiUpdater.DisplayTime(CurrentSeconds);

        //wait until starts to drive
        while (Mathf.Abs(rb.velocity.x) + Mathf.Abs(rb.velocity.z) <= 0.01)
        {
            yield return null;
        }
        //count seconds
        while (true)
        {
            yield return null;
            CurrentSeconds += Time.deltaTime;
            uiUpdater.DisplayTime(CurrentSeconds);
        }
    }
}
