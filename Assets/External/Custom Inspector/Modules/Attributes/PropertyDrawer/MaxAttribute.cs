using System;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace CustomInspector
{
    /// <summary>
    /// Sets a maximum value for inputs in the inspector in form of a number or a reference (given by path)
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    [Conditional("UNITY_EDITOR")]
    public class MaxAttribute : PropertyAttribute, IMinMaxAttribute
    {
        /// <summary>
        /// The maximum allowed value.
        /// </summary>
        readonly float max;
        public float CapValue => max;
        readonly string maxPath = null;
        public string CapPath => maxPath;

        private MaxAttribute()
        {
            //Has to be before the built-in MinAttribute
            order = -10;
        }
        /// <summary>
        /// Attribute used to make a float or int variable in a script be restricted to a specific maximum  value.
        /// </summary>
        /// <param name="max">The maximum  allowed value.</param>
        public MaxAttribute(float max) : this()
        {
            this.max = max;
        }
        public MaxAttribute(string maxPath) : this()
        {
            if (string.IsNullOrEmpty(maxPath))
                Debug.LogWarning($"No {nameof(maxPath)} given to Min2Attribute to retrieve value from");
            this.maxPath = maxPath;
        }
        public bool DependsOnOtherProperty() => maxPath != null;
    }
}