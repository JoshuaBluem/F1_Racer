using CustomInspector.Extensions;
using CustomInspector.Helpers.Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace CustomInspector.Editor
{
    [CustomPropertyDrawer(typeof(AnimatorParameterAttribute))]
    public class AnimatorParameterAttributeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            PropInfo info = cache.GetInfo(property, attribute, fieldInfo);
            if (info.ErrorMessage != null)
            {
                DrawProperties.DrawPropertyWithMessage(position, label, property, info.ErrorMessage, MessageType.Error);
            }
            else
            {
                var apa = (AnimatorParameterAttribute)attribute;

                List<string> paramNames = info.ParameterNames(property);
                if (paramNames == null)
                {
                    property.stringValue = "<missing>";
                    DrawProperties.DrawPropertyWithMessage(position,
                                                            label,
                                                            property,
                                                            $"AnimatorParameter: Animator or AnimatorController on {apa.animatorPath} is null. Please fill it in the inspector",
                                                            MessageType.Error,
                                                            disabled: true);
                }
                else if (paramNames.Any())
                {
                    int index = paramNames.IndexOf(property.stringValue);
                    if (index == -1) //not found
                    {
                        index = 0;
                        property.stringValue = paramNames[0];
                        property.serializedObject.ApplyModifiedProperties();
                    }

                    EditorGUI.BeginChangeCheck();
                    index = EditorGUI.Popup(position, label, index, paramNames.Select(name => new GUIContent(name)).ToArray());
                    if (EditorGUI.EndChangeCheck())
                    {
                        property.stringValue = paramNames[index];
                        property.serializedObject.ApplyModifiedProperties();
                    }
                }
                else //no params existing
                {
                    property.stringValue = "<invalid>";
                    DrawProperties.DrawPropertyWithMessage(position,
                                                            label,
                                                            property,
                                                            $"AnimatorParameter: {apa.animatorPath} has no animator parameters.",
                                                            MessageType.Error,
                                                            disabled: true);
                }
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            PropInfo info = cache.GetInfo(property, attribute, fieldInfo);
            if (info.ErrorMessage != null)
            {
                return DrawProperties.GetPropertyWithMessageHeight(label, property);
            }
            else
            {
                var paramNames = info.ParameterNames(property);
                if (paramNames != null && paramNames.Any())
                    return EditorGUIUtility.singleLineHeight;
                else
                    return DrawProperties.GetPropertyWithMessageHeight(label, property);
            }
        }

        readonly static PropInfoCache<PropInfo> cache = new();

        class PropInfo : ICachedPropInfo
        {
            public string ErrorMessage { get; private set; }
            public Func<SerializedProperty, List<string>> ParameterNames { get; private set; } = null;

            public PropInfo() { }
            public void Initialize(SerializedProperty property, PropertyAttribute attr, FieldInfo fieldInfo)
            {
                AnimatorParameterAttribute attribute = (AnimatorParameterAttribute)attr;

                SerializedProperty prop;
                PropertyValues.IFindProperties owner = property.GetOwnerAsFinder();
                try
                {
                    prop = owner.FindPropertyRelative(attribute.animatorPath);
                }
                catch (Exception e)
                {
                    ErrorMessage = e.Message;
                    return;
                }
                if (prop == null)
                {
                    ErrorMessage = $"AnimatorParameter: {attribute.animatorPath} was not found on {owner.Name}";
                    return;
                }

                Type type = DirtyValue.GetType(prop);
                if (type == typeof(Animator))
                {
                    ParameterNames = p =>
                    {
                        SerializedProperty animator = p.GetOwnerAsFinder().FindPropertyRelative(attribute.animatorPath);
                        Animator value = animator.GetValue() as Animator;
                        if (value != null)
                            return value.parameters?.Select(p => p.name).ToList(); //they are null no controller is assigned
                        else
                            return null;
                    };
                }
                else if (type == typeof(AnimatorController))
                {
                    ParameterNames = p =>
                    {
                        SerializedProperty animatorController = p.GetOwnerAsFinder().FindPropertyRelative(attribute.animatorPath);
                        AnimatorController value = animatorController.GetValue() as AnimatorController;
                        if (value != null)
                            return value.parameters.Select(p => p.name).ToList();
                        else
                            return null;
                    };
                }
                else
                {
                    ErrorMessage = $"AnimatorParameter: Type {type} is invalid. {attribute.animatorPath} in {owner.Name} must be of type Animator or AnimatorController";
                }
            }
        }
    }
}
