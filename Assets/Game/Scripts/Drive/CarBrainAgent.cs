using CustomInspector;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// controls the actual input, whether it is a keyboard or ai-input
/// </summary>
public class CarBrainAgent : Agent, CarController.ICarEvents, CarStatistics.ICompletionObserver
{
    [SerializeField, SelfFill] BehaviorParameters behaviorParameters;
    [SerializeField, SelfFill] CarController carController;
    [SerializeField, SelfFill] CarStatistics carStatistics;

    enum ControlMode { Human, Alg }
    [SerializeField] ControlMode controlMode = ControlMode.Human;

    #region Human Control
    //Human
    [ShowIfIs(nameof(controlMode), ControlMode.Human)]
    [SerializeField, ForceFill] InputActionReference accelerationInput;
    [ShowIfIs(nameof(controlMode), ControlMode.Human)]
    [SerializeField, ForceFill] InputActionReference steeringInput;

    [ShowIfIs(nameof(controlMode), ControlMode.Human)]
    [Tooltip("How fast the max maxSteeringAngle is reached.")]
    [SerializeField, Range(1, 6)] float steeringSmoothness = 3;

    [ShowIfIs(nameof(controlMode), ControlMode.Human)]
    [SerializeField, Min(.2f), Unit("/sec")]
    float steerSpeed = 5;

    float humanSteering;
    #endregion
    #region AI
    [SerializeField, Unit("sec")] int maxEpisodeDuration = 60;
    float episodeDuration;
    #endregion


    private void Awake()
    {
        carController.carEventsObs.Add(this);
        carStatistics.completionObservers.Add(this);
    }
    private void OnDestroy()
    {
        carController.carEventsObs.Remove(this);
        carStatistics.completionObservers.Remove(this);
    }
    private void FixedUpdate()
    {
        if (!behaviorParameters.IsInHeuristicMode()) // if not human controls the car (but ai)
        {
            episodeDuration += Time.deltaTime;
            if (episodeDuration >= maxEpisodeDuration) //1 minute
            {
                EndEpisode();
            }
        }
    }
    #region ml-agents
    public new void EndEpisode()
    {
        if (carStatistics.CurrentCompletion < 1) //if whole track not completed yet
        {
            //aktuelle Strecke noch geben
            AddReward(carStatistics.GetTrackPartPercent() * 10);
        }
        else
        {
            //zusätzliche belohnung, falls im Ziel
            if (maxEpisodeDuration > episodeDuration)
            {
                float usedTime_percent = episodeDuration / maxEpisodeDuration;
                float oneTrackReward = 10 * TrackGenerator.TracksGenerated;
                AddReward(((1 / usedTime_percent) - 1) * oneTrackReward); //he gets the same reward for whole track again, if he drives in half time
            }
        }

        base.EndEpisode();
    }
    public override void CollectObservations(VectorSensor sensor)
    {
        //5 observations
        sensor.AddObservation((float)carStatistics.GetOnLaneRotation());
        sensor.AddObservation((float)carStatistics.GetLaneXPos());
        sensor.AddObservation((float)carStatistics.CurrentSpeed);
        sensor.AddObservation((float)carStatistics.GetSideSlip()); //how much drifting
        sensor.AddObservation((float)carStatistics.GetTrackPartPercent()); //position on current trackpart
        //6 observations
        foreach (int trackPartType in carStatistics.GetNextTrackShapeIds(6)) //list of current and coming trackPartTypes
            sensor.AddObservation(trackPartType);
    }
    public override void OnEpisodeBegin()
    {
        episodeDuration = 0;
        carController.RequestStartRun();
        // Debug.Log("OnEpisodeBegin");
    }
    public override void OnActionReceived(ActionBuffers actions)
    {
        float acceleration = actions.ContinuousActions[0];
        float steering = actions.ContinuousActions[1];

        carController.SetAcceleration(acceleration);
        carController.SetSteering(steering);

        if (!behaviorParameters.IsInHeuristicMode()) //only for ai to help her
        {
            if (acceleration < 0 && carStatistics.CurrentSpeed < 0.001f)
            {
                AddReward(-10);
                EndEpisode();
            }
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut) // called to get input if not ai in controlling
    {
        float acceleration;
        float steering;

        if (controlMode == ControlMode.Human)
        {
            acceleration = (accelerationInput.action.ReadValue<float>());

            //steering
            float hInput = steeringInput.action.ReadValue<float>() * 0.75f;
            //weight small adjustments more
            hInput = Mathf.Sign(hInput) * Mathf.Pow(Mathf.Abs(hInput), steeringSmoothness);
            //smooth out movements
            if (hInput > humanSteering)
            {
                humanSteering += steerSpeed * Time.deltaTime;
                humanSteering = Mathf.Min(humanSteering, hInput);
            }
            else if (hInput < humanSteering)
            {
                humanSteering -= steerSpeed * Time.deltaTime;
                humanSteering = Mathf.Max(humanSteering, hInput);
            }
            steering = (humanSteering);
        }
        else if (controlMode == ControlMode.Alg)
        {
            //Hold speed of x kmh
            if (carStatistics.CurrentTrackPart)
            {
                //get infos
                IEnumerable<int> trackIds = carStatistics.GetNextTrackShapeIds(4);
                float trackPartPercent = carStatistics.GetTrackPartPercent();

                //calculate speed
                float targetSpeed = trackIds.Select(i => GetMaxSpeedPossible(i))
                                        .Select((speed, index) => speed + Mathf.Clamp01(index - (trackPartPercent * 1.5f - .33f)) * 50) //weight far away less
                                        .Min(); //take min speed

                static float GetMaxSpeedPossible(int trackShapeId) //in kmh
                {
                    return trackShapeId switch
                    {
                        0 => float.MaxValue, //long straight
                        1 => float.MaxValue, //short straight
                        -3 => 130, //left 45°
                        3 => 130, //right 45°
                        -4 => 90, //big left 90°
                        4 => 90, //big right 90°
                        -5 => 65, //small left 90°
                        5 => 65, //small right 90°
                        _ => throw new ArgumentException($"Unknown {nameof(trackShapeId)}: {trackShapeId}")
                    };
                }

                //Apply speed
                float curr_kmh = carStatistics.CurrentSpeed * 3.6f;
                float accelerationUC = (targetSpeed - curr_kmh) / 5;
                acceleration = (Mathf.Clamp(accelerationUC, -1, 1));
            }
            else
            {
                //drive until a trackpart is found
                acceleration = (1);
            }


            //steer
            if (carStatistics.CurrentTrackPart != null)
            {
                //get infos
                float curveRadius = carStatistics.CurrentTrackPart.WorldCurveRadius;
                float curveEndAngle = carStatistics.CurrentTrackPart.EndAngle;
                float onLaneRotation = carStatistics.GetOnLaneRotation();
                float laneXPos = carStatistics.GetLaneXPos();

                //calculate infos
                float straightSteering = curveRadius == 0 ? 0 : Mathf.Atan(8 / curveRadius) / (Mathf.PI / 2f); //the steering, at what the car should stay in center of street
                if (curveEndAngle < 0)
                    straightSteering *= -1;

                steering = Mathf.Clamp(straightSteering
                                            + -laneXPos * 1.75f //move to middle lane
                                            - onLaneRotation / 30f, //make car straight
                                            -1, 1);
            }
            else
                steering = 0;
        }
        else throw new NotImplementedException(controlMode.ToString());

        var continuousActions = actionsOut.ContinuousActions;
        continuousActions[0] = acceleration;
        continuousActions[1] = steering;
    }
    #endregion
    #region interfaces
    void CarStatistics.ICompletionObserver.OnEndReached() => EndEpisode();
    void CarController.ICarEvents.OnRunEnd(bool doNotLearn)
    {
        // AddReward(carStatistics.CurrentCompletion * (60 / carStatistics.CurrentSeconds));
        if (doNotLearn)
            EpisodeInterrupted();
        else
            EndEpisode();
    }
    void CarController.ICarEvents.OnRunStart() { } //this is called from onepisodebegin
    void CarStatistics.ICompletionObserver.OnNewTrackPartReached()
    {
        AddReward(10);
    }
    #endregion
}
