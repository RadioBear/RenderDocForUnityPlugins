using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace RenderDocPlugins
{
    public class PresetWindow : EditorWindow
    {
        #region 

        class InternalEditorGUIUtility
        {
            // internal static Texture2D LoadIcon(string name)
            static System.Reflection.MethodInfo s_methd_LoadIcon;
            public static Texture2D LoadIcon(string name)
            {
                if (s_methd_LoadIcon == null)
                {
                    s_methd_LoadIcon = typeof(EditorGUIUtility).GetMethod("LoadIcon", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                }
                if (s_methd_LoadIcon != null)
                {
                    return s_methd_LoadIcon.Invoke(null, new object[] { name }) as Texture2D;
                }
                return null;
            }
        }

        #endregion

        private class Styles
        {
            public readonly GUIStyle background = "grey_border";
            public readonly GUIStyle menuItem = "MenuItem";
            public readonly GUIStyle listEvenBg = "ObjectPickerResultsOdd";
            public readonly GUIStyle listOddBg = "ObjectPickerResultsEven";
            public readonly GUIStyle separator = "sv_iconselector_sep";
            public readonly GUIStyle lockButton = "IN LockButton";
            public readonly GUIStyle listHeaderStyle;
            public readonly GUIStyle scrollViewAlt = GetStyle("ScrollViewAlt");
            public readonly GUIStyle statusIcon = "StatusBarIcon";
            public readonly Texture2D visibleOn = InternalEditorGUIUtility.LoadIcon("animationvisibilitytoggleon");
            public readonly Texture2D visibleOff = InternalEditorGUIUtility.LoadIcon("animationvisibilitytoggleoff");
            public readonly GUIContent title = EditorGUIUtility.TrTextContent("Preset");
            public readonly GUIContent saveCurrentToPreset = EditorGUIUtility.TrTextContent("Save Current To Preset...");
            public readonly GUIContent revealSavePreset = EditorGUIUtility.TrTextContent("Reveal Save Preset Location");
            
            public readonly GUIContent trash = EditorGUIUtility.IconContent("TreeEditor.Trash");

            public Styles()
            {
                listHeaderStyle = new GUIStyle(EditorStyles.boldLabel);
                listHeaderStyle.padding.left = 5;
            }

            internal static GUIStyle GetStyle(string styleName)
            {
                GUIStyle s = GUI.skin.FindStyle(styleName) ?? EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).FindStyle(styleName);
                if (s == null)
                {
                    Debug.LogError("Missing built-in guistyle " + styleName);
                    s = GUIStyle.none;
                }
                return s;
            }
        }

        const float kScrollBarWidth = 14;
        const float kFrameWidth = 1f;
        const float kSeparatorHeight = 6;

        private static PresetWindow s_PresetWindow;
        private static long s_LastClosedTime;
        private static Styles s_Styles;

        private readonly List<string> m_PresetNames = new();

        private float m_ContentHeight;
        private Vector2 m_ScrollPosition;



        internal void OnEnable()
        {
            hideFlags = HideFlags.DontSave;
            wantsMouseMove = true;
        }

        internal void OnDisable()
        {
            s_LastClosedTime = System.DateTime.Now.Ticks / System.TimeSpan.TicksPerMillisecond;
            s_PresetWindow = null;
        }

        internal static bool ShowAtPosition(Rect buttonRect)
        {
            // We could not use realtimeSinceStartUp since it is set to 0 when entering/exitting playmode, we assume an increasing time when comparing time.
            long nowMilliSeconds = System.DateTime.Now.Ticks / System.TimeSpan.TicksPerMillisecond;
            bool justClosed = nowMilliSeconds < s_LastClosedTime + 50;
            if (!justClosed)
            {
                Event.current.Use();
                if (s_PresetWindow == null)
                    s_PresetWindow = CreateInstance<PresetWindow>();
                s_PresetWindow.Init(buttonRect);
                return true;
            }
            return false;
        }

        private void Init(Rect buttonRect)
        {
            s_Styles ??= new Styles();

            // Has to be done before calling Show / ShowWithMode
            buttonRect = GUIUtility.GUIToScreenRect(buttonRect);

            m_PresetNames.Clear();
            VertexAttributeMappingPresetManager.GetAllPresetNames(m_PresetNames);

            var toggleSize = s_Styles.statusIcon.CalcSize(s_Styles.trash);
            float widthMax = 0.0f;
            for (int i = 0; i < m_PresetNames.Count; ++i)
            {
                var size = s_Styles.menuItem.CalcSize(EditorGUIUtility.TrTempContent(m_PresetNames[i]));
                widthMax = Mathf.Max(widthMax, size.x + toggleSize.x);
            }
            {
                var size = s_Styles.menuItem.CalcSize(s_Styles.saveCurrentToPreset);
                widthMax = Mathf.Max(widthMax, size.x);
            }
            {
                var size = s_Styles.menuItem.CalcSize(s_Styles.revealSavePreset);
                widthMax = Mathf.Max(widthMax, size.x);
            }
            widthMax = Mathf.Min(widthMax, 400f);

            // БъЬт
            var rowCount = (m_PresetNames.Count + 1 + 1 + 1);

            var windowHeight = rowCount * EditorGUIUtility.singleLineHeight + kSeparatorHeight;

            m_ContentHeight = windowHeight;
            windowHeight += 2 * kFrameWidth;
            windowHeight = Mathf.Min(windowHeight, 600);

            var windowSize = new Vector2(widthMax, windowHeight);
            ShowAsDropDown(buttonRect, windowSize);
        }

        internal void OnGUI()
        {
            // We do not use the layout event
            if (Event.current.type == EventType.Layout)
                return;

            s_Styles ??= new Styles();

            var scrollViewRect = new Rect(kFrameWidth, kFrameWidth, position.width - 2 * kFrameWidth, position.height - 2 * kFrameWidth);
            var contentRect = new Rect(0, 0, 1, m_ContentHeight);
            bool isScrollbarVisible = m_ContentHeight > scrollViewRect.height;
            float listElementWidth = scrollViewRect.width;
            if (isScrollbarVisible)
                listElementWidth -= kScrollBarWidth;

            m_ScrollPosition = GUI.BeginScrollView(scrollViewRect, m_ScrollPosition, contentRect, false, false, GUI.skin.horizontalScrollbar, GUI.skin.verticalScrollbar);
            Draw(listElementWidth);
            GUI.EndScrollView();

            // Background with 1 pixel border
            GUI.Label(new Rect(0, 0, position.width, position.height), GUIContent.none, s_Styles.background);

            // Use mouse move so we get hover state correctly in the menu item rows
            if (Event.current.type == EventType.MouseMove)
                Event.current.Use();

            // Escape closes the window
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                Close();
                GUIUtility.ExitGUI();
            }
        }

        private void DrawListBackground(Rect rect, bool even)
        {
            GUIStyle backgroundStyle = even ? s_Styles.listEvenBg : s_Styles.listOddBg;
            GUI.Label(rect, GUIContent.none, backgroundStyle);
        }

        private void DrawHeader(ref Rect rect, GUIContent text, ref bool even)
        {
            DrawListBackground(rect, even);
            GUI.Label(rect, text, s_Styles.listHeaderStyle);
            rect.y += EditorGUIUtility.singleLineHeight;
            even = !even;
        }

        private void DrawSeparator(ref Rect rect, bool even)
        {
            DrawListBackground(new Rect(rect.x + 1, rect.y, rect.width - 2, kSeparatorHeight), even);
            GUI.Label(new Rect(rect.x + 5, rect.y + 3, rect.width - 10, 3), GUIContent.none, s_Styles.separator);
            rect.y += kSeparatorHeight;
        }

        private void Draw(float listElementWidth)
        {
            var drawPos = new Rect(0, 0, listElementWidth, EditorGUIUtility.singleLineHeight);

            bool even = false;

            DrawHeader(ref drawPos, s_Styles.title, ref even);

            // Preset
            for (var i = 0; i < m_PresetNames.Count; ++i)
            {
                DoOnePreset(drawPos, i, ref even);
                drawPos.y += EditorGUIUtility.singleLineHeight;
            }


            // Edit Layers entry
            DrawSeparator(ref drawPos, even);

            DrawListBackground(drawPos, even);
            if (GUI.Button(drawPos, s_Styles.saveCurrentToPreset, s_Styles.menuItem))
            {
                Close();
                if (MeshImporterWindow.Instance != null)
                {
                    var preset = MeshImporterWindow.Instance.GetCurrentVertexAttributeMappingPreset();
                    if (preset != null)
                    {
                        SaveVertexAttrMappingPresetWindow.Show(GUIUtility.GUIToScreenRect(drawPos), preset);
                    }
                }
                GUIUtility.ExitGUI();
            }

            drawPos.y += EditorGUIUtility.singleLineHeight;
            even = !even;

            if (GUI.Button(drawPos, s_Styles.revealSavePreset, s_Styles.menuItem))
            {
                Close();
                EditorUtility.RevealInFinder(VertexAttributeMappingPresetManager.PreferencesPath);
                GUIUtility.ExitGUI();
            }
        }

        void DoOnePreset(Rect rect, int index, ref bool even)
        {
            var presetName = m_PresetNames[index];
            DoPresetEntry(rect, presetName, even, out bool apply, out bool removeChanged);
            if (apply)
            {
                Close();
                if(MeshImporterWindow.Instance != null)
                {
                    MeshImporterWindow.Instance.ApplyVertexAttributeMappingPreset(VertexAttributeMappingPresetManager.ReadPreset(presetName));
                }
                GUIUtility.ExitGUI();
            }
            if (removeChanged)
            {
                if (EditorUtility.DisplayDialog("Delete Preset", $"Do you want to delete preset \"{presetName}\"", "Yes", "No"))
                {
                    VertexAttributeMappingPresetManager.DeletePreset(presetName);
                }
                Close();
                GUIUtility.ExitGUI();
            }
            even = !even;
        }



        private void DoPresetEntry(Rect rect, string presetName, bool even, out bool apply, out bool removeChanged)
        {
            apply = false;
            removeChanged = false;
            var toggleSize = s_Styles.statusIcon.CalcSize(s_Styles.trash);

            DrawListBackground(rect, even);

            Rect textRect = rect;
            textRect.width -= toggleSize.x;
            if (GUI.Button(textRect, EditorGUIUtility.TrTempContent(presetName), s_Styles.menuItem))
            {
                apply = true;
            }
            var toggleRect = new Rect(rect.width - toggleSize.x, rect.y + (rect.height - toggleSize.y) * 0.5f, toggleSize.x, textRect.y);
            if (GUI.Button(toggleRect, s_Styles.trash, s_Styles.statusIcon))
            {
                removeChanged = true;
            }

        }
    }
}