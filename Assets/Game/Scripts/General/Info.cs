using UnityEngine;

/// <summary>
/// Can be attached to a gameObject to show some description text
/// </summary>
public class Info : MonoBehaviour
{
    [TextArea] public string text;

#if !UNITY_EDITOR
    private void Awake()
    {
        Destroy(this);
    }
#endif
}
