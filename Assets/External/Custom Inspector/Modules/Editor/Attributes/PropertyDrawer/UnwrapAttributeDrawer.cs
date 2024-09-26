using CustomInspector.Extensions;
using CustomInspector.Helpers.Editor;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace CustomInspector.Editor
{
    [CustomPropertyDrawer(typeof(UnwrapAttribute))]
    public class UnwrapAttributeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            PropInfo info = cache.GetInfo(property, attribute, fieldInfo);

            if (info.ErrorMessage != null)
            {
                DrawProperties.DrawPropertyWithMessage(position, label, property, info.ErrorMessage, MessageType.Error);
                return;
            }

            IEnumerable<SerializedProperty> props = info.ChildrenPaths.Select(_ => property.serializedObject.FindProperty(_));

            UnwrapAttribute u = (UnwrapAttribute)attribute;
            string prefix = (u.applyName && label?.text != null) ? $"{label.text}: " : "";
            EditorGUI.BeginChangeCheck();
            foreach (var prop in props)
            {
                position.height = DrawProperties.GetPropertyHeight(prop);
                DrawProperties.PropertyField(position, property: prop, label: new GUIContent(prefix + prop.name, prop.tooltip));
                position.y += position.height + EditorGUIUtility.standardVerticalSpacing;
            }
            if (EditorGUI.EndChangeCheck())
                property.serializedObject.ApplyModifiedProperties();
        }
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            PropInfo info = cache.GetInfo(property, attribute, fieldInfo);

            if (info.ErrorMessage != null)
            {
                return DrawProperties.GetPropertyWithMessageHeight(label, property);
            }

            IEnumerable<SerializedProperty> props = info.ChildrenPaths.Select(_ => property.serializedObject.FindProperty(_));

            return props.Select(_ => DrawProperties.GetPropertyHeight(_) + EditorGUIUtility.standardVerticalSpacing).Sum() - EditorGUIUtility.standardVerticalSpacing;
        }

        /// <summary>
        /// We save them for performance reasons
        /// </summary>
        static readonly PropInfoCache<PropInfo> cache = new();
        class PropInfo : ICachedPropInfo
        {
            public string ErrorMessage { get; private set; }
            public string[] ChildrenPaths { get; private set; }

            public PropInfo() { }
            public void Initialize(SerializedProperty property, PropertyAttribute attribute, FieldInfo fieldInfo)
            {
                if (property.propertyType != SerializedPropertyType.Generic)
                {
                    ErrorMessage = $"{nameof(UnwrapAttribute)} only valid on Generic's (a serialized class)." +
                                   $"\nNote: Attributes on {typeof(List<>).FullName} are applied to the elements.";
                    return;
                }

                ErrorMessage = null;
                ChildrenPaths = property.GetAllVisibleProperties(true).Select(_ => _.propertyPath).ToArray();
            }
        }
    }
}

