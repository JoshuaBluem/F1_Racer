using CustomInspector;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// highscore table in the ui
/// </summary>
public class ShowHighscores : MonoBehaviour, TrackGenerator.ITrackObserver
{
    public static ShowHighscores Instance { get; private set; }

    TrackGenerator trackGenerator => TrackGenerator.Instance;
    [SerializeField, ForceFill] TMP_Text headlineText = null;
    [SerializeField, ForceFill] TMP_Text descriptionText = null;
    [SerializeField, ForceFill] TMP_Text valuesText = null;

    [SerializeField, Min(0)] public int entriesShown = 5;


    enum HighscoreType { Global = 0, Session = 1, Track = 2 }
    [SerializeField] HighscoreType displayedHighscore = HighscoreType.Session;


    [SerializeField] HighScoreDisplayer globalHighscores; //since all time
    [SerializeField] HighScoreDisplayer sessionHighscores; //since application started
    [SerializeField] HighScoreDisplayer trackHighscores; //on current track


    [Title("Input")]
    [SerializeField] InputActionReference nextHighscoreType = null;
    [SerializeField] InputActionReference previousHighscoreType = null;

    public ShowHighscores()
    {
        globalHighscores = new(this);
        sessionHighscores = new(this);
        trackHighscores = new(this);

        Debug.Assert(Instance == null, "Duplicate Instance " + nameof(ShowHighscores));
        Instance = this;
    }

    public void AddScore(float seconds)
    {
        globalHighscores.AddScore(seconds);
        sessionHighscores.AddScore(seconds);
        trackHighscores.AddScore(seconds);

        DisplayHighscore();
    }
    void TrackGenerator.ITrackObserver.OnTrackChanged()
    {
        trackHighscores.storedHighscores.Clear();
        if (displayedHighscore == HighscoreType.Track)
            trackHighscores.DisplayOnUI();
    }

    public void ChangeHighscoreType(int direction)
    {
        displayedHighscore = (HighscoreType)(((int)(displayedHighscore) + direction % 3 + 3) % 3);
        DisplayHighscore();
    }
    void DisplayHighscore()
    {
        switch (displayedHighscore)
        {
            case HighscoreType.Global:
                globalHighscores.DisplayOnUI();
                break;
            case HighscoreType.Session:
                sessionHighscores.DisplayOnUI();
                break;
            case HighscoreType.Track:
                trackHighscores.DisplayOnUI();
                break;
        }
    }

    private void Awake()
    {
        string csv = PlayerPrefs.GetString(nameof(globalHighscores));
        if (!string.IsNullOrEmpty(csv))
        {
            try
            {
                List<float> scores = csv.Split("$").Select(i => float.Parse(i)).ToList();
                if (trackGenerator.trackLength == (int)scores[0]) //first is the trackPartsAmount
                {
                    scores.RemoveAt(0);
                    globalHighscores.storedHighscores = scores;
                }
                else
                {
                    Debug.LogWarning($"Stored global highscores were set for {nameof(trackGenerator.trackLength)}={scores[0]} instead of ={trackGenerator.trackLength}.\n" +
                                      "All saved scores were deleted!");
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Stored global highscores were in wrong format. All saved scores were deleted!" +
                               "\n\nFormat was: " + csv);
                Debug.LogException(e);
            }
        }

        trackGenerator.trackObserver.Add(this);
    }
    private void Start()
    {
        DisplayHighscore();
    }
    private void OnDestroy()
    {
        //Add TrackGenerator.TracksGenerated number at start to know how the scores were hit
        string csv = globalHighscores.storedHighscores.Any() ?
                $"{trackGenerator.trackLength}$" + string.Join("$", globalHighscores.storedHighscores)
                : $"{trackGenerator.trackLength}";

        PlayerPrefs.SetString(nameof(globalHighscores), csv);

        trackGenerator.trackObserver.Remove(this);
    }
    private void OnEnable()
    {
        nextHighscoreType.action.started += Input_NextHighscoreType;
        previousHighscoreType.action.started += Input_PreviousHighscoreType;
    }
    private void OnDisable()
    {
        nextHighscoreType.action.started -= Input_NextHighscoreType;
        previousHighscoreType.action.started -= Input_PreviousHighscoreType;
    }
    void Input_NextHighscoreType(InputAction.CallbackContext context) => ChangeHighscoreType(1);
    void Input_PreviousHighscoreType(InputAction.CallbackContext context) => ChangeHighscoreType(-1);

    [System.Serializable]
    class HighScoreDisplayer
    {
        [NonSerialized] public ShowHighscores baseClass;
        public HighScoreDisplayer(ShowHighscores baseClass)
        {
            this.baseClass = baseClass;
        }

        TMP_Text ValuesTextAsset => baseClass.valuesText;
        TMP_Text HeadlineTextAsset => baseClass.headlineText;
        TMP_Text DescriptionTextAsset => baseClass.descriptionText;

        int EntriesShown => baseClass.entriesShown;

        [SerializeField, ReadOnly, ListContainer]
        public ListContainer<float> storedHighscores = new();

        [SerializeField, ForceFill("<missing>")]
        public string headline = "<missing>";
        [SerializeField, ForceFill("<missing>")]
        public string description = "<missing>";

        public void DisplayOnUI()
        {
            DescriptionTextAsset.text = description;
            HeadlineTextAsset.text = headline;

            if (storedHighscores.Count > 5)
                storedHighscores = storedHighscores.Take(5).ToList();

            ValuesTextAsset.text = string.Join("", storedHighscores.Select((v, i) => $"{i}. {Helpers.TimeString(v)}\n"));

            for (int i = storedHighscores.Count; i < 5; i++)
            {
                ValuesTextAsset.text += $"{i}. --:--:---\n";
            }
        }
        /// <summary>
        /// Adds a score if better than top 5 results or new worst result. Only best 5 are shown
        /// </summary>
        public void AddScore(float seconds)
        {
            if (storedHighscores.Count <= 0)
            {
                storedHighscores.Add(seconds);
            }
            else
            {
                //If any better result (less seconds)
                for (int i = 0; i < Math.Min(storedHighscores.Count, EntriesShown); i++)
                {
                    if (seconds < storedHighscores[i])
                    {
                        storedHighscores.Insert(i, seconds);
                        break;
                    }
                }
                //If worse than everything, than append
                if (seconds > storedHighscores[^1])
                {
                    storedHighscores.Add(seconds);
                }
            }
        }
    }
}
