using CustomInspector;
using System.Collections.Generic;
using UnityEngine;

public class Chassis : MonoBehaviour
{
    [ForceFill] public WheelHandler wheel_frontLeft;
    [ForceFill] public WheelHandler wheel_frontRight;
    [ForceFill] public WheelHandler wheel_rearLeft;
    [ForceFill] public WheelHandler wheel_rearRight;

    public IEnumerable<WheelHandler> AllWheels()
    {
        yield return wheel_frontLeft;
        yield return wheel_frontRight;
        yield return wheel_rearLeft;
        yield return wheel_rearRight;
    }
}
