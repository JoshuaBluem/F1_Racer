using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

public class EditHyperparameter : WithButtonMethod
{
    public override string Description { get; } = "Sets a specific parameter in all training-results in a folder";

    [SerializeField] string target_Folder = "C:\\Unity\\Uni\\F1_Racer\\results";

    [SerializeField] string parameterName = "max_steps";
    [SerializeField] string targetValue = "22000000";


    public override void ButtonMethod()
    {
        for (int i = 1; i <= 100; i++)
        {
            string path = target_Folder + $"\\SAC_Config{i}\\configuration.yaml";
            string[] lines = File.ReadAllLines(path);
            for (int j = 0; j < lines.Length; j++)
            {
                if (lines[j].Contains(parameterName + ": ")) // the space after ':' makes sure, that there comes a value after that we can change.
                {
                    lines[j] = Regex.Replace(lines[j], pattern: @"(.*:\s)(.*)(\r?\n?)", replacement: "${1}" + targetValue + "${3}");
                    // Debug.Log("Found!");
                    File.WriteAllLines(path, lines);
                    goto nextFile;
                }
            }
            throw new ArgumentException($"Parameter '{parameterName}' could not be edited directly in '{path}'.");
        nextFile:;
        }
    }
}
