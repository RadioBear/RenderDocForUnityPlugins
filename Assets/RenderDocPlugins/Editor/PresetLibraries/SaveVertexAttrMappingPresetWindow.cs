using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace RenderDocPlugins
{
    internal class SaveVertexAttrMappingPresetWindow : EditorWindow
    {
        private const string k_Title = "Save Preset";

        bool m_DidFocus;
        const int k_Offset = 20;
        const int k_Width = 200;
        const int k_Height = 48;
        const int k_HelpBoxHeight = 40;

        static readonly System.Collections.ObjectModel.ReadOnlyCollection<char> k_InvalidChars = new System.Collections.ObjectModel.ReadOnlyCollection<char>(System.IO.Path.GetInvalidFileNameChars());
        static System.Text.StringBuilder s_CurrentInvalidChars = new System.Text.StringBuilder(k_InvalidChars.Count);
        static string s_InvalidCharsFormatString = L10n.Tr("Invalid characters: {0}");
        static string s_TargetName = string.Empty;
        static VertexAttributeMappingPreset s_TargetPreset;

        internal static SaveVertexAttrMappingPresetWindow Show(Rect r, VertexAttributeMappingPreset preset)
        {
            s_TargetName = string.Empty;
            s_TargetPreset = preset;
            SaveVertexAttrMappingPresetWindow w = GetWindowWithRect<SaveVertexAttrMappingPresetWindow>(new Rect(r.xMax - (k_Width - k_Offset), r.y + k_Offset, k_Width, k_Height), true, L10n.Tr(k_Title));
            //w.m_Parent.window.m_DontSaveToLayout = true;
            return w;
        }

        private static void UpdateCurrentInvalidChars()
        {
            s_CurrentInvalidChars.Clear();
            // This approach will get the invalid characters in the layout name in they order they appear.
            // This approach would help locate invalid characters faster (in theory) and makes more sense to display them this way if a few unique characters were being typed in a row.

            // We loop through the characters in the name of the layout.
            for (int i = 0; i < s_TargetName.Length; ++i)
            {
                bool wasAdded = false;
                bool isInvalidChr = false;

                // We loop through the invalid characters, trying to see if the current character in the layout name is invalid.
                for (int j = 0; j < k_InvalidChars.Count && !isInvalidChr; ++j)
                {
                    if (s_TargetName[i] == k_InvalidChars[j])
                    {
                        isInvalidChr = true;

                        // We loop through the invalid characters to see if the current invalid character was already added.
                        for (int k = 0; k < s_CurrentInvalidChars.Length && !wasAdded; ++k)
                        {
                            if (s_CurrentInvalidChars[k] == k_InvalidChars[j])
                            {
                                wasAdded = true;
                            }
                        }
                    }
                }

                if (!wasAdded && isInvalidChr)
                {
                    s_CurrentInvalidChars.Append(s_TargetName[i]);
                }
            }
        }

        void OnEnable()
        {
            titleContent.text = k_Title;
        }

        void OnGUI()
        {
            GUILayout.Space(5);
            Event evt = Event.current;
            bool hitEnter = evt.type == EventType.KeyDown && (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter);
            GUI.SetNextControlName("m_PreferencesName");
            EditorGUI.BeginChangeCheck();
            s_TargetName = EditorGUILayout.TextField(s_TargetName);
            s_TargetName = s_TargetName.TrimEnd();
            if (EditorGUI.EndChangeCheck())
            {
                UpdateCurrentInvalidChars();
            }

            if (!m_DidFocus)
            {
                m_DidFocus = true;
                EditorGUI.FocusTextInControl("m_PreferencesName");
            }

            if (s_CurrentInvalidChars.Length != 0)
            {
                EditorGUILayout.HelpBox(string.Format(s_InvalidCharsFormatString, s_CurrentInvalidChars), MessageType.Warning);
                minSize = new Vector2(k_Width, k_Height + k_HelpBoxHeight);
            }
            else
            {
                minSize = new Vector2(k_Width, k_Height);
            }

            bool canSaveLayout = s_TargetName.Length > 0 && s_CurrentInvalidChars.Length == 0;
            EditorGUI.BeginDisabledGroup(!canSaveLayout);

            if (GUILayout.Button("Save") || hitEnter && canSaveLayout)
            {
                Close();

                VertexAttributeMapping.SavePreset(s_TargetPreset, s_TargetName);
                GUIUtility.ExitGUI();
            }
            else
            {
                m_DidFocus = false;
            }

            EditorGUI.EndDisabledGroup();
        }

        private void Update()
        {
            if(EditorWindow.focusedWindow != this)
            {
                Close();
            }
        }
    }
}