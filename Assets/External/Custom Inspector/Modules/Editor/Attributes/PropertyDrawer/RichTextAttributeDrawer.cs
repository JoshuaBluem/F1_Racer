using CustomInspector.Extensions;
using CustomInspector.Helpers;
using CustomInspector.Helpers.Editor;
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace CustomInspector.Editor
{
    [CustomPropertyDrawer(typeof(RichTextAttribute))]
    public class RichTextAttributeDrawer : PropertyDrawer
    {
        const float toggleWidth = 17;

        static Vector2 scrollPos = Vector2.zero;
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            PropInfo styleInfo = cache.GetInfo(property, attribute, fieldInfo);


            if (property.propertyType != SerializedPropertyType.String)
            {
                DrawProperties.DrawPropertyWithMessage(position, label, property, $"{nameof(RichTextAttribute)} only valid on strings.", MessageType.Error);
                return;
            }

            Rect labelRect = new(position)
            {
                width = EditorGUIUtility.labelWidth,
                height = EditorGUIUtility.singleLineHeight,
            };
            if (label.text == "")
            {
                //still show the foldout that uses space, if no space (position.x) is available
                labelRect.width = Mathf.Clamp(18 - position.x, 0, 18);
            }

            //The boolean below
            Rect infoRect = new(position)
            {
                height = EditorGUIUtility.singleLineHeight
            };
            infoRect.y = position.y + position.height - infoRect.height - EditorGUIUtility.standardVerticalSpacing;


            Rect textRect = new()
            {
                x = labelRect.x + labelRect.width + 2,
                y = position.y,
                width = position.width - labelRect.width,
                height = position.height,
            };

            styleInfo.lastWidth = Mathf.Max(textRect.width, 10);

            if (property.isExpanded) //draw info
                textRect.height = position.height - (infoRect.height + EditorGUIUtility.standardVerticalSpacing);

            property.isExpanded = EditorGUI.Foldout(labelRect, property.isExpanded, label);


            EditorGUI.BeginChangeCheck();
            using (new NewIndentLevel(0))
            {
                GUIStyle gUIStyle = styleInfo.GetGUIStyle(property);
                float neededHeight = gUIStyle.CalcHeight(new GUIContent(property.stringValue), textRect.width - Common.scrollbarThickness);

                if (neededHeight <= textRect.height)
                {
                    property.stringValue = styleInfo.preventEnterConfirm ?
                                    EditorGUI.TextArea(textRect, property.stringValue, gUIStyle)
                                    : EditorGUI.TextField(textRect, property.stringValue, gUIStyle);
                }
                else
                {
                    Rect innerPosition = new Rect(0, 0, textRect.width - Common.scrollbarThickness, neededHeight); //position in the scrollbar

                    using (var scrollScope = new GUI.ScrollViewScope(textRect, scrollPos, innerPosition))
                    {
                        scrollPos = scrollScope.scrollPosition;

                        property.stringValue = styleInfo.preventEnterConfirm ?
                                        EditorGUI.TextArea(innerPosition, property.stringValue, gUIStyle)
                                        : EditorGUI.TextField(innerPosition, property.stringValue, gUIStyle);
                    }
                }
            }
            if (EditorGUI.EndChangeCheck())
            {
                property.serializedObject.ApplyModifiedProperties();
            }

            if (property.isExpanded)
            {
                using (new EditorGUI.IndentLevelScope(1))
                    property.isExpanded = !EditorGUI.Toggle(infoRect, new GUIContent("Use Rich text", "Richttext is currently disabled for this textfield. Click the toggle to active it."), !property.isExpanded);
            }
        }
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
                return DrawProperties.GetPropertyWithMessageHeight(label, property);

            PropInfo propInfo = cache.GetInfo(property, attribute, fieldInfo);
            string[] contentLines = property.stringValue.Split('\n');

            string[] filled;
            if (contentLines.Length < propInfo.minLines)
            {
                filled = new string[propInfo.minLines];
                Array.Copy(contentLines, filled, contentLines.Length);
            }
            else if (contentLines.Length > propInfo.maxLines)
            {
                filled = new string[propInfo.maxLines];
                Array.Copy(contentLines, filled, propInfo.maxLines);
            }
            else
                filled = contentLines;

            GUIStyle gUIStyle = propInfo.GetGUIStyle(property);
            float height = gUIStyle.CalcHeight(new GUIContent(string.Join('\n', filled)), propInfo.lastWidth);
            if (height > 1000) //cap, if the user mad a mistake
                height = 1000;
            if (property.isExpanded)
                height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            return height;
        }

        readonly static PropInfoCache<PropInfo> cache = new();

        class PropInfo : ICachedPropInfo
        {
            /// <summary>
            /// If pressing enter should insert '\n' instead of confirming selection
            /// </summary>
            public bool preventEnterConfirm { get; private set; } = false;
            /// <summary>
            /// If a scrollbar should be added when not all text is showing
            /// </summary>
            public bool insertScrollBar { get; private set; } = false;
            /// <summary>
            /// If words should be moved in next line if available width is too low
            /// </summary>
            public bool wordWrap { get; private set; } = false;
            /// <summary>
            /// Min and max height of input-box measured in lines
            /// </summary>
            public int minLines { get; private set; } = 1;
            public int maxLines { get; private set; } = 1; //keep in mind that a line-height's can vary because of richText - we just take the first n lines as the height


            public float lastWidth = 200;

            public PropInfo() { }
            public void Initialize(SerializedProperty property, PropertyAttribute attribute, FieldInfo fieldInfo)
            {
                var multilineA = fieldInfo.GetCustomAttribute<MultilineAttribute>();
                if (multilineA != null)
                {
                    wordWrap = false;
                    preventEnterConfirm = true;
                    minLines = maxLines = multilineA.lines;
                    return;
                }

                var textAreaA = fieldInfo.GetCustomAttribute<TextAreaAttribute>();
                if (textAreaA != null)
                {
                    wordWrap = true;
                    preventEnterConfirm = true;
                    minLines = textAreaA.minLines;
                    maxLines = textAreaA.maxLines;
                    return;
                }
            }
            public GUIStyle GetGUIStyle(SerializedProperty property)
            {
                return new(GUI.skin.textField)
                {
                    richText = !property.isExpanded,
                    wordWrap = wordWrap,
                };
            }
        }
        class ExactPropertyIdentifier //should also differentiate between two instances of same object shown in two inspector windows (because we store inspector-width)
        {
            readonly SerializedObject targetObject;
            readonly string propertyPath;
            public ExactPropertyIdentifier(SerializedProperty property)
            {
                targetObject = property.serializedObject;
                propertyPath = property.propertyPath;
            }
            public override bool Equals(object o)
            {
                if (o is ExactPropertyIdentifier other)
                {
                    return targetObject == other.targetObject && propertyPath == other.propertyPath;
                }
                else return false;
            }
            public override int GetHashCode() => HashCode.Combine(targetObject, propertyPath);
        }
    }
}
