using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif


public abstract class WithButtonMethod : MonoBehaviour
{
    public abstract void ButtonMethod();
    public abstract string Description { get; }
}

#if UNITY_EDITOR
[CustomEditor(typeof(WithButtonMethod), true)]
class WithButtonMethodEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var mainObject = (WithButtonMethod)target;
        EditorGUILayout.LabelField("Description:", mainObject.Description, EditorStyles.wordWrappedLabel, GUILayout.Height(EditorGUIUtility.singleLineHeight * 2));

        DrawDefaultInspector();

        if (GUILayout.Button("Execute", GUILayout.Height(30)))
        {
#pragma warning disable IDE0220 // Add explicit cast
            foreach (WithButtonMethod obj in targets) // Loop through all selected objects
            {
                obj.ButtonMethod();
            }
#pragma warning restore IDE0220 // Add explicit cast
        }
    }
}
#endif