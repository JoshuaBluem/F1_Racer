using CustomInspector;
using System.Linq;
using TMPro;
using UnityEngine;

/// <summary>
/// controls additional ui during the "dark"-mode
/// </summary>
public class UpdateLightsOffInfos : MonoBehaviour, CarStatistics.ICompletionObserver, ToggleLightMode.LightsObserver
{
    // [SerializeField, Hook(nameof(SpectatedCarChanged), ExecutionTarget.IsPlaying)]
    CarStatistics SpectatedCar => CarBrainAgent.FirstCar.carStatistics;

    [SerializeField, Layer] int lightsOffLayer;

    [SerializeField, Tooltip("Current + How many future trackparts are known?"), Min(1)] int trackPartsShown = 5;
    [SerializeField, ForceFill] TMP_Text trackPartsInfo;
    [SerializeField, ForceFill] TMP_Text completion;
    [SerializeField, ForceFill] TMP_Text lanePosition;
    [SerializeField, ForceFill] TMP_Text onLaneRotation;
    [SerializeField, ForceFill] TMP_Text wheelsSideSlip;

    bool lightsOn;
    public bool LightsOn
    {
        set
        {
            if (!value)
                this.gameObject.SetActive(SpectatedCar != null);
            else
                this.gameObject.SetActive(false);
            lightsOn = value;
        }
    }

    private void Awake()
    {
        ToggleLightMode.lightsObservers.Add(this);
    }
    void Start()
    {
        SpectatedCarChanged(null, SpectatedCar);
    }
    private void OnDestroy()
    {
        ToggleLightMode.lightsObservers.Remove(this);
    }
    void SpectatedCarChanged(CarStatistics oldValue, CarStatistics newValue)
    {
        //only show infos if possible
        LightsOn = lightsOn;

        if (oldValue != null)
            oldValue.completionObservers.Remove(this);
        if (newValue != null)
            newValue.completionObservers.Add(this);
    }
    private void FixedUpdate()
    {
        /*        if ((Camera.main.cullingMask & (1 << lightsOffLayer)) != 0) //if cameraMain is rendering the lightsOffLayer
                {*/
        //Show Car-specific infos
        if (SpectatedCar.CurrentCompletion >= 1)
        {
            lanePosition.text = "FINISH LINE REACHED!";
            onLaneRotation.text = "FINISH LINE REACHED!";
            wheelsSideSlip.text = "FINISH LINE REACHED!";
            return;
        }

        //completion, position and rotation on lane
        TrackPart tp = SpectatedCar.CurrentTrackPart;
        if (tp != null)
        {
            completion.text = $"Current TrackPart: {SpectatedCar.GetTrackPartPercent():0.00} / 1";

            lanePosition.text = $"LanePosition X: {SpectatedCar.GetLaneXPos():0.00}";
            onLaneRotation.text = $"OnLaneRotation: {SpectatedCar.GetOnLaneRotation():00.0}°";
        }
        else
        {
            completion.text = "Current TrackPart: <unknown>";

            lanePosition.text = $"LanePosition X: <unknown>";
            onLaneRotation.text = $"OnLaneRotation: <unknown>";
        }


        //slip
        wheelsSideSlip.text = $"Wheels SideSlip (Drift): {SpectatedCar.GetSideSlip():0.0}";
        //}
    }
    public void OnNewTrackPartReached()
    {
        var trackParts = SpectatedCar.GetNextTrackShapeIds(trackPartsShown).Select<int, string>(p => p.ToString());
        trackPartsInfo.text = "Next TrackParts: " + string.Join(", ", trackParts) + ", ..."; //always starts on a straight line, thats why "1," is added
    }

    void CarStatistics.ICompletionObserver.OnEndReached()
    {

    }
}
