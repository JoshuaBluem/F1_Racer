using CustomInspector.Extensions;
using CustomInspector.Helpers;
using CustomInspector.Helpers.Editor;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CustomInspector.Editor
{
    [CustomPropertyDrawer(typeof(ColorPaletteAttribute))]
    public class ColorPaletteAttributeDrawer : PropertyDrawer
    {
        /// <summary>
        /// thickness of outline of selected color
        /// </summary>
        [Min(4)] //cause we need 2 for white above and below and 2 for black above and below
        const float outlineThickness = 4;
        float SettingsIconWith => EditorGUIUtility.singleLineHeight;
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.Color)
            {
                DrawProperties.DrawPropertyWithMessage(position,
                                label,
                                property,
                                nameof(ColorPaletteAttributeDrawer) + " is only valid on Colors.",
                                MessageType.Error);
                return;
            }

            position.y += outlineThickness / 2f;
            ColorPaletteAttribute cpa = (ColorPaletteAttribute)attribute;
            // label
            Rect labelRect = new(position)
            {
                width = EditorGUIUtility.labelWidth,
                height = EditorGUIUtility.singleLineHeight,
            };
            if (!cpa.hideFoldout)
                property.isExpanded = EditorGUI.Foldout(labelRect, property.isExpanded, label);
            else
                EditorGUI.LabelField(labelRect, label);

            // display colors
            string paletteName = ((ColorPaletteAttribute)attribute).paletteName;
            List<Color> colors = ColorPaletteWindow.Palettes.FromEditorPrefs().GetPalette(paletteName).colors;

            // display colors
            Rect contentRect;
            if (!string.IsNullOrEmpty(label.text))
            {
                contentRect = new(labelRect)
                {
                    x = position.x + labelRect.width + EditorGUIUtility.standardVerticalSpacing,
                    width = Mathf.Max(position.width - labelRect.width - EditorGUIUtility.standardVerticalSpacing - SettingsIconWith,
                                        outlineThickness * 2 * colors.Count),
                };
            }
            else
            {
                contentRect = new(position)
                {
                    height = EditorGUIUtility.singleLineHeight,
                    width = position.width - SettingsIconWith,
                };
                if (labelRect.x < 21)
                {
                    contentRect.width -= 21 - contentRect.x;
                    contentRect.x = 21;
                }
            }

            using (new NewIndentLevel(0))
            {
                contentRect.width /= colors.Count;
                contentRect.width -= outlineThickness;

                //draw previews
                for (int i = 0; i < colors.Count; i++)
                {
                    //outline selected color
                    Color color = colors[i];
                    if (ColorEqual(property.colorValue, color))
                    {
                        Rect extended = ExpandRect(contentRect, outlineThickness);
                        EditorGUI.DrawRect(extended, Color.white);

                        extended = ExpandRect(contentRect, outlineThickness / 2f);
                        EditorGUI.DrawRect(extended, Color.black);
                    }

                    //draw button below for selection
                    if (GUI.Button(contentRect, GUIContent.none))
                    {
                        property.colorValue = color;
                        property.serializedObject.ApplyModifiedProperties();
                    }

                    //draw color previews
                    color.a = 1;
                    if (contentRect.Contains(Event.current.mousePosition))
                    {
                        //Hover+Click
                        if (EditorGUIUtility.hotControl != 0) //button clicked
                        {
                            //Click
                            Color darkened = new(color.r - 0.1f, color.g - 0.1f, color.b - 0.1f, 1f);
                            EditorGUI.DrawRect(contentRect, darkened);
                        }
                        else
                        {
                            //Hover
                            EditorGUI.DrawRect(ExpandRect(contentRect, outlineThickness / 2f), color);
                        }
                    }
                    else
                    {
                        //Normal
                        EditorGUI.DrawRect(contentRect, color);
                    }

                    contentRect.x += contentRect.width + outlineThickness;
                }

                //draw settings icon
                Rect settingsRect = new(contentRect)
                {
                    x = contentRect.x,
                    width = SettingsIconWith,
                };
                using (new GUIColorScope(new Color(1, 1, 1, 0.5f)))
                {
                    if (GUI.Button(settingsRect, ""))
                    {
                        // Debug.Log("Edit color palettes");
                        ColorPaletteWindow.OpenWindow();
                    }
                }
                EditorGUI.LabelField(settingsRect, EditorGUIUtility.IconContent(InspectorIcon.Settings.ToInternalIconName()));
            }

            //draw foldout
            if (property.isExpanded && !cpa.hideFoldout)
            {
                Rect lowerLine = new(position)
                {
                    y = position.y + EditorGUIUtility.singleLineHeight + outlineThickness / 2f + EditorGUIUtility.standardVerticalSpacing,
                    height = EditorGUIUtility.singleLineHeight,
                };
                using (new EditorGUI.IndentLevelScope(1))
                {
                    GUIContent foldoutContent = string.IsNullOrEmpty(label.text) ? new(PropertyConversions.NameFormat(property.name)) : new GUIContent("Current Value");
                    property.colorValue = EditorGUI.ColorField(lowerLine, foldoutContent, property.colorValue);
                    property.serializedObject.ApplyModifiedProperties();
                }
            }
        }
        static bool ColorEqual(Color c1, Color c2) => c1.r == c2.r && c1.g == c2.g && c1.b == c2.b;
        static Rect ExpandRect(Rect original, float pixel)
        {
            return new()
            {
                x = original.x - pixel / 2f,
                y = original.y - pixel / 2f,
                width = original.width + pixel,
                height = original.height + pixel,
            };
        }
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.propertyType == SerializedPropertyType.Color)
            {
                float topLine = EditorGUIUtility.singleLineHeight + outlineThickness;

                ColorPaletteAttribute cpa = (ColorPaletteAttribute)attribute;
                if (property.isExpanded && !cpa.hideFoldout)
                    return topLine + EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight;
                else
                    return topLine;
            }
            else
                return DrawProperties.GetPropertyWithMessageHeight(label, property);
        }
        public class ColorPaletteWindow : EditorWindow
        {
            static ColorPalettesHolder palettesHolder = null;
            Vector2 scrollPos = Vector2.zero;

            public static void OpenWindow()
            {
                var window = EditorWindow.GetWindow<ColorPaletteWindow>(title: "Color Palettes", focus: true);
                window.minSize = new Vector2(330, 200);
            }
            void OnGUI()
            {
                try
                {
                    using (new NewIndentLevel(0))
                    {
                        TryGUI();
                    }
                }
                catch (ExitGUIException e)
                {
                    throw e;
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
            private void OnDestroy()
            {
                DestroyImmediate(palettesHolder);
            }
            void TryGUI()
            {
                //calc position for palettes
                Rect position = new(base.position)
                {
                    position = Vector2.one * 10, //some margin
                    size = base.position.size - (Vector2.one * 20),
                };
                float resetButtonHeight = 20;
                position.height -= resetButtonHeight + resetButtonHeight; //we use its height also as spacing

                //display palettes
                if (palettesHolder == null)
                {
                    palettesHolder = ScriptableObject.CreateInstance<ColorPalettesHolder>();
                    palettesHolder.palettes = Palettes.FromEditorPrefs();
                }

                using var so = new SerializedObject(palettesHolder);

                SerializedProperty defaultColors = so.FindProperty("palettes.defaultPalette.colors");
                SerializedProperty additionalPalettes = so.FindProperty("palettes.additionalPalettes");

                float totalPalettesHeight = EditorGUI.GetPropertyHeight(defaultColors) + EditorGUI.GetPropertyHeight(additionalPalettes);
                using (var scrollScope = new GUI.ScrollViewScope(position, scrollPos, new Rect(0, 0, position.width - Common.scrollbarThickness, totalPalettesHeight)))
                {
                    scrollPos = scrollScope.scrollPosition;

                    Rect line = new(position)
                    {
                        x = 0,
                        y = 0,
                    };
                    if (totalPalettesHeight >= position.height)
                        line.width = position.width - Common.scrollbarThickness;

                    EditorGUI.BeginChangeCheck();

                    line.height = EditorGUI.GetPropertyHeight(defaultColors);
                    DrawProperties.PropertyField(line, new("Default Palette"), defaultColors);

                    line.y += line.height + EditorGUIUtility.standardVerticalSpacing;
                    line.height = EditorGUI.GetPropertyHeight(additionalPalettes);
                    DrawProperties.PropertyField(line, new("Additional Palettes"), additionalPalettes);

                    if (EditorGUI.EndChangeCheck())
                    {
                        // Debug.Log("GUI changed");
                        so.ApplyModifiedProperties();
                        palettesHolder.palettes.SaveToEditorPrefs();
                    }
                }

                //display resest button below
                position.y += position.height + resetButtonHeight;
                position.x += (position.width - 100) / 2f; //center alignment
                position.height = resetButtonHeight;
                position.width = 100;
                if (GUI.Button(position, "Reset"))
                {
                    DestroyImmediate(palettesHolder);
                    Palettes.DeleteEditorPrefs();
                }
            }
            [Serializable]
            class ColorPalettesHolder : ScriptableObject
            {
                public Palettes palettes;
                private ColorPalettesHolder() { } // Scriptable Objects must be instantiated using the ScriptableObject.CreateInstance method instead of new
            }
            [Serializable]
            public class Palettes
            {
                public Palette defaultPalette;
                public List<Palette> additionalPalettes = new();

                private Palettes()
                {
                    defaultPalette = new Palette();
                    defaultPalette.name = "default";

                    additionalPalettes = new() { new Palette("Alternative Scheme", new List<Color>()
                    {
                        new(0.00f, 1.00f, 0.88f, 1),
                        new(1.00f, 0.73f, 0.27f, 1),
                        new(1.00f, 1.00f, 0.00f, 1),
                        new(0.94f, 0.73f, 0.88f, 1),
                        new(1.00f, 0.10f, 0.14f, 1),
                    }) };
                }

                public Palette GetPalette(string paletteName)
                {
                    if (string.IsNullOrEmpty(paletteName))
                        paletteName = "default";
                    if (paletteName == "default")
                        return defaultPalette;

                    foreach (Palette palette in additionalPalettes)
                    {
                        if (palette.name == paletteName)
                            return palette;
                    }
                    Debug.LogWarning($"{nameof(ColorPaletteAttribute)}: Could not find palette with name '{paletteName}'." +
                                     "\nUsing default palette instead");
                    return defaultPalette;
                }
                const string editorPrefsKey = "CustomInspector.ColorPalettes";
                public static Palettes FromEditorPrefs()
                {
                    string json = EditorPrefs.GetString(key: editorPrefsKey, defaultValue: "{}");
                    Palettes palettes;

                    if (string.IsNullOrEmpty(json) || json == "{}")
                    {
                        palettes = new Palettes();
                        palettes.SaveToEditorPrefs();
                        return palettes;
                    }

                    try
                    {
                        // Debug.Log("Loaded JSON: " + json);
                        palettes = JsonUtility.FromJson<Palettes>(json);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                        palettes = new Palettes();
                        palettes.SaveToEditorPrefs();
                    }

                    return palettes;
                }
                public void SaveToEditorPrefs()
                {
                    //log error
                    Debug.Assert(defaultPalette != null, "Trying to save empty default palette");
                    Debug.Assert(additionalPalettes != null, "Trying to save empty additional palettes");

                    //check class
                    if (defaultPalette.colors.Count <= 0)
                    {
                        Debug.LogError("Default palette cannot be empty");
                        defaultPalette = new();
                        defaultPalette.name = "default";
                    }
                    for (int i = 0; i < additionalPalettes.Count; i++)
                    {
                        if (additionalPalettes[i].colors.Count <= 0)
                        {
                            Debug.LogError("Palette cannot be empty");
                            additionalPalettes[i] = new();
                        }
                    }

                    //save json
                    string json = JsonUtility.ToJson(this);
                    EditorPrefs.SetString(key: editorPrefsKey, value: json);
                }
                public static void DeleteEditorPrefs()
                {
                    EditorPrefs.DeleteKey(key: editorPrefsKey);
                }
                [Serializable]
                public class Palette
                {
                    public string name;
                    public List<Color> colors;

                    public Palette()
                    {
                        //put in default values
                        name = "new Palette";
                        colors = new List<Color>()
                        {
                            new(0.70f, 0.80f, 0.88f, 1),
                            new(0.39f, 0.59f, 0.69f, 1),
                            new(0.01f, 0.36f, 0.59f, 1),
                            new(0.01f, 0.22f, 0.42f, 1),
                            new(0.00f, 0.12f, 0.29f, 1),
                            new(0.02f, 0.02f, 0.05f, 1),
                        };
                    }
                    public Palette(string name, List<Color> colors)
                    {
                        this.name = name;
                        this.colors = colors;
                    }
                }
            }
        }
    }
}
