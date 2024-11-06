using SFB;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;
public class RecordEpisodes
{
    public RecordEpisodes(bool activeAtStart)
    {
        this.active = activeAtStart;
    }
    bool active = false;
    bool episodeStarted = false;

    List<EpisodeInformation> allEpisodes = new();
    public float currentEpisodeReward = 0;
    public DateTime currentEpisodeStartTime;

    public bool Active
    {
        get => active;
        set
        {
            if (active && !value) // gets deactivated
            {
                SaveRecording();
            }
            else if (!active && value) // get activated
            {
                EpisodeInterrupted();
                allEpisodes.Clear();
            }
            active = value;
        }
    }
    public void StartEpisode()
    {
        if (!active)
            return;

        // Debug.Log("StartEpisode");
        currentEpisodeReward = 0;
        currentEpisodeStartTime = DateTime.Now;
        episodeStarted = true;
    }
    public void EndEpisode()
    {
        if (!active)
            return;

        if (!episodeStarted)
        {
            Debug.LogWarning("Episode was already terminated. Please call StartEpisode before calling EndEpisode again.");
            return;
        }
        // Debug.Log("EndEpisode");
        episodeStarted = false;
        if (currentEpisodeReward != 0) //if anything recorded
        {
            allEpisodes.Add(new EpisodeInformation(reward: currentEpisodeReward, startTime: currentEpisodeStartTime, endTime: DateTime.Now));
        }
    }
    public void EpisodeInterrupted()
    {
        episodeStarted = false;
        currentEpisodeReward = 0;
    }
    public void AddReward(float amount)
    {
        if (!active)
            return;

        // Debug.Log("AddReward");
        if (episodeStarted)
            currentEpisodeReward += amount;
    }
    void SaveRecording()
    {
        if (allEpisodes.Count <= 0)
        {
            Debug.Log("Recording stopped without any records");
            return;
        }
        string newPath = StandaloneFileBrowser.SaveFilePanel("Save your recording", null, "Recording_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"), "csv");
        if (string.IsNullOrEmpty(newPath))
        {
            Debug.LogError($"Recording was not saved, because no target path was selected in the {nameof(StandaloneFileBrowser)}");
            return;
        }
        File.WriteAllText(newPath, EpisodeInformation.Labels + "\n" + string.Join("\n", allEpisodes.Select(i => i.Values)));
        Debug.Log("Recording saved at " + newPath);
    }

    readonly struct EpisodeInformation
    {
        /// <summary>
        /// Reward collected in this episode
        /// </summary>
        public readonly float reward;
        /// <summary>
        /// Start time of this episode
        /// </summary>
        public readonly long wallTime;
        /// <summary>
        /// Duration of this episode
        /// </summary>
        public readonly int milliseconds;

        public static string Labels => $"{nameof(reward)},{nameof(wallTime)},{nameof(milliseconds)}";
        public readonly string Values => $"{reward.ToString(CultureInfo.InvariantCulture)},{wallTime},{milliseconds}";

        public EpisodeInformation(float reward, DateTime startTime, DateTime endTime)
        {
            this.reward = reward;
            this.wallTime = new DateTimeOffset(startTime).ToUnixTimeMilliseconds();
            this.milliseconds = (int)(((endTime - startTime) * Time.timeScale).TotalMilliseconds);
        }
    }
}
