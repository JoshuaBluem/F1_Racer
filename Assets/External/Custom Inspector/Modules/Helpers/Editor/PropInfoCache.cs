using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace CustomInspector.Helpers.Editor
{
    public class PropInfoCache<PropInfo> where PropInfo : ICachedPropInfo, new()
    {
        readonly Dictionary<PropertyIdentifier, PropInfo> infos = new();

        public PropInfo GetInfo(SerializedProperty property, PropertyAttribute attribute, FieldInfo fieldInfo)
        {
            PropertyIdentifier id = new(property);
            if (!infos.TryGetValue(id, out PropInfo info))
            {
                info = new PropInfo();
                info.Initialize(property, attribute, fieldInfo);
                infos.Add(id, info);
            }
            return info;
        }
    }
    public class ExactPropInfoCache<PropInfo> where PropInfo : ICachedPropInfo, new()
    {
        readonly Dictionary<ExactPropertyIdentifier, PropInfo> infos = new();

        public PropInfo GetInfo(SerializedProperty property, PropertyAttribute attribute, FieldInfo fieldInfo)
        {
            ExactPropertyIdentifier id = new(property);
            if (!infos.TryGetValue(id, out PropInfo info))
            {
                info = new PropInfo();
                info.Initialize(property, attribute, fieldInfo);
                infos.Add(id, info);
            }
            return info;
        }
    }

    public interface ICachedPropInfo
    {
        void Initialize(SerializedProperty property, PropertyAttribute attribute, FieldInfo fieldInfo);
    }
}
