using System;
using System.Diagnostics;
using UnityEngine;

namespace CustomInspector
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    [Conditional("UNITY_EDITOR")]
    public class ColorPaletteAttribute : PropertyAttribute
    {
        /// <summary>
        /// removes the foldout option to choose a custom color
        /// </summary>
        public bool hideFoldout;
        public readonly string paletteName;
        public ColorPaletteAttribute(string paletteName = "default")
        {
            if (string.IsNullOrEmpty(paletteName))
                paletteName = "default";
            this.paletteName = paletteName;
        }
    }
}
