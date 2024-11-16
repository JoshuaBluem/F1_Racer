using CustomInspector;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

/// <summary>
/// generates a random track by placing random trackparts. Also calls trees to be spawned
/// </summary>
public class TrackGenerator : MonoBehaviour
{
    public static TrackGenerator Instance { get; private set; }
    //Serialized

    [Title("References")]

    [SerializeField, Min(2), //minimum a start and an end
    Unit("Parts")]
    public int trackLength = 50;

    [SerializeField, ForceFill] GameObject[] trackParts;
    [SerializeField, ForceFill] GameObject finishLine;
    [SerializeField, Layer] int trackLayer;

    [Tooltip("The position where the first track-part is generated")]
    [SerializeField, ForceFill] TrackPart startTrackPart;
    [SerializeField] TreesSpawner treesSpawner;

    [Title("Regeneration")]
    [ShowIf(StaticConditions.IsPlaying), Button(nameof(RegenerateTrack))]
    [SerializeField, HideField] bool _;

    [SerializeField, Tooltip("Automatically regenerate the track over time"), Hook(nameof(OnTrackRegenerationChanged), ExecutionTarget.IsPlaying)]
    bool autoRegenerateTrack = false;
    [ShowIf(nameof(autoRegenerateTrack))]
    [SerializeField, Tooltip("Seconds until the track regenerates"), Unit("sec"), Min(60)]
    int trackLifeTime = 10 * 60;
    [ShowIf(BoolOperator.And, nameof(autoRegenerateTrack), StaticConditions.IsPlaying)]
    [SerializeField, Tooltip("Remaining seconds until the track regenerates"), Min(0)] float remRegSeconds;
    [ShowIf(nameof(autoRegenerateTrack))]
    [SerializeField] TMP_Text newTrackInText;

    //NonSerialized
    public static int TracksGenerated { get; private set; } = 0;

    [Tooltip("The position the next track-part is generated"), ReadOnly]
    Vector3 nextPosition = Vector3.zero;
    [Tooltip("The angle in which next track-part is generated"), ReadOnly]
    float nextDirection = 0;
    Quaternion NextRotation => Quaternion.Euler(0, nextDirection, 0);
    public IReadOnlyList<TrackPart> GeneratedTrackParts => generatedTrackParts;
    readonly List<TrackPart> generatedTrackParts = new();
    readonly List<Vector3> savedTreePositions = new();
    public readonly List<ITrackObserver> trackObserver = new();

    Coroutine trackRegeneration = null;




    private void Awake()
    {
        Debug.Assert(Instance == null, "Multiple Instances of " + nameof(TrackGenerator));
        Instance = this;
    }
    private void Start()
    {
        RegenerateTrack();
    }
    private void OnEnable() => OnTrackRegenerationChanged(false, autoRegenerateTrack);
    void OnTrackRegenerationChanged(bool oldValue, bool newValue)
    {
        newTrackInText.gameObject.SetActive(newValue);

        if (newValue)
        {
            if (trackRegeneration != null)
                StopCoroutine(trackRegeneration);
            trackRegeneration = StartCoroutine(Regeneration());

            IEnumerator Regeneration()
            {
                yield return null; //wait for start to execute so 'remRegSeconds' is initialized
                yield return null;

                while (true)
                {
                    while (remRegSeconds > 0)
                    {
                        newTrackInText.text = $"New Track in: {Helpers.TimeString(remRegSeconds)}";
                        remRegSeconds -= Time.deltaTime;
                        yield return null;
                    }
                    RegenerateTrack();
                }
            }
        }
        else
        {
            if (trackRegeneration != null)
            {
                StopCoroutine(trackRegeneration);
                trackRegeneration = null;
            }
        }
    }
    public void RegenerateTrack()
    {
        //Delete old
        if (generatedTrackParts.Any()) //at ApplicationStart there are no track parts (not even the startPart)
        {
            generatedTrackParts.RemoveAt(0); //do not destroy the start
            foreach (var trackPart in generatedTrackParts)
            {
                Destroy(trackPart.gameObject);
            }
            generatedTrackParts.Clear();
        }
        generatedTrackParts.Add(startTrackPart);

        treesSpawner.DestroyAllTrees();
        savedTreePositions.Clear();

        nextPosition = startTrackPart.CurrentWorldEndPosition;
        nextDirection = 0;


        //Create new track parts
        for (int i = 0; i < trackLength - 2; i++) //-2 because start is already there and finish line gets added
        {
            AddSingleTrackPart(i + 1);
        }
        TrackPart finishLine_Instance = Instantiate(finishLine, nextPosition, NextRotation, this.transform).GetComponent<TrackPart>();
        generatedTrackParts.Add(finishLine_Instance);
        finishLine_Instance.SetTrackNumber(trackLength - 1);

        TracksGenerated = trackLength; //+ finish Line

        //Spawn new trees
        treesSpawner.SpawnTrees(treesSpawner.startSpawnPosition);

        foreach (Vector3 position in savedTreePositions)
        {
            treesSpawner.SpawnTrees(position);
        }

        //inform about regeneration
        foreach (ITrackObserver obs in trackObserver)
        {
            obs.OnTrackChanged();
        }
        remRegSeconds = trackLifeTime;
    }
    public void AddSingleTrackPart(int trackNumber)
    {
        GameObject newTrackPart = Instantiate(trackParts[Random.Range(0, trackParts.Length)],
                                              nextPosition, NextRotation, this.transform);
        TrackPart tp = newTrackPart.GetComponent<TrackPart>();
        //Straighten track: Never turn too much left or too much right
        if (nextDirection + tp.EndAngle < -90
            || nextDirection + tp.EndAngle > 90)
        {
            Destroy(newTrackPart);
            AddSingleTrackPart(trackNumber);
            return;
        }

        tp.SetTrackNumber(trackNumber);
        generatedTrackParts.Add(tp);
        nextPosition = tp.CurrentWorldEndPosition;
        nextDirection += tp.EndAngle;

        //Save tree position
        savedTreePositions.Add(nextPosition);
    }
    private void OnDrawGizmosSelected()
    {
        //Position
        Gizmos.DrawSphere(nextPosition, .2f);
        //Direction
        Gizmos.DrawLine(nextPosition, nextPosition + this.transform.TransformDirection(NextRotation * Vector3.forward));
    }
    public interface ITrackObserver
    {
        abstract void OnTrackChanged();
    }
}
