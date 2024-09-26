using CustomInspector;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Collects keyboard and mouse input, that belong to ui elements and forwards the input
/// </summary>
public class UIHandler : MonoBehaviour
{
    [SerializeField, ForceFill] InputActionReference newTrackR;
    [SerializeField, ForceFill] InputActionReference resetCarsR;

    [SerializeField, ForceFill] ToggleLightMode toggleLightMode;
    [SerializeField, ForceFill] InputActionReference toggleLightR;

    [SerializeField, ForceFill] TMP_Text lightsButton;
    [SerializeField, ForceFill] TMP_Text graphicsButton;

    [SerializeField, ForceFill] GameObject additionalGraphics = null;

    public void NewTrackButton()
    {
        TrackGenerator.Instance.RegenerateTrack();
    }
    public void ResetCarsButton()
    {
        foreach (CarController car in CarController.allCars)
        {
            car.RequestEndRun();
        }
    }
    private void OnEnable()
    {
        newTrackR.action.started += Input_NewTrack;
        resetCarsR.action.started += Input_ResetCars;
        toggleLightR.action.started += Input_ToggleLightMode;
    }
    private void OnDisable()
    {
        newTrackR.action.started -= Input_NewTrack;
        resetCarsR.action.started -= Input_ResetCars;
        toggleLightR.action.started -= Input_ToggleLightMode;
    }
    void Input_NewTrack(InputAction.CallbackContext context) => NewTrackButton();
    void Input_ResetCars(InputAction.CallbackContext context) => ResetCarsButton();
    void Input_ToggleLightMode(InputAction.CallbackContext context) => ToggleLightMode();

    private void Awake()
    {
        UpdateLightsButton();
    }
    public void ToggleLightMode()
    {
        toggleLightMode.ToggleLights();
        UpdateLightsButton();
    }
    void UpdateLightsButton()
    {
        if (toggleLightMode.LightsOn)
            lightsButton.text = "Lights: On";
        else
            lightsButton.text = "Lights: Off";

        if (graphicsOn != toggleLightMode.LightsOn)
            ToggleGraphicsMode();
    }
    bool graphicsOn = true;
    public void ToggleGraphicsMode()
    {
        graphicsOn ^= true;

        if (graphicsOn)
            graphicsButton.text = "Graphics: On";
        else
            graphicsButton.text = "Graphics: Off";


        additionalGraphics.SetActive(graphicsOn);
    }
}
