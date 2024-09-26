using CustomInspector.Extensions;
using CustomInspector.Helpers;
using CustomInspector.Helpers.Editor;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CustomInspector.Editor
{
    [CustomPropertyDrawer(typeof(FromChildrenAttribute))]
    public class FromChildrenAttributeDrawer : PropertyDrawer
    {
        bool HasWrongValue(SerializedProperty property) => property.objectReferenceValue == null && property.isExpanded;
        void SetWrongValue(SerializedProperty property, bool value) => property.isExpanded = value;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginChangeCheck();

            PropInfo propInfo = cache.GetInfo(property, attribute, fieldInfo);
            if (!string.IsNullOrEmpty(propInfo.ErrorMessage))
            {
                DrawProperties.DrawPropertyWithMessage(position,
                                                        label,
                                                        property,
                                                        propInfo.ErrorMessage,
                                                        MessageType.Error);
                return;
            }


            FromChildrenAttribute fca = (FromChildrenAttribute)attribute;
            Object value = property.objectReferenceValue; //references can be gameObject, component or material
            //No value
            if (value == null)
            {
                if (HasWrongValue(property))
                {
                    //null, because value had to be deleted

                    position = EditorGUI.IndentedRect(position);
                    float buttonWidth = 25;
                    position.y += DrawProperties.messageBoxStartSpacing;
                    position.height -= DrawProperties.messageBoxStartSpacing;

                    Rect messageRect = new(position)
                    {
                        width = position.width - buttonWidth - 5,
                        height = DrawProperties.messageBoxHeight,
                    };
                    Rect buttonRect = new(messageRect)
                    {
                        x = messageRect.x + messageRect.width + 5,
                        width = buttonWidth
                    };
                    position.height -= messageRect.height + DrawProperties.messageBoxEndSpacing;
                    position.y += messageRect.height + DrawProperties.messageBoxEndSpacing;

                    using (new NewIndentLevel(0))
                    {
                        MessageType mt = fca.allowNull ? MessageType.Warning : MessageType.Error;
                        EditorGUI.HelpBox(messageRect,
                            $"FromChildrenAttribute: Reference to '{property.name}' must be from this gameobjects children.",
                            mt);


                        if (GUI.Button(buttonRect, new GUIContent("X")))
                        {
                            SetWrongValue(property, false);
                        }

                        DrawProperties.PropertyField(position, label, property);
                    }

                }
                else
                {
                    //just not filled
                    if (!fca.allowNull)
                    {
                        DrawProperties.DrawPropertyWithMessage(position,
                                label,
                                property,
                                $"FromChildrenAttribute: Reference to '{property.name}' missing (null)." +
                                    "\nReference must point to an object on this gameobjects children.",
                                MessageType.Error);
                        return;
                    }
                    else
                    {
                        DrawProperties.PropertyField(position, label, property);
                        return;
                    }
                }
            }
            //There is a value
            else
            {
                Object[] matchingFromChild = propInfo.Matchings;

                // Debug.Log(string.Join(", ", matchingFromChild.Select(c => c.name)));

                if (matchingFromChild.Any(v => v == value))
                {
                    DrawProperties.PropertyField(position, label, property);
                    SetWrongValue(property, false);
                }
                else
                {
                    SetWrongValue(property, true);
                    Debug.LogWarning(nameof(FromChildrenAttribute) + $": Value '{property.objectReferenceValue}' removed at '{property.serializedObject.targetObject.name}.{property.propertyPath}', " +
                                            $"because it was not from '{((Component)property.serializedObject.targetObject).gameObject.name}'s children");
                    property.objectReferenceValue = null;
                    property.serializedObject.ApplyModifiedProperties();
                }
            }
            if (EditorGUI.EndChangeCheck())
                property.serializedObject.ApplyModifiedProperties();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.ObjectReference)
            {
                return DrawProperties.GetPropertyWithMessageHeight(label, property);
            }

            if (property.objectReferenceValue == null)
            {
                FromChildrenAttribute fca = (FromChildrenAttribute)attribute;
                if (HasWrongValue(property))
                    return DrawProperties.GetPropertyWithMessageHeight(label, property);
                else if (!fca.allowNull)
                    return DrawProperties.GetPropertyHeight(label, property) + DrawProperties.messageBoxHeight + DrawProperties.messageBoxStartSpacing + DrawProperties.messageBoxEndSpacing;
                else
                    return DrawProperties.GetPropertyHeight(label, property);
            }
            else
            {
                return DrawProperties.GetPropertyHeight(label, property);
            }
        }

        /*static*/
        readonly ExactPropInfoCache<PropInfo> cache = new();

        class PropInfo : ICachedPropInfo
        {
            public Object[] Matchings { get; private set; }
            public string ErrorMessage { get; private set; }

            public void Initialize(SerializedProperty property, PropertyAttribute attribute, FieldInfo fieldInfo)
            {
                if (property.propertyType != SerializedPropertyType.ObjectReference)
                {
                    ErrorMessage = "FromChildrenAttribute is only valid for References";
                    return;
                }

                if (property.serializedObject.targetObject is Component component)
                {
                    var propType = DirtyValue.GetType(property);
                    if (propType != typeof(GameObject))
                        Matchings = component.GetComponentsInChildren(propType, true).Except(component.GetComponents(propType)).ToArray();
                    else
                        Matchings = component.transform.GetAllChildren().Select(transform => transform.gameObject).ToArray();
                }
                else
                {
                    if (property.serializedObject.targetObject is ScriptableObject)
                    {
                        ErrorMessage = "FromChildrenAttribute only works on GameObjects." +
                                       $"\nScriptableObjects do not have children.";
                    }
                    else
                    {
                        ErrorMessage = "FromChildrenAttribute only works on GameObjects." +
                                       $"\n'{property.serializedObject.targetObject.GetType()}' does not have children.";
                    }

                    return;
                }
            }
        }
    }
}
