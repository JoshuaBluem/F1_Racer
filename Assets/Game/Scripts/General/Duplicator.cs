using System.IO;
using System.Linq;
using UnityEngine;

/// <summary>
/// Duplicates any scene-object if enabled
/// </summary>
public class Duplicator : MonoBehaviour
{
    [SerializeField] GameObject original;
    [SerializeField, Min(1)] int amount = 10;

    private void Awake()
    {
        string[] args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "agentsAmount" && i + 1 < args.Length)
            {
                if (int.TryParse(args[i + 1], out int agentsAmount))
                {
                    if (agentsAmount >= 1)
                    {
                        Debug.Log("Agents Amount: " + agentsAmount);
                        amount = agentsAmount - 1;
                    }
                    else
                    {
                        Debug.LogError($"Agents amount must be greater zero.\namount was {agentsAmount}");
                    }
                }
                else
                {
                    Debug.LogError("ParseError: Invalid value for agentsAmount.");
                }
                break;
            }
        }
    }
    void OnEnable()
    {
        for (int i = 0; i < amount; i++)
        {
            var copy = Instantiate(original, original.transform.position, original.transform.rotation, this.transform);
            copy.name = original.name + " (Copy)";
            Debug.Assert(copy.transform.position == original.transform.position, "Copy was instantiated on wrong position");
            // Debug.Log($"position set: {copy.transform.position}");
        }
        Debug.Log($"Playing with +{amount} duplicates");
        if (!Application.isEditor)
        {
            string resultsPath = "C:\\Unity\\Uni\\F1_Racer\\results";
            var logFiles = Directory.GetFiles(resultsPath).Where(filePath =>
            {
                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
                return fileNameWithoutExtension.Length >= "Playing with".Length &&
                       fileNameWithoutExtension[.."Playing with".Length] == "Playing with";
            });
            foreach (var logFile in logFiles)
                File.Delete(logFile);
            File.WriteAllText(resultsPath + $"/Playing with {amount + 1} agents.empty", "\n");
        }
    }
    private void OnDisable()
    {
        foreach (Transform child in this.transform)
        {
            Destroy(child.gameObject);
        }
    }
}
