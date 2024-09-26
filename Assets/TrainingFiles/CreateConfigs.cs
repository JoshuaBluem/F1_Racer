using CustomInspector;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

public class CreateConfigs : MonoBehaviour
{
    [SerializeField, AssetPath] FilePath configurationFile = new();
    [SerializeField, AssetPath] FolderPath configFiles = new();

    [SerializeField, Min(3)] int filesAmount = 100;

    [Button(nameof(CreateFiles))]
    [SerializeField, HideField] bool _;

    public void CreateFiles()
    {
        Debug.Log("Creating files ...");

        // Read
        string[] lines = configurationFile.ReadAllLines();

        // Check Input
        if (!configurationFile.HasPath())
            throw new NullReferenceException("configurationFilePreset must be filled in the inspector");
        if (!configFiles.HasPath())
            throw new NullReferenceException("configFiles must be filled in the inspector");

        if (!lines.Any(l => l.Any(c => c == '[')))
            throw new ArgumentException(nameof(configurationFile) + " does not contain any ranges to choose values from. Expected ranges like \"[0, 1, -]\"");

        if (configFiles.GetAllFiles().Any())
            throw new ArgumentException($"There are already files present in {configFiles.GetPath()}.\nPlease delete them first!"); //i do not want to delete it from code, because deleted files cannot be restored!

        int filesAmount = this.filesAmount;
        int possibilities = (int)Mathf.Pow(3, lines.Where(l => l.Any(c => c == '[')).Count());
        if (possibilities < filesAmount)
        {
            Debug.LogWarning("There are less configuration possibilities than requested filesAmount");
            filesAmount = possibilities;
        }


        // Create new
        string trainerMethodName = configurationFile.Name[..3];
        List<ParameterLine> content = lines.Select(l => new ParameterLine(l)).ToList();

        Stopwatch sw = Stopwatch.StartNew();
        for (int fileIndex = 0; fileIndex < filesAmount; fileIndex++)
        {
            //set valies in lines
            lines = content.Select(l => l.WithRandomValue()).ToArray();
            //constraints
            for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                if (Cut(WithoutWhitespaces(lines[lineIndex]), "buffer_size".Length) == "buffer_size")
                {
                    //read
                    int bufferSize_separatorIndex = lines[lineIndex].IndexOf(':');
                    int buffer_size = int.Parse(lines[lineIndex][(bufferSize_separatorIndex + 1)..]);
                    string batch_size_line = lines.First(l => Cut(WithoutWhitespaces(l), "batch_size".Length) == "batch_size");
                    int batch_size = int.Parse(batch_size_line[(batch_size_line.IndexOf(':') + 1)..]);
                    //calculate new buffersize
                    if (buffer_size > 2 * batch_size)
                        buffer_size = (int)(buffer_size / (float)batch_size + 0.5f) * batch_size; //buffer_size must be multiples of batch_size
                    else
                        buffer_size = 2 * batch_size;
                    //override new buffersize
                    lines[lineIndex] = lines[lineIndex][..(bufferSize_separatorIndex + 2)] + buffer_size.ToString();
                }
                else if (Cut(WithoutWhitespaces(lines[lineIndex]), "run_id".Length) == "run_id")
                {
                    int separatorIndex = lines[lineIndex].IndexOf(':');
                    lines[lineIndex] = lines[lineIndex][..separatorIndex] + ": " + trainerMethodName + "_Config" + fileIndex;
                }
            }

            //Create file
            configFiles.CreateFileAllText($"{trainerMethodName}_Config{fileIndex}.yaml", string.Join("\n", lines));
            //Check time
            if (sw.ElapsedMilliseconds / 1000 > 20)
            {
                Debug.LogError("Timeout!");
                return;
            }
        }


        Debug.Log("... files created!");

        static string Cut(string s, int length)
        {
            if (s.Length < length)
                return s;
            else
                return s[..length];
        }
        static string WithoutWhitespaces(string s)
        {
            string res = "";
            foreach (char c in s)
            {
                if (c != ' ')
                    res += c;
            }
            return res;
        }
    }

    class ParameterLine
    {
        static readonly NumberFormatInfo floatFormat = new CultureInfo("en-US").NumberFormat;
        readonly string name;
        readonly MinMax possibleValues;

        public ParameterLine(string line)
        {
            //empty line
            if (line.Length <= 0)
                return;

            //parameter line
            for (int i = 0; i < line.Length - 2; i++)
            {
                if (line[i] == ':')
                {
                    name = line[..(i + 2)];
                    var values = new string(line[(i + 2)..].Where(c => c != ' ' //e.g. "5" or "[1, 2, -] U [1.5]"
                                                               && c != '['
                                                               && c != ']'
                                                               && c != '-').ToArray()).Split(',').Where(l => !string.IsNullOrEmpty(l)).ToArray();


                    if (!values.Skip(1).Any()) //only 1 element
                    {
                        possibleValues = new FixedValue(values[0]);
                    }
                    else if (line.Contains('.')) //are floats
                    {
                        var floats = values.Select(v => float.Parse(v, floatFormat));
                        possibleValues = new FloatMinMax(floats.Min(), floats.Max());
                    }
                    else
                    {
                        try //ints
                        {
                            var ints = values.Select(v => int.Parse(v));
                            possibleValues = new IntMinMax(ints.Min(), ints.Max());
                        }
                        catch (FormatException) //words
                        {
                            possibleValues = new OptionsSelect(values);
                        }
                    }
                    return;
                }
            }

            //some headline
            name = line;
            possibleValues = new OptionsSelect(new string[] { "" });
        }
        public string WithRandomValue()
        {
            return name + possibleValues.GetRandomValue();
        }

        abstract class MinMax
        {
            public abstract string GetRandomValue();
        }
        class FixedValue : MinMax
        {
            readonly string value;
            public FixedValue(string value)
            {
                this.value = value;
            }
            public override string GetRandomValue()
            {
                return value;
            }
        }
        class OptionsSelect : MinMax
        {
            readonly string[] options;
            public OptionsSelect(string[] options)
            {
                this.options = options;
            }
            public override string GetRandomValue()
            {
                return options.GetRandomItem();
            }
        }
        class IntMinMax : MinMax
        {
            readonly int min;
            readonly int max;
            readonly bool makeEven; //multiples of 2
            public IntMinMax(int min, int max)
            {
                Debug.Assert(min < max, "Min has to be smaller than Max: min={min}, max={max}");
                makeEven = max - min > 50;
                this.min = min;
                this.max = max;
            }
            public override string GetRandomValue()
            {
                int value = Random.Range(min, max + 1);
                if (makeEven)
                    value = ((int)(value / 2)) * 2;
                return value.ToString();
            }
        }
        class FloatMinMax : MinMax
        {
            readonly float min;
            readonly float max;
            public FloatMinMax(float min, float max)
            {
                Debug.Assert(min < max, "Min has to be smaller than Max: min={min}, max={max}");
                this.min = min;
                this.max = max;
            }
            public override string GetRandomValue()
            {
                return Random.Range(min, max).ToString(floatFormat);
            }
        }
    }
}
