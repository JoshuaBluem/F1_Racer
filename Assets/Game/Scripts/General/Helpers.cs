using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// often used methods
/// </summary>
public static class Helpers
{
    public static T GetRandomItem<T>(this IList<T> collection)
    {
        return collection[Random.Range(0, collection.Count)];
    }
    public static string TimeString(int seconds)
    {
        return $"{(seconds / 60):00}:{(seconds % 60):00}";
    }
    public static string TimeString(float seconds)
    {
        int floored = (int)seconds;
        return $"{(floored / 60):00}:{(floored % 60):00}:{((seconds % 1) * 1000):000}";
    }
    public static Color GetRandomColor()
    {
        return ColorFromHSV(Random.Range(0f, 1f), 1, 0.7f);
    }
    public static Color ColorFromHSV(float hue, float saturation, float value)
    {
        Debug.Assert(hue <= 1, "hue must be in range [0, 1]");
        Debug.Assert(saturation <= 1, "saturation must be in range [0, 1]");
        Debug.Assert(value <= 1, "value must be in range [0, 1]");

        int hi = Convert.ToInt32(Mathf.Floor(hue * 255 / 60)) % 6;
        float f = hue * 255 / 60 - Mathf.Floor(hue / 60);

        // value = value * 255;
        float v = value;
        float p = value * (1 - saturation);
        float q = value * (1 - f * saturation);
        float t = value * (1 - (1 - f) * saturation);

        if (hi == 0)
            return new Color(v, t, p, 1);
        else if (hi == 1)
            return new Color(q, v, p, 1);
        else if (hi == 2)
            return new Color(p, v, t, 1);
        else if (hi == 3)
            return new Color(p, q, v, 1);
        else if (hi == 4)
            return new Color(t, p, v, 1);
        else
            return new Color(v, p, q, 1);
    }
    public static IEnumerable<IEnumerable<T>> Split<T>(this IEnumerable<T> enumerable, T separator)
    {
        var e = enumerable.GetEnumerator();

        List<T> group;

    CreateNewGroup:
        group = new();

        while (e.MoveNext())
        {
            if (e.Current?.Equals(separator) ?? separator is null)
            {
                if (group.Count > 0)
                {
                    yield return group;
                    goto CreateNewGroup;
                }
            }
            else
            {
                group.Add(e.Current);
            }
        }

        if (group.Count > 0)
        {
            yield return group;
        }
    }
}
