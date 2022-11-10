using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Unity.Collections;

namespace RenderDocPlugins
{
    [CustomEditor(typeof(CSVMaterialConstantBuffer))]
    public class CSVMaterialConstantBufferEditor : Editor
    {
        private static bool s_FoldoutDataList;
        private static bool s_ModifyMode = false;
        private Vector2 m_ScrollPos;

        private void OnEnable()
        {
            
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var obj = target as CSVMaterialConstantBuffer;
            var buffer = obj.Buffer;
            if(buffer != null && buffer.IsValid())
            {
                EditorGUILayout.LabelField($"Buffer is set to renderer. Count: {buffer.count} Byte: {buffer.count * buffer.stride}B");

                unsafe
                {
                    var count = buffer.count / 4;
                    var arrayData = new Vector4[count];
                    bool modify = false;
                    buffer.GetData(arrayData);

                    s_ModifyMode = EditorGUILayout.ToggleLeft("Modify Mode", s_ModifyMode);

                    s_FoldoutDataList = EditorGUILayout.Foldout(s_FoldoutDataList, "Data List:", true);
                    if (s_FoldoutDataList)
                    {
                        m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos);
                        {
                            for (int i = 0; i < arrayData.Length; ++i)
                            {
                                EditorGUILayout.BeginHorizontal();
                                GUILayout.Label($"[{i}]", GUILayout.ExpandWidth(false));
                                if (s_ModifyMode)
                                {
                                    var newValue = EditorGUILayout.Vector4Field(GUIContent.none, arrayData[i]);
                                    if(newValue != arrayData[i])
                                    {
                                        arrayData[i] = newValue;
                                        modify = true;
                                    }
                                }
                                else
                                {
                                    EditorGUILayout.LabelField($"{arrayData[i].x},{arrayData[i].y},{arrayData[i].z},{arrayData[i].w}");
                                    if (arrayData[i].x >= 0.0f && arrayData[i].x <= 1.0f &&
                                        arrayData[i].y >= 0.0f && arrayData[i].y <= 1.0f &&
                                        arrayData[i].z >= 0.0f && arrayData[i].z <= 1.0f &&
                                        arrayData[i].w >= 0.0f && arrayData[i].w <= 1.0f)
                                    {
                                        if (GUILayout.Button("Color(Hex)"))
                                        {
                                            Color c = arrayData[i];
                                            EditorGUIUtility.systemCopyBuffer = ColorUtility.ToHtmlStringRGB(c);
                                        }
                                    }
                                    else
                                    {
                                        if (GUILayout.Button("Copy"))
                                        {
                                            EditorGUIUtility.systemCopyBuffer = $"{arrayData[i].x},{arrayData[i].y},{arrayData[i].z},{arrayData[i].w}";
                                        }
                                    }
                                }
                                EditorGUILayout.EndHorizontal();
                            }
                        }
                        EditorGUILayout.EndScrollView();
                    }

                    if(modify)
                    {
                        buffer.SetData(arrayData);
                    }
                }
            }
        }
    }
}