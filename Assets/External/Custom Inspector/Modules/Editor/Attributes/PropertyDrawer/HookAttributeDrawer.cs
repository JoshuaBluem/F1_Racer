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
    [CustomPropertyDrawer(typeof(HookAttribute))]
    public class HookAttributeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            PropInfo info = cache.GetInfo(property, attribute, fieldInfo);

            if (info.ErrorMessage != null)
            {
                DrawProperties.DrawPropertyWithMessage(position, label, property, info.ErrorMessage, MessageType.Error);
                return;
            }


            if (Selection.count <= 1) //single editing
            {
                object oldValue = property.GetValue();

                EditorGUI.BeginChangeCheck();
                DrawProperties.PropertyField(position, label, property);
                if (EditorGUI.EndChangeCheck())
                {
                    object newValue = property.GetValue();

                    HookAttribute a = (HookAttribute)attribute;
                    if (a.useHookOnly)
                    {
                        //Revert change on property
                        property.SetValue(oldValue);
                    }
                    //property to instantiation
                    property.serializedObject.ApplyModifiedProperties();
                    //method on instantiation
                    info.HookMethod(property, oldValue, newValue);
                    //instantiation to property
                    property.serializedObject.ApplyModifiedFields(true);
                }
            }
            else // multiediting
            {
                List<SerializedObject> serializedObjects = property.serializedObject.targetObjects.Select(_ => new SerializedObject(_)).ToList();
                List<SerializedProperty> serializedProperties = serializedObjects.Select(so => so.FindProperty(property.propertyPath)).ToList();
                List<object> oldValues = serializedProperties.Select(p => p.GetValue()).ToList();

                EditorGUI.BeginChangeCheck();
                DrawProperties.PropertyField(position, label, property);
                if (EditorGUI.EndChangeCheck())
                {
                    object newValue = property.GetValue();

                    //remove not changed ones
                    if (property.propertyType != SerializedPropertyType.Generic) //on generics the equals is overridden often, so we cant really know if something changed
                    {
                        for (int i = 0; i < oldValues.Count; i++)
                        {
                            bool equal = oldValues[i] == null ? newValue == null : oldValues[i].Equals(newValue);
                            if (equal)
                            {
                                serializedObjects.RemoveAt(i);
                                serializedProperties.RemoveAt(i);
                                oldValues.RemoveAt(i);
                                i--;
                            }
                        }
                        Debug.Assert(oldValues.Count > 0, $"{nameof(HookAttribute)}: Hook not executed, because new value *equals* old value.");
                    }

                    //revert changes if hook only
                    HookAttribute a = (HookAttribute)attribute;
                    if (a.useHookOnly)
                    {
                        //Revert change on property
                        for (int i = 0; i < serializedProperties.Count; i++)
                            serializedProperties[i].SetValue(oldValues[i]);
                    }
                    //property to instantiation
                    foreach (var so in serializedObjects)
                        so.ApplyModifiedProperties();
                    //method on instantiation
                    for (int i = 0; i < serializedProperties.Count; i++)
                        info.HookMethod(serializedProperties[i], oldValues[i], newValue);
                    //instantiation to property
                    foreach (var so in serializedObjects)
                        so.ApplyModifiedFields(true);
                }
            }
        }
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            PropInfo info = cache.GetInfo(property, attribute, fieldInfo);

            if (info.ErrorMessage != null)
                return DrawProperties.GetPropertyWithMessageHeight(label, property);
            else
                return DrawProperties.GetPropertyHeight(label, property);
        }

        static readonly PropInfoCache<PropInfo> cache = new();

        class PropInfo : ICachedPropInfo
        {
            public string ErrorMessage { get; private set; }
            public bool MethodHasParameters { get; private set; }
            /// <summary> A method that executes on with property, oldValue & newValue </summary>
            public Action<SerializedProperty, object, object> HookMethod { get; private set; }

            public PropInfo() { }
            public void Initialize(SerializedProperty property, PropertyAttribute attr, FieldInfo fieldInfo)
            {
                DirtyValue owner = DirtyValue.GetOwner(property);
                HookAttribute attribute = (HookAttribute)attr;
                Type propertyType = fieldInfo.FieldType;

                InvokableMethod method;
                try
                {
                    try
                    {
                        method = property.GetMethodOnOwner(attribute.methodPath);
                        MethodHasParameters = false;
                        ErrorMessage = null;
                    }
                    catch
                    {
                        method = property.GetMethodOnOwner(attribute.methodPath, new Type[] { propertyType, propertyType });
                        MethodHasParameters = true;
                        ErrorMessage = null;
                    }
                }
                catch (MissingMethodException e)
                {
                    ErrorMessage = e.Message + " or without parameters";
                    return;
                }
                catch (Exception e)
                {
                    ErrorMessage = e.Message;
                    return;
                }

                if (!MethodHasParameters && attribute.useHookOnly)
                {
                    ErrorMessage = $"HookAttribute: New inputs are not applied, because you set 'useHookOnly', " +
                            $"but your method on '{attribute.methodPath}' did not define the parameters {propertyType} oldValue, {propertyType} newValue";
                    return;
                }

                Func<bool> ifExecute = attribute.target switch
                {
                    ExecutionTarget.Always => () => true,
                    ExecutionTarget.IsPlaying => () => Application.isPlaying,
                    ExecutionTarget.IsNotPlaying => () => !Application.isPlaying,
                    _ => throw new NotImplementedException(attribute.target.ToString()),
                };


                if (MethodHasParameters)
                {
                    HookMethod = (p, o, n) =>
                    {
                        if (ifExecute())
                        {
                            try
                            {
                                p.CallMethodOnOwner(attribute.methodPath, new Type[] { propertyType, propertyType }, new object[] { o, n });
                            }
                            catch (Exception e)
                            {
                                Debug.LogException(e);
                            }
                        }
                    };
                }
                else
                {
                    HookMethod = (p, o, n) =>
                    {
                        if (ifExecute())
                        {
                            try
                            {
                                p.CallMethodOnOwner(attribute.methodPath);
                            }
                            catch (Exception e)
                            {
                                Debug.LogException(e);
                            }
                        }
                    };
                }
            }
        }
    }
}
