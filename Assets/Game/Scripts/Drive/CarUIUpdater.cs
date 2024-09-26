using CustomInspector;
using TMPro;
using UnityEngine;

/// <summary>
/// controls the ui in the car cockpit
/// </summary>
public class CarUIUpdater : MonoBehaviour
{
    [SerializeField, ForceFill] TMP_Text speedText;
    [SerializeField, ForceFill] TMP_Text timeText;
    [SerializeField, ForceFill] TMP_Text completionText;

    public void DisplaySpeed(float meterPerSeconds)
    {
        speedText.text = $"{(int)(meterPerSeconds * 3.6f)} km/h";
    }
    public void DisplayTime(float seconds)
    {
        timeText.text = Helpers.TimeString(seconds);
    }
    public void SetCompletion(float percent)
    {
        Debug.Assert(percent >= 0 && percent <= 1);
        completionText.text = $"{(int)(percent * 100)}%";
    }
}
