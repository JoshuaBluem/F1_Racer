using System;
using System.Collections.Generic;
using UnityEditor;

namespace CustomInspector.Helpers
{
    /// <summary>
    /// Identifies a property on a component.
    /// Two properties on different components cannot be differentiated!
    /// Used to save errors or other information on specific properties for performance through different OnGUI's.
    /// </summary>
    public class PropertyIdentifier
    {
        public readonly Type targetObjectType;
        public readonly string propertyPath;

        public PropertyIdentifier(SerializedProperty property)
        {
            this.targetObjectType = property.serializedObject.targetObject.GetType();
            this.propertyPath = property.propertyPath;
        }
        public PropertyIdentifier(Type targetObject, string fullPath)
        {
            this.targetObjectType = targetObject;
            this.propertyPath = fullPath;
        }



        public override bool Equals(object obj)
        {
            if (obj is PropertyIdentifier identifier)
            {
                return EqualityComparer<Type>.Default.Equals(targetObjectType, identifier.targetObjectType) &&
                        propertyPath == identifier.propertyPath;
            }
            return false;
        }
        public override int GetHashCode() => HashCode.Combine(targetObjectType, propertyPath);

        public static bool operator ==(PropertyIdentifier i1, PropertyIdentifier i2)
        {
            if (i1 is null)
                return i2 is null;
            else if (i2 is null)
                return false;

            return i1.targetObjectType == i2.targetObjectType
                && i1.propertyPath == i2.propertyPath;
        }

        public static bool operator !=(PropertyIdentifier i1, PropertyIdentifier i2)
        {
            if (i1 is null)
                return i2 is not null;
            else if (i2 is null)
                return true;

            return i1.targetObjectType != i2.targetObjectType
                || i1.propertyPath == i2.propertyPath;
        }
    }

    /// <summary>
    /// Identifies properties.
    /// Differentiates also between properties on different components
    /// </summary>
    class ExactPropertyIdentifier
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
                return targetObject == other.targetObject
                    && propertyPath == other.propertyPath;
            }
            else return false;
        }
        public override int GetHashCode() => HashCode.Combine(targetObject, propertyPath);

        public static bool operator ==(ExactPropertyIdentifier i1, ExactPropertyIdentifier i2)
        {
            if (i1 is null)
                return i2 is null;
            else if (i2 is null)
                return false;

            return i1.targetObject == i2.targetObject
                && i1.propertyPath == i2.propertyPath;
        }

        public static bool operator !=(ExactPropertyIdentifier i1, ExactPropertyIdentifier i2)
        {
            if (i1 is null)
                return i2 is not null;
            else if (i2 is null)
                return true;

            return i1.targetObject != i2.targetObject
                || i1.propertyPath == i2.propertyPath;
        }
    }
}
