using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

/*
* Reads all Folders within a Folder and gets the config files in each. From there it reads all parameters of each file and saves from all files in one big csv file.
*/

class ReadHyperparams : WithButtonMethod
{
    public override string Description { get; } = "Saves hyperparameters of all training-results in a folder in a csv file";

    [SerializeField, Tooltip("Path to folder, where configs are in")] string top_training_paths = "E:\\Wichtig!!\\Uni\\Bachelorarbeit\\Training\\results-sac-100x_new";
    [SerializeField, Tooltip("Path where generated csv should be saved")] string target_csv = "C:\\tmp\\results.txt";

    public override void ButtonMethod()
    {
        string[] all_training_paths = Directory.GetDirectories(top_training_paths);
        var values = new List<string>();


        for (int i = 0; i < all_training_paths.Length; i++)
        {
            string fileContent = File.ReadAllText(all_training_paths[i] + "\\configuration.yaml");
            // Console.WriteLine($"used configuration for i={i}:\n" + fileContent);
            Info contents = Info.GetInfos(fileContent);

            if (i == 0)
            {
                string paths = "run_name," + string.Join(",", Info.Path_Values(contents).Select(x => x.path));
                values.Add(paths);
            }

            string line = Path.GetFileNameWithoutExtension(all_training_paths[i]) + ",";
            line += string.Join(",", Info.Path_Values(contents).Select(x => x.value));
            values.Add(line);
        }

        string full_table = string.Join("\n", values);

        File.WriteAllText(target_csv, full_table);
    }


    static class Methods
    {
        public static int SpacesCount(string s)
        {
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == ' ' || s[i] == '\t')
                {
                    continue;
                }
                else
                {
                    Debug.Log($"{i} spaces on '{s}'");
                    return i;
                }
            }
            throw new Exception("String was not expected to have only spaces!");
        }
    }
    class Info
    {
        public readonly string name = "";
        public readonly object content;

        public static Info GetInfos(string fileContent)
        {
            string[] lines = fileContent.Split("\r\n");
            lines = lines.Where(l => l.Any(c => char.IsLetter(c))).ToArray(); // remove empty lines
#pragma warning disable CS8123 // The tuple element name is ignored because a different name or no name is specified by the assignment target.
            List<(int indentation, string content)> grouped = lines.Select<string, (int, string)>(line => (Methods.SpacesCount(line), line))
                                                                          .Select<(int, string), (int, string)>(tuple => (indentation: tuple.Item1, content: tuple.Item2[tuple.Item1..]))
                                                                          .ToList();
#pragma warning restore CS8123 // The tuple element name is ignored because a different name or no name is specified by the assignment target.
            /*
             * e.g
             * grouped = { (0, "C: 1"), (0, "Behaviour:"), (1, "A: 1"), (1, "Bs:"), (2, "B1: 1") }
             */
            return new Info("", grouped);
        }
        private Info(string splitElement, List<(int indentation, string content)> lines)
        {
            if (lines.Count < 1)
            {
                int splitIndex = splitElement.IndexOf(':');
                Debug.Assert(splitIndex != -1, "No Info given! SplitElement and lines cannot be both empty!");
                this.name = splitElement[..splitIndex];
                Debug.Assert(splitIndex + 2 < splitElement.Length, "If no lines given, content must be provided after the ':'! Error in: " + splitElement);
                this.content = splitElement[(splitIndex + 2)..];
            }
            else
            {
                if (splitElement.Length > 0)
                    this.name = splitElement[..^1];

                int lowestIndentation = lines[0].indentation;
                lines = lines.Select(l => (l.indentation - lowestIndentation, l.content)).ToList();
                Debug.Assert(lines.All(l => l.indentation >= 0));

                List<(string splitElement, List<(int indentation, string content)> group)> groups = Divide(lines).ToList();

                content = groups.Select(x => new Info(x.splitElement, x.group));


                ///<summary>Splits, where indentation is 0 and returns each group and the split element seperately</summary>
                static IEnumerable<(string splitElement, List<(int indentation, string content)> group)> Divide(IEnumerable<(int indentation, string content)> collection)
                {
                    List<(int indentation, string content)> res = new();
                    string splitElement = null;
                    foreach (var item in collection)
                    {
                        Debug.Log($"Item read: {item}");
                        if (item.indentation == 0)
                        {
                            if (splitElement != null)
                            {
                                Debug.Log($"Returned: ({splitElement}, {res.Count})");
                                yield return (splitElement, res); // send previous group
                            }
                            splitElement = item.content;
                            res = new();
                        }
                        else
                        {
                            Debug.Log($"Item added: {item}");
                            res.Add(item);
                        }
                    }
                    if (splitElement == null)
                        throw new Exception("Not a single group was there??");
                    yield return (splitElement, res); // send previous group
                }
            }
        }
        private Info(string name, object content)
        {
            this.name = name;
            this.content = content;
        }
        public override string ToString()
        {
            return $"Info({name})";
        }
        /// <summary>
        /// Returns the path of each parameter and the value of it
        /// </summary>
        public static IEnumerable<(string path, string value)> Path_Values(Info info, string prefix = "")
        {
            if (info.content is IEnumerable<object> ie)
            {
                if (!string.IsNullOrEmpty(info.name))
                    prefix += $"{info.name}.";
                foreach (var item in ie)
                {
                    if (item is Info i)
                    {
                        foreach (var element in Path_Values(i, prefix))
                            yield return element;
                    }
                    else if (item is string s)
                    {
                        int splitIndex = s.IndexOf(':');
                        string name = s[..splitIndex];
                        string content = s[(splitIndex + 2)..];
                        yield return (prefix + name, content);
                    }
                    else
                        throw new Exception();
                }
            }
            else
            {
                Debug.Assert(!string.IsNullOrEmpty(info.name), "Field cannot have no name");
#pragma warning disable CS8619 // Nullability of reference types in value doesn't match target type.
                yield return (prefix + info.name, info.content.ToString());
#pragma warning restore CS8619 // Nullability of reference types in value doesn't match target type.
            }
        }
        public string ToCSV()
        {
            return "path,value\n" + string.Join("\n", Path_Values(this).Select(t => $"{t.Item1},{t.Item2}"));
        }
    }

}

