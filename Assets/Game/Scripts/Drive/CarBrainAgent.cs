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
    [SelfFill] public BehaviorParameters behaviorParameters;
    [SerializeField, SelfFill] CarController carController;
    [SerializeField, SelfFill] public CarStatistics carStatistics;

    public enum ControlMode { Human, Alg }
    public ControlMode controlMode = ControlMode.Human;

    [Tooltip("The first car instantiated and the one we are spectating with the camera")]
    public static CarBrainAgent FirstCar { get; private set; }

    RecordEpisodes recorder = new(activeAtStart: false);
    public bool Record
    {
        get => recorder.Active;
        set => recorder.Active = value;
    }

    #region Human Control
    //Human
    [ShowIfIs(nameof(controlMode), ControlMode.Human)]
    [SerializeField, ForceFill] InputActionReference accelerationInput;
    [ShowIfIs(nameof(controlMode), ControlMode.Human)]
    [SerializeField, ForceFill] InputActionReference steeringInput;

    [ShowIfIs(nameof(controlMode), ControlMode.Human)]
    [Tooltip("How sensitive the gamepad/controller is for small movements.\n1: 50% movement on gamepad means 50% wheel turn\nInfinity: Small gamepad movements count almost nothing. (Amlost like a high controller dead-zone)")]
    [SerializeField, Range(1, 6)] float steeringSmoothness = 3;

    [ShowIfIs(nameof(controlMode), ControlMode.Human)]
    [Tooltip("How fast the max maxSteeringAngle is reached.")]
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
        FirstCar = this;
    }
    private void OnDestroy()
    {
        carController.carEventsObs.Remove(this);
        carStatistics.completionObservers.Remove(this);

        recorder.EndEpisode();
    }
    private void FixedUpdate()
    {
        episodeDuration += Time.deltaTime;
        if (!behaviorParameters.IsInHeuristicMode()) // if not human controls the car (but ai)
        {
            if (episodeDuration >= maxEpisodeDuration) //1 minute
            {
                EndEpisode();
            }
        }
    }
    #region ml-agents
    public new void EndEpisode()
    {
        if (carStatistics.CurrentCompletion < 1) // if whole track not completed yet
        {
            // give reward proportionally on current track
            AddReward(carStatistics.GetTrackPartPercent() * 10);
        }
        else // whole track was completed
        {
            // additional reward, if there is time remaining
            if (maxEpisodeDuration > episodeDuration)
            {
                float usedTime_percent = episodeDuration / maxEpisodeDuration;
                float oneTrackReward = 10 * TrackGenerator.TracksGenerated;
                if (usedTime_percent < 0.1)
					throw new Exception("No valid time captured when reaching end.\nIt's not realistic to complete the whole track in unter a second");
                // he gets the same reward for whole track again, if he drives in half time
                AddReward(((1 / usedTime_percent) - 1) * oneTrackReward);
            }
        }

        recorder.EndEpisode();
        base.EndEpisode();
    }
    public new void AddReward(float value)
    {
        recorder.AddReward(value);
        base.AddReward(value);
    }
    public new void EpisodeInterrupted()
    {
        recorder.EpisodeInterrupted();
        base.EpisodeInterrupted();
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
    public override void OnEpisodeBegin()
    {
        Debug.Log("OnEpisodeBegin");
        episodeDuration = 0;
        carController.RequestStartRun();
        recorder.StartEpisode();
    }

    public override void Heuristic(in ActionBuffers actionsOut) // called to get input if not ai in controlling
    {
        float acceleration;
        float steering;

        if (controlMode == ControlMode.Human)
        {
            acceleration = (accelerationInput.action.ReadValue<float>());

            //steering
            float hInput = steeringInput.action.ReadValue<float>();
            //weight small adjustments more
            hInput = Mathf.Sign(hInput) * Mathf.Pow(Mathf.Abs(hInput), steeringSmoothness);

            if (hInput == 0)
            {
                //reset of steering to straigth should always happen immediate
                humanSteering = hInput;
            }
            else
            {
                //smooth out movements to right and left
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
            }

            steering = humanSteering;
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
