using System;
using System.Diagnostics;
using UnityEngine;

namespace CustomInspector
{

    [Conditional("UNITY_EDITOR")]
    public class NullableAttribute : PropertyAttribute { }

    [Serializable]
    public struct SerializableNullable<T> where T : struct
    {
        [MessageBox("Nullable<T> is missing the [Nullable]-attribute to be correctly displayed.", MessageBoxType.Warning)]


        [SerializeField] private bool hasValue;
        public bool HasValue
        {
            get => hasValue;
            set => hasValue = value;
        }

        [SerializeField] private T value;
        public T Value
        {
            get => this.hasValue ? this.value : throw new InvalidOperationException("Nullable object must have a value.");
            set
            {
                this.value = value;
                this.hasValue = true;
            }
        }

        public SerializableNullable(T value)
        {
            hasValue = true;
            this.value = value;
        }
        public SerializableNullable(NullType _)
        {
            hasValue = false;
            this.value = default(T);
        }


        public static explicit operator T(SerializableNullable<T> value) => value.Value;

        public static implicit operator SerializableNullable<T>(T value) => new(value);
        public static implicit operator SerializableNullable<T>(NullType value) => new(value);

        public static explicit operator CustomInspector.SerializableNullable<T>(System.Nullable<T> value) => value.HasValue ? value.Value : null;
        public static explicit operator System.Nullable<T>(CustomInspector.SerializableNullable<T> value) => value.HasValue ? value.Value : null;

        public override readonly bool Equals(object obj)
        {
            if (obj is SerializableNullable<T> other)
            {
                return Equals(other);
            }
            return false;
        }
        public readonly bool Equals(SerializableNullable<T> other)
        {
            if (!this.hasValue)
                return !other.hasValue;

            return value.Equals(other.value);
        }
        public override readonly int GetHashCode()
        {
            if (!hasValue)
                return -1;
            return value.GetHashCode();
        }
        public override string ToString()
        {
            if (hasValue)
                return value.ToString();
            else
                return string.Empty;
        }

        public static bool operator ==(SerializableNullable<T> a, SerializableNullable<T> b) => a.Equals(b);
        public static bool operator !=(SerializableNullable<T> a, SerializableNullable<T> b) => !(a == b);

        public static bool operator ==(SerializableNullable<T> a, NullType b) => !a.hasValue;
        public static bool operator !=(SerializableNullable<T> a, NullType b) => a.hasValue;

        public static bool operator ==(NullType b, SerializableNullable<T> a) => a == b;
        public static bool operator !=(NullType b, SerializableNullable<T> a) => a != b;


        public class NullType { }
    }
}
