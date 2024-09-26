using CustomInspector.Extensions;
using CustomInspector.Helpers.Editor;
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace CustomInspector.Editor
{
    public abstract class MinMaxAttributeDrawer : PropertyDrawer
    {
        public abstract int CapInt(int value, float cap);
        public abstract float CapFloat(float value, float cap);


        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var info = PropInfo.GetInfo(property, attribute, fieldInfo);
            IMinMaxAttribute mm = (IMinMaxAttribute)attribute;

            if (!string.IsNullOrEmpty(info.ErrorMessage))
            {
                DrawProperties.DrawPropertyWithMessage(position, label, property, info.ErrorMessage, MessageType.Error);
                return;
            }

            EditorGUI.BeginChangeCheck();
            DrawProperties.PropertyField(position, label, property);

            // do the capping
            if (mm.DependsOnOtherProperty() || EditorGUI.EndChangeCheck())
            {
                switch (property.propertyType)
                {
                    case SerializedPropertyType.Integer:
                        property.intValue = CapInt(property.intValue, info.Cap[0]);
                        break;
                    case SerializedPropertyType.Float:
                        property.floatValue = CapFloat(property.floatValue, info.Cap[0]);
                        break;

                    case SerializedPropertyType.Character:
                        property.intValue = CapInt(property.intValue, info.Cap[0]);
                        break;

                    case SerializedPropertyType.Vector2Int:
                        Vector2Int v2i = property.vector2IntValue;
                        property.vector2IntValue = new Vector2Int(CapInt(v2i.x, info.Cap[0]), CapInt(v2i.y, info.Cap[1]));
                        break;
                    case SerializedPropertyType.Vector2:
                        Vector2 v2 = property.vector2Value;
                        property.vector2Value = new Vector2(CapFloat(v2.x, info.Cap[0]), CapFloat(v2.y, info.Cap[1]));
                        break;

                    case SerializedPropertyType.Vector3Int:
                        Vector3Int v3i = property.vector3IntValue;
                        property.vector3IntValue = new Vector3Int(CapInt(v3i.x, info.Cap[0]), CapInt(v3i.y, info.Cap[1]), CapInt(v3i.z, info.Cap[2]));
                        break;
                    case SerializedPropertyType.Vector3:
                        Vector3 v3 = property.vector3Value;
                        property.vector3Value = new Vector3(CapFloat(v3.x, info.Cap[0]), CapFloat(v3.y, info.Cap[1]), CapFloat(v3.z, info.Cap[2]));
                        break;

                    case SerializedPropertyType.Vector4:
                        Vector4 v4 = property.vector4Value;
                        property.vector4Value = new Vector4(CapFloat(v4.x, info.Cap[0]), CapFloat(v4.y, info.Cap[1]), CapFloat(v4.z, info.Cap[2]), CapFloat(v4.w, info.Cap[3]));
                        break;

                    default:
                        Debug.LogError($"Min/Max attribute is not valid for {property.serializedObject.targetObject.name}.{property.propertyPath}.\nIt seems to not be a number (or vector)");
                        break;
                }
                property.serializedObject.ApplyModifiedProperties();
            }
        }
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {

            var info = PropInfo.GetInfo(property, attribute, fieldInfo);

            if (string.IsNullOrEmpty(info.ErrorMessage))
                return DrawProperties.GetPropertyHeight(label, property);
            else
                return DrawProperties.GetPropertyWithMessageHeight(label, property);
        }
        // not saved as cache because values always change, so this is actually no cache :)
        //readonly ExactPropInfoCache<PropInfo> cache = new();

        class PropInfo : ICachedPropInfo
        {
            /// <summary>
            /// The min or max values (component-wise)
            /// </summary>
            public float[] Cap { get; private set; }
            public string ErrorMessage { get; private set; }
            public void Initialize(SerializedProperty property, PropertyAttribute attribute, FieldInfo fieldInfo)
            {
                var mm = (IMinMaxAttribute)attribute;
                if (string.IsNullOrEmpty(mm.CapPath))
                {
                    SetSingleCap(mm.CapValue);
                }
                else
                {
                    object max_Value = DirtyValue.GetOwner(property).FindRelative(mm.CapPath).GetValue();
                    if (max_Value is System.Single s)
                    {
                        SetSingleCap(s);
                    }
                    else if (max_Value is Vector2 v2)
                    {
                        Cap = new[] { v2.x, v2.y };
                    }
                    else if (max_Value is Vector2Int v2i)
                    {
                        Cap = new float[] { v2i.x, v2i.y };
                    }
                    else if (max_Value is Vector3 v3)
                    {
                        Cap = new[] { v3.x, v3.y, v3.z };
                    }
                    else if (max_Value is Vector3Int v3i)
                    {
                        Cap = new float[] { v3i.x, v3i.y, v3i.z };
                    }
                    else if (max_Value is Vector4 v4)
                    {
                        Cap = new[] { v4.x, v4.y, v4.z, v4.w };
                    }
                    else
                    {

                        try
                        {
                            float casted = System.Convert.ToSingle(max_Value); //maybe it is provided as string like "7"
                            SetSingleCap(casted);
                        }
                        catch (Exception e)
                        {
                            ErrorMessage = $"{max_Value.GetType()} could not be read as a number (or vector).\n" + e.Message;
                        }
                    }
                }

                void SetSingleCap(float cap)
                {
                    if (property.propertyType == SerializedPropertyType.Integer
                      || property.propertyType == SerializedPropertyType.Float
                      || property.propertyType == SerializedPropertyType.Character)
                    {
                        Cap = new[] { cap };
                    }
                    else if (property.propertyType == SerializedPropertyType.Vector2
                      || property.propertyType == SerializedPropertyType.Vector2Int)
                    {
                        Cap = new[] { cap, cap };
                    }
                    else if (property.propertyType == SerializedPropertyType.Vector3
                      || property.propertyType == SerializedPropertyType.Vector3Int)
                    {
                        Cap = new[] { cap, cap, cap };
                    }
                    else if (property.propertyType == SerializedPropertyType.Vector4)
                    {
                        Cap = new[] { cap, cap, cap, cap };
                    }
                    else
                    {
                        ErrorMessage = $"Property '{property.name}' cannot be capped, because it could not be read as a number (or vector)";
                    }
                }
            }
            public static PropInfo GetInfo(SerializedProperty property, PropertyAttribute attribute, FieldInfo fieldInfo)
            {
                PropInfo pi = new();
                pi.Initialize(property, attribute, fieldInfo);
                return pi;
            }
        }
    }

    [CustomPropertyDrawer(typeof(MaxAttribute))]
    public class MaxAttributeDrawer : MinMaxAttributeDrawer
    {
        public override int CapInt(int value, float cap) => Math.Min(value, (int)cap);
        public override float CapFloat(float value, float cap) => Mathf.Min(value, cap);
    }


    [CustomPropertyDrawer(typeof(Min2Attribute))]
    public class Min2AttributeDrawer : MinMaxAttributeDrawer
    {
        public override int CapInt(int value, float cap) => Math.Max(value, (int)cap);
        public override float CapFloat(float value, float cap) => Mathf.Max(value, cap);
    }
}

