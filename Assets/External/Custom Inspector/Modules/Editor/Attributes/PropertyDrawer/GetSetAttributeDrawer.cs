using CustomInspector.Extensions;
using CustomInspector.Helpers.Editor;
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace CustomInspector.Editor
{

    [CustomPropertyDrawer(typeof(GetSetAttribute))]
    public class GetSetAttributeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            GetSetAttribute sm = (GetSetAttribute)attribute;
            var info = cache.GetInfo(property, attribute, fieldInfo);

            if (!string.IsNullOrEmpty(info.ErrorMessage))
            {
                DrawProperties.DrawPropertyWithMessage(position, label, property, info.ErrorMessage, MessageType.Error);
                return;
            }

            object value;
            try
            {
                value = info.GetValue(property);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                DrawProperties.DrawPropertyWithMessage(position, label, property, "Error in Get-Function! See console for more information.", MessageType.Error);
                return;
            }

            //Draw Value
            Rect getRect = new(position)
            {
                height = DrawProperties.GetPropertyHeight(PropertyConversions.ToPropertyType(info.DisplayedType), label),
            };

            EditorGUI.BeginChangeCheck();
            object res = DrawProperties.DrawField(position: getRect, label: info.Label, value: value, info.DisplayedType);
            if (EditorGUI.EndChangeCheck())
            {
                //call setter
                try
                {
                    info.SetValue(property, res);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }

            //Draw Property below
            if (!info.HideActualProperty)
            {
                Rect propRect = new(position)
                {
                    y = position.y + getRect.height + EditorGUIUtility.standardVerticalSpacing,
                    height = DrawProperties.GetPropertyHeight(label, property),
                };
                EditorGUI.BeginChangeCheck();
                DrawProperties.PropertyField(propRect, label, property);
                if (EditorGUI.EndChangeCheck())
                    property.serializedObject.ApplyModifiedProperties();
            }
        }
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var info = cache.GetInfo(property, attribute, fieldInfo);

            //check for errors
            if (!string.IsNullOrEmpty(info.ErrorMessage))
                return DrawProperties.GetPropertyWithMessageHeight(label, property);
            try
            {
                info.GetValue(property);
            }
            catch
            {
                return DrawProperties.GetPropertyWithMessageHeight(label, property);
            }

            //height
            if (info.HideActualProperty)
            {
                return DrawProperties.GetPropertyHeight(PropertyConversions.ToPropertyType(info.DisplayedType), label);
            }
            else
            {
                return DrawProperties.GetPropertyHeight(PropertyConversions.ToPropertyType(info.DisplayedType), label)
                    + DrawProperties.GetPropertyHeight(label, property);
            }
        }


        static readonly PropInfoCache<PropInfo> cache = new();
        class PropInfo : ICachedPropInfo
        {

            /// <summary>
            /// If not null, all other propertys on PropInfo are invalid
            /// </summary>
            public string ErrorMessage { get; private set; }
            /// <summary>
            /// Getter Returntype and what type the new field will be
            /// </summary>
            public Type DisplayedType { get; private set; }
            /// <summary>
            /// The displayed value
            /// </summary>
            public Func<SerializedProperty, object> GetValue { get; private set; }
            /// <summary>
            /// What can be used, if diplayed value was changed
            /// </summary>
            public Action<SerializedProperty, object> SetValue { get; private set; }
            /// <summary>
            /// Label of displayed value
            /// </summary>
            public GUIContent Label { get; private set; }
            /// <summary>
            /// If the property is passed into the getter and set from return value of setter
            /// </summary>
            public bool PassedIntoGetter { get; private set; }
            /// <summary>
            /// If the property is passed into the getter and set from return value of setter
            /// </summary>
            public bool PassFromSetter;
            /// <summary>
            /// If ONLY the getter-setter-field should be visible and not the actual property under it
            /// </summary>
            public bool HideActualProperty { get; private set; }

            public void Initialize(SerializedProperty property, PropertyAttribute attribute, FieldInfo fieldInfo)
            {
                GetSetAttribute gsa = (GetSetAttribute)attribute;

                //without params
                Type propertyType = fieldInfo.FieldType;

                InvokableMethod getterInstance;
                if (PropertyValues.TryGetMethodOnOwner(property, out getterInstance, gsa.getmethodPath))
                {
                    GetValue = (p) =>
                    {
                        p.serializedObject.ApplyModifiedPropertiesWithoutUndo();
                        return PropertyValues.CallMethodOnOwner(p, gsa.getmethodPath);
                    };
                }
                else
                {
                    Type[] propTypeArray = new Type[] { propertyType };

                    if (PropertyValues.TryGetMethodOnOwner(property, out getterInstance, gsa.getmethodPath, propTypeArray))
                    {
                        GetValue = (p) =>
                        {
                            p.serializedObject.ApplyModifiedPropertiesWithoutUndo();
                            return PropertyValues.CallMethodOnOwner(p, gsa.getmethodPath, propTypeArray, new object[] { p.GetValue() });
                        };
                    }
                    else
                    {
                        ErrorMessage = $"{nameof(GetSetAttribute)}: No method '{gsa.getmethodPath}' found on {property.serializedObject.targetObject.GetType()}" +
                                       $"\nwithout or with '{propertyType.Name}' as parameter";
                        return;
                    }
                }

                //check getters return type
                if (getterInstance.ReturnType == typeof(void))
                {
                    ErrorMessage = $"Get-Method {gsa.getmethodPath} doesnt have a return value";
                    return;
                }
                DisplayedType = getterInstance.ReturnType;

                //get setter with getters return type
                Type getterReturnType = getterInstance.ReturnType;
                Type[] getterReturnTypeArray = new Type[] { getterReturnType };

                if (PropertyValues.TryGetMethodOnOwner(property, out InvokableMethod setterInstance, gsa.setmethodPath, getterReturnTypeArray))
                {
                    if (getterInstance.ParameterCount() > 0
                        && setterInstance.ReturnType == propertyType)
                    {
                        HideActualProperty = true;
                        SetValue = (p, obj) =>
                        {
                            p.SetValue(PropertyValues.CallMethodOnOwner(p, gsa.setmethodPath, getterReturnTypeArray, new object[] { obj }));
                            p.serializedObject.ApplyModifiedProperties();
                        };
                    }
                    else
                    {
                        HideActualProperty = false;
                        SetValue = (p, obj) =>
                        {
                            PropertyValues.CallMethodOnOwner(p, gsa.setmethodPath, getterReturnTypeArray, new object[] { obj });
                            p.serializedObject.ApplyModifiedFields(true);
                        };
                    }
                }
                else
                {
                    ErrorMessage = $"{nameof(GetSetAttribute)}: No method '{gsa.setmethodPath}' found on {property.serializedObject.targetObject.GetType()}" +
                                   $"\nwith '{getterReturnType.Name}' as parameter";
                }

                //set label
                if (gsa.label is null)
                    Label = new(ShowMethodAttributeDrawer.TryGetNameOuttaGetter(getterInstance.Name), gsa.tooltip);
                else
                    Label = new(gsa.label, gsa.tooltip);
            }
        }
    }
}