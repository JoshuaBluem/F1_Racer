using CustomInspector;
using System.Collections;
using System.Diagnostics;
using TMPro;
using UnityEngine;

/// <summary>
/// displays fps in the ui
/// </summary>
public class FPS_Counter : MonoBehaviour
{
    [SerializeField, ForceFill] TMP_Text fps_text;

    [SerializeField, ReadOnly] int currentFPS = 60;

    private void OnEnable()
    {
        StartCoroutine(Counting());

        IEnumerator Counting()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            while (this.enabled)
            {
                int frames = 0;
                while (stopwatch.ElapsedMilliseconds < 1000)
                {
                    frames++;
                    yield return null;
                }
                currentFPS = (frames + currentFPS) / 2; // smooth out changes
                fps_text.text = $"{currentFPS} FPS";

                yield return null;
                stopwatch.Restart();
            }
        }
    }
}
