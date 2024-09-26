using CustomInspector.Extensions;
using UnityEditor;
using UnityEngine;

namespace CustomInspector.Editor
{
    [CustomPropertyDrawer(typeof(CustomInspector.SerializableNullable<>))]
    [CustomPropertyDrawer(typeof(NullableAttribute))]
    public class SerializableNullableDrawer : PropertyDrawer
    {

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty hasValueProp = property.FindPropertyRelative("hasValue");
            SerializedProperty valueProp = property.FindPropertyRelative("value");

            position.width -= EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            //Draw property
            bool hasValue = hasValueProp.boolValue;
            if (hasValue)
                DrawProperties.PropertyField(position, label, valueProp);
            else
                EditorGUI.LabelField(position, label, new GUIContent("null"));

            // button
            using (new NewIndentLevel(0))
            {
                Rect buttonRect = new(position)
                {
                    x = position.x + position.width + EditorGUIUtility.standardVerticalSpacing,
                    width = EditorGUIUtility.singleLineHeight,
                };
                if (GUI.Button(buttonRect, hasValue ? "-" : "+"))
                {
                    hasValueProp.boolValue ^= true;
                    property.serializedObject.ApplyModifiedProperties();
                }
            }
        }
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty hasValueProp = property.FindPropertyRelative("hasValue");
            SerializedProperty valueProp = property.FindPropertyRelative("value");

            if (hasValueProp.boolValue)
                return EditorGUI.GetPropertyHeight(valueProp, label);
            else
                return EditorGUIUtility.singleLineHeight;
        }
    }
}
