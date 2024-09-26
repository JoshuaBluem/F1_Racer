using CustomInspector.Extensions;
using CustomInspector.Helpers.Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CustomInspector.Editor
{
    [CustomPropertyDrawer(typeof(FoldoutAttribute))]
    public class FoldoutAttributeDrawer : PropertyDrawer
    {
        /// <summary> How many classes can be nested in each other. As a safety to stackOverflow if it references itself </summary>
        [Min(1)] const int maxRecursion = 7; //7 is also unitys default max depth: Edit > Project Settings > Editor > Inspector> Deep Inspection
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            using (var rs = new RecursionScope())
            {
                if (rs.CurrentDepth >= maxRecursion)
                {
                    return;
                }

                var info = cache.GetInfo(property, attribute, fieldInfo);
                if (info.ErrorMessage != null)
                {
                    DrawProperties.DrawPropertyWithMessage(position, label, property, info.ErrorMessage, MessageType.Error);
                    return;
                }

                Debug.Assert(property.propertyType == SerializedPropertyType.ObjectReference, "Foldout: Expected ObjectReference."); //this should have tested in cached infos

                //Draw current
                Rect holdersRect = new(position)
                {
                    height = DrawProperties.GetPropertyHeight(label, property)
                };

                Object value = property.objectReferenceValue;

                if (value == null) //nothing to foldout, bec its null
                {
                    DrawProperties.PropertyField(position, label, property);
                    property.isExpanded = false;
                    return;
                }

                DrawProperties.PropertyFieldWithFoldout(holdersRect, label, property);

                //Draw Members
                if (!property.isExpanded)
                    return;

                using (new EditorGUI.IndentLevelScope(1))
                {
                    Rect membersRect = EditorGUI.IndentedRect(position);
                    using (new NewIndentLevel(0))
                    {
                        membersRect.y = holdersRect.y + holdersRect.height + EditorGUIUtility.standardVerticalSpacing;
                        membersRect.height = position.height - holdersRect.height - EditorGUIUtility.standardVerticalSpacing;


                        DrawMembers(membersRect, value);
                    }
                }


                void DrawMembers(Rect position, Object displayedObject)
                {
                    Debug.Assert(displayedObject != null, "No Object found to draw members on.");
                    using (SerializedObject serializedObject = new(displayedObject))
                    {
                        var names = GetPropNames(serializedObject, displayedObject.GetType());
                        if (!names.Any())
                        {
                            Debug.LogWarning(NoPropsWarning(displayedObject));
                            property.isExpanded = false;
                            return;
                        }
                        EditorGUI.BeginChangeCheck();
                        foreach (var propertyPath in names)
                        {
                            SerializedProperty p = serializedObject.FindProperty(propertyPath);
                            position.height = DrawProperties.GetPropertyHeight(new GUIContent(p.name, p.tooltip), p);
                            string propName = PropertyConversions.NameFormat(p.name);
                            try
                            {
                                DrawProperties.PropertyField(position, new GUIContent(propName, p.tooltip), p);
                            }
                            catch
                            {
                                Debug.LogError(nameof(FoldoutAttribute) + ": Recursive Overflow\n" + //that should be the only error that can occur
                                                    "Trying to foldout a class that is already visible.");
                                p.isExpanded = false;
                                throw;
                            }
                            position.y += position.height + EditorGUIUtility.standardVerticalSpacing;
                        }
                        if (EditorGUI.EndChangeCheck())
                            serializedObject.ApplyModifiedProperties();
                    }
                }
            }
        }
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            using (var rs = new RecursionScope())
            {
                if (rs.CurrentDepth >= maxRecursion)
                {
                    return 0;
                }

                var info = cache.GetInfo(property, attribute, fieldInfo);
                if (info.ErrorMessage != null)
                    return DrawProperties.GetPropertyWithMessageHeight(label, property);

                float currentHeight = DrawProperties.GetPropertyHeight(label, property);

                if (property.isExpanded && property.objectReferenceValue != null)
                    currentHeight += GetMembersHeight(property.objectReferenceValue);

                return currentHeight;


                float GetMembersHeight(Object displayedObject)
                {
                    Debug.Assert(displayedObject != null, "No Object found to search members on.");
                    using (SerializedObject serializedObject = new(displayedObject))
                    {
                        var names = GetPropNames(serializedObject, displayedObject.GetType());
                        try
                        {
                            return names.Select(path => serializedObject.FindProperty(path))
                                        .Select(p => DrawProperties.GetPropertyHeight(new GUIContent(p.name, p.tooltip), p))
                                        .Sum(x => x + EditorGUIUtility.standardVerticalSpacing);
                        }
                        catch (Exception e)
                        {
                            Debug.LogWarning(e);
                            return 0;
                        }
                    }
                }
            }
        }
        readonly static Dictionary<Type, string[]> propNames = new();
        readonly static PropInfoCache<ErrorInfos> cache = new();

        string[] GetPropNames(SerializedObject serializedObject, Type serializedObjectType)
        {
            if (serializedObject == null)
                throw new ArgumentException("Cannot retrieve memebers of null object");
            if (!propNames.TryGetValue(serializedObjectType, out string[] names))
            {
                names = serializedObject.GetAllVisibleProperties(true).Select(x => x.propertyPath).ToArray();
                propNames.Add(serializedObjectType, names);
            }
            return names;
        }
        class ErrorInfos : ICachedPropInfo
        {
            public string ErrorMessage { get; private set; } = null;

            public ErrorInfos() { }
            public void Initialize(SerializedProperty property, PropertyAttribute attribute, FieldInfo fieldInfo)
            {
                Type propType = DirtyValue.GetType(property);

                if (property.propertyType == SerializedPropertyType.Generic) //already has a foldout
                {
                    if (propType.IsGenericType && propType.GetGenericTypeDefinition() == typeof(ListContainer<>))
                        ErrorMessage = $"{nameof(FoldoutAttribute)}: ListContainer is already generic.\nTo apply attributes to elements, use the default List instead.";
                    else
                        ErrorMessage = $"{nameof(FoldoutAttribute)}: {propType} is already generic causing property to already foldout.";
                    return;
                }
                else if (property.propertyType != SerializedPropertyType.ObjectReference)
                {
                    ErrorMessage = $"{nameof(FoldoutAttribute)}'s supported type is only ObjectReference and not {property.propertyType}";
                    return;
                }
                if (!typeof(Object).IsAssignableFrom(propType))
                {
                    ErrorMessage = $"{nameof(FoldoutAttribute)} is only available on UnityEngine.Object 's";
                    return;
                }
            }
        }
        static string NoPropsWarning(Object target)
        {
            Type type = target.GetType();
            return nameof(FoldoutAttribute) + $": No properties found on {target.name} -> {type}." +
                                    $"\nPlease open the '{type}' script and make sure properties are public and serializable." +
                                    "\nPrivates can be serialized with the [SerializeField] attribute.";
        }
        /// <summary>
        /// Used to check if there is to big recursion going on. If a class displays a reference to itself it would cause a stackoverflow that crashes unity
        /// </summary>
        class RecursionScope : IDisposable
        {
            public int CurrentDepth => currentDepth;
            static int currentDepth = 0;
            public RecursionScope()
            {
                currentDepth++;
            }
            public void Dispose()
            {
                currentDepth--;
            }
        }
    }
}
