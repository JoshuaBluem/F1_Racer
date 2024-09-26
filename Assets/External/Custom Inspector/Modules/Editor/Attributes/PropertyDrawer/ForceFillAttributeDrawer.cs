using CustomInspector.Extensions;
using CustomInspector.Helpers.Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;


namespace CustomInspector.Editor
{
    [CustomPropertyDrawer(typeof(ForceFillAttribute))]
    public class ForceFillAttributeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            ForceFillAttribute ffa = (ForceFillAttribute)attribute;
            var info = cache.GetInfo(property, ffa, fieldInfo);

            //general errors
            if (info.ErrorMessage != null)
            {
                DrawProperties.DrawPropertyWithMessage(position, label, property, info.ErrorMessage, MessageType.Error);
                return;
            }

            //If we should even test
            if (ffa.onlyTestInPlayMode && !Application.isPlaying)
            {
                EditorGUI.BeginChangeCheck();
                DrawProperties.PropertyField(position, label, property);
                if (EditorGUI.EndChangeCheck())
                    property.serializedObject.ApplyModifiedProperties();
                return;
            }

            //if filled
            EditorGUI.BeginChangeCheck();

            object value = property.GetValue();
            if (info.Invalids.Contains(value))
            {
                string errorMessage;
                if (!string.IsNullOrEmpty(ffa.errorMessage))
                {
                    errorMessage = ffa.errorMessage;
                }
                else
                {
                    errorMessage = $"ForceFill: Value of '{ToString(value)}' on '{property.name}' is not valid.";
                    if (info.Invalids.Length > 1)
                        errorMessage += $"\nForbidden Values are: {string.Join(", ", info.Invalids.Select(_ => ToString(_)))}";
                }

                DrawProperties.DrawPropertyWithMessage(position, label, property,
                            errorMessage, MessageType.Error);

                static string ToString(object o)
                {
                    if (o == null)
                        return "null";
                    string res = o.ToString();
                    if (res == "")
                        return "empty";
                    return res;
                }
            }
            else
            {
                DrawProperties.PropertyField(position, label, property);
            }

            if (EditorGUI.EndChangeCheck())
                property.serializedObject.ApplyModifiedProperties();
        }
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var info = cache.GetInfo(property, attribute, fieldInfo);

            //general errors
            if (info.ErrorMessage != null)
            {
                return DrawProperties.GetPropertyWithMessageHeight(label, property);
            }

            //If even test
            var ffa = (ForceFillAttribute)attribute;
            if (ffa.onlyTestInPlayMode && !Application.isPlaying)
                return DrawProperties.GetPropertyHeight(label, property);

            //if filled
            object value = property.GetValue();
            if (info.Invalids.Contains(value))
                return DrawProperties.GetPropertyWithMessageHeight(label, property);
            else
                return DrawProperties.GetPropertyHeight(label, property);

        }

        static readonly PropInfoCache<PropInfo> cache = new();
        class PropInfo : ICachedPropInfo
        {
            public string ErrorMessage { get; private set; }
            public object[] Invalids { get; private set; }

            public PropInfo() { }
            public void Initialize(SerializedProperty property, PropertyAttribute attr, FieldInfo fieldInfo)
            {
                ForceFillAttribute attribute = (ForceFillAttribute)attr;

                //if no given invalids, we take default value
                if ((attribute.notAllowed?.Length ?? 0) < 1)
                {
                    Type propSystemType = property.propertyType.ToSystemType();
                    if (propSystemType == null)
                    {
                        ErrorMessage = "Property is null";
                        this.Invalids = new object[] { null };
                    }
                    else if (propSystemType == typeof(string)) //unity auto converts null-string to empty-string
                        this.Invalids = new object[] { "" };
                    else if (propSystemType == typeof(char)) //Activator.CreateInstance invalid on char
                        this.Invalids = new object[] { '\0' };
                    else if (propSystemType == typeof(Enum)) //Activator.CreateInstance invalid on enums
                        this.Invalids = new object[] { Enum.ToObject(fieldInfo.FieldType, 0) };
                    else if (propSystemType.IsValueType)
                    {
                        try
                        {
                            this.Invalids = new object[] { Activator.CreateInstance(propSystemType) };
                        }
                        catch
                        {
                            this.Invalids = new object[] { null };
                            Debug.LogWarning($"{nameof(ForceFillAttribute)}: needs a paramter (invalid value) for value type {propSystemType} because it has no default constructor");
                        }
                    }
                    else
                        this.Invalids = new object[] { null };
                    ErrorMessage = null;
                }
                else
                {
                    List<object> invalids = new();
                    //add given invalids
                    foreach (var item in attribute.notAllowed)
                    {
                        if (property.propertyType == SerializedPropertyType.String //unity auto converts null-string to empty-string
                            && item == null)
                        {
                            invalids.Add("");
                            continue;
                        }

                        try
                        {
                            invalids.Add(property.ParseString(item));
                        }
                        catch
                        {
                            ErrorMessage = $"ForceFill: Failed to parse \"{item}\" as \"{property.propertyType}\"";
                        }
                    }
                    this.Invalids = invalids.ToArray();
                    ErrorMessage = null;
                }
            }
        }
    }
}