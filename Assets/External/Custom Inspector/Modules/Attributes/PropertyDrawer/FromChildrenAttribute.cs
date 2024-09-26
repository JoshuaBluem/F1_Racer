using System;
using System.Diagnostics;
using UnityEngine;

namespace CustomInspector
{
    /// <summary>
    /// Forces filled references to be only from children
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    [Conditional("UNITY_EDITOR")]
    public class FromChildrenAttribute : PropertyAttribute
    {
        public bool allowNull = false;
        public FromChildrenAttribute()
        {
            order = -10;
        }
    }
}
