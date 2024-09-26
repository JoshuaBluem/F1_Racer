using CustomInspector;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// toggles the "dark"-mode in game
/// </summary>
public class ToggleLightMode : MonoBehaviour
{
    [SerializeField, SelfFill] Camera _camera;


    [SerializeField] LayerMask lightsOnMask = int.MaxValue;
    [SerializeField] LayerMask lightsOffMask = 0;

    [SerializeField, ReadOnly] bool lightsOn = true;

    public static List<LightsObserver> lightsObservers = new();

    public bool LightsOn
    {
        get => lightsOn;
        set
        {
            if (value == lightsOn)
                return;
            else
            {
                lightsOn = value;
                if (value)
                {
                    _camera.cullingMask = lightsOnMask;
                    _camera.clearFlags = CameraClearFlags.Skybox;
                }
                else
                {
                    _camera.cullingMask = lightsOffMask;
                    _camera.clearFlags = CameraClearFlags.SolidColor;
                }

                foreach (var item in lightsObservers)
                {
                    item.LightsOn = value;
                }
            }
        }
    }

    [Button(nameof(ToggleLights))]
    [SerializeField, HideField] bool _;
    public void ToggleLights() => LightsOn ^= true;

    private void Start()
    {
        //trigger to set lights on
        lightsOn = false;
        LightsOn = true;
    }

    public interface LightsObserver
    {
        public bool LightsOn { set; }
    }
}
