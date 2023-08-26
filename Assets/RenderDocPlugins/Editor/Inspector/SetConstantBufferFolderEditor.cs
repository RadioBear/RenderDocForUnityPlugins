using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEditor;
using UnityEngine;
using static RenderDocPlugins.ArrayDataUtility;
using static RenderDocPlugins.SetConstantBufferFolder;
using AnimatedBool = UnityEditor.AnimatedValues.AnimBool;

namespace RenderDocPlugins
{
    [CustomEditor(typeof(SetConstantBufferFolder))]
    public class SetConstantBufferFolderEditor : Editor
    {
        #region Helper

        /// <summary>
        /// This struct is used to access the hidden .Net fields of the Array type.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct ArrayHeader
        {
            /// <summary>
            /// Array type.
            /// </summary>
            public UIntPtr type;

            /// <summary>
            /// Array length. 
            /// </summary>
            public UIntPtr length;
        }
        public unsafe static UIntPtr BeginAsArrayOfLength<T>(T[] buffer, int length)
        {
            if (length <= 0) return UIntPtr.Zero;
            if (length == buffer.Length) return UIntPtr.Zero;

            var handle = System.Runtime.InteropServices.GCHandle.Alloc(buffer, System.Runtime.InteropServices.GCHandleType.Pinned);
            var addr = handle.AddrOfPinnedObject();
            //void* pBuffer = GetBufferPointer();
            void* pBuffer = addr.ToPointer();

            // Get the header
            ArrayHeader* header = (ArrayHeader*)pBuffer - 1;

            // Change the length
            UIntPtr originalLength = header->length;
            header->length = new UIntPtr((ulong)length);
            handle.Free();
            return originalLength;
        }

        public unsafe static void EndAsArrayOfLength<T>(T[] buffer, UIntPtr old_len)
        {
            if (old_len != UIntPtr.Zero)
            {
                var handle = System.Runtime.InteropServices.GCHandle.Alloc(buffer, System.Runtime.InteropServices.GCHandleType.Pinned);
                var addr = handle.AddrOfPinnedObject();
                //void* pBuffer = GetBufferPointer();
                void* pBuffer = addr.ToPointer();

                // Get the header
                ArrayHeader* header = (ArrayHeader*)pBuffer - 1;

                // Revert back to old length
                header->length = old_len;

                handle.Free();
            }
        }

        // This is a helper class to allow the binding code to manipulate the internal fields of
        // System.Collections.Generic.List.  The field order below must not be changed.
        internal class ListPrivateFieldAccess<T>
        {
#pragma warning disable CS0649
#pragma warning disable CS8618
            internal T[] _items; // Do not rename (binary serialization)
#pragma warning restore CS8618
            internal int _size; // Do not rename (binary serialization)
            internal int _version; // Do not rename (binary serialization)
#pragma warning restore CS0649
        }

        // some helpers to handle List<T> in c# api (used for no-alloc apis where user provides list and we fill it):
        //   on il2cpp/mono we can "resize" List<T> (up to Capacity, sure, but this is/should-be handled higher level)
        //   also we can easily "convert" List<T> to System.Array
        // NB .net backend is treated as second-class citizen going through ToArray call
        sealed class NoAllocHelpers
        {
            public static void EnsureListElemCount<T>(List<T> list, int count)
            {
                if (list == null)
                    throw new ArgumentNullException("list");

                if (count < 0)
                    throw new ArgumentException("invalid size to resize.", "list");

                list.Clear();

                // make sure capacity is enough (that's where alloc WILL happen if needed)
                if (list.Capacity < count)
                    list.Capacity = count;

                if (count != list.Count)
                {
                    ListPrivateFieldAccess<T> tListAccess = UnsafeUtility.As<List<T>, ListPrivateFieldAccess<T>>(ref list);
                    tListAccess._size = count;
                    tListAccess._version++;
                }
            }

            // tiny helpers
            public static int SafeLength(System.Array values) { return values != null ? values.Length : 0; }
            public static int SafeLength<T>(List<T> values) { return values != null ? values.Count : 0; }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static T[] ExtractArrayFromList<T>(List<T> list)
            {
                if (list == null)
                    return null;

                ListPrivateFieldAccess<T> tListAccess = UnsafeUtility.As<List<T>, ListPrivateFieldAccess<T>>(ref list);
                return tListAccess._items;
            }
        }

        #endregion

        private static class Styles
        {
            public static readonly GUIContent iconAdd = EditorGUIUtility.TrIconContent("Toolbar Plus", "Add new folder");
            public static readonly GUIContent iconFolder = EditorGUIUtility.TrIconContent("Folder Icon");
            public static readonly GUIContent iconFolderOn = EditorGUIUtility.TrIconContent("Folder On Icon");
            public static readonly GUIContent iconFolderOpened = EditorGUIUtility.TrIconContent("FolderOpened Icon");
            public static readonly GUIContent iconFolderOpenedOn = EditorGUIUtility.TrIconContent("FolderOpened On Icon");
            public static readonly GUIContent iconMaterial = EditorGUIUtility.TrIconContent("Material Icon");
            public static readonly GUIContent iconMaterialOn = EditorGUIUtility.TrIconContent("Material On Icon");
            public static readonly GUIContent iconRefresh = EditorGUIUtility.TrIconContent("Refresh");
            public static readonly GUIContent iconRemove = EditorGUIUtility.TrIconContent("ol_minus_act");
            public static readonly GUIContent iconClear = EditorGUIUtility.TrIconContent("clear");
            public static readonly GUIContent textConstantBuffer = EditorGUIUtility.TrTextContent("Constant Buffer ({0})");
            public static readonly GUIContent textClearAll = EditorGUIUtility.TrTextContent("Clear All");
            public static readonly GUIContent textCSV = EditorGUIUtility.TrTextContent("CSV");
            public static readonly GUIContent textModify = EditorGUIUtility.TrTextContent("Modify");
            public static readonly GUIContent textAll = EditorGUIUtility.TrTextContent("All");

            public static readonly GUIStyle styleToggleFolder = new (EditorStyles.iconButton);
            public static readonly GUIStyle styleToolbar = new (EditorStyles.toolbar);
            public static readonly GUIStyle styleToolbarButton = new (EditorStyles.toolbarButton);
            public static readonly GUIStyle styleBackground = new (EditorStyles.helpBox);
            public static readonly GUIStyle styleFoldoutHeader = new (EditorStyles.foldoutHeader);
            public static readonly GUIStyle styleIcon = new(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                padding = new(0,0,0,0),
            };

        }

        private static bool s_ModifyMode = false;
        private static readonly List<Vector4> s_BufferData = new ();

        private readonly AnimatedBool m_ShowBGColorOptions = new AnimatedBool();
        private SerializedProperty m_MaterialList;
        private SetConstantBufferFolder m_CurrentObject;

        private void OnEnable()
        {
            m_MaterialList = serializedObject.FindProperty("m_MaterialList");
        }
        private void OnDisable()
        {
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            m_CurrentObject = target as SetConstantBufferFolder;

            GUILayout.BeginHorizontal(Styles.styleToolbar, GUILayout.ExpandWidth(true));
            if(GUILayout.Button(Styles.iconAdd, Styles.styleToolbarButton, GUILayout.ExpandWidth(false)))
            {
                m_MaterialList.arraySize += 1;
            }
            s_ModifyMode = GUILayout.Toggle(s_ModifyMode, Styles.textModify, Styles.styleToolbarButton, GUILayout.ExpandWidth(false));
            if (GUILayout.Button(Styles.textClearAll, Styles.styleToolbarButton, GUILayout.ExpandWidth(false)))
            {
                m_MaterialList.arraySize = 0;
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);

            var arraySize = GetMaterialContantBufferCount();
            for (int i = 0; i < arraySize; i++)
            {
                if (i != 0)
                {
                    GUILayout.Space(2f);
                }
                DrawMaterialConstantBuffer(m_MaterialList.GetArrayElementAtIndex(i), m_MaterialList, i);
            }
            serializedObject.ApplyModifiedProperties();
            m_CurrentObject = null;
        }

        private int GetMaterialContantBufferCount()
        {
            if (!m_MaterialList.hasMultipleDifferentValues)
                return m_MaterialList.arraySize;

            int smallerArraySize = m_MaterialList.arraySize;
            foreach (var targetObject in m_MaterialList.serializedObject.targetObjects)
            {
                SerializedObject serializedObject = new SerializedObject(targetObject);
                SerializedProperty property = serializedObject.FindProperty(m_MaterialList.propertyPath);
                smallerArraySize = Math.Min(property.arraySize, smallerArraySize);
            }
            return smallerArraySize;
        }

        /// <summary>
        /// MaterialConstantBuffer
        /// </summary>
        /// <param name="property"></param>
        private void DrawMaterialConstantBuffer(SerializedProperty property, SerializedProperty parent, int index)
        {
            SerializedProperty folderObject = property.FindPropertyRelative("m_FolderObject");
            SerializedProperty materialIndex = property.FindPropertyRelative("m_MaterialIndex");

            var oldHierarchyMode = EditorGUIUtility.hierarchyMode;
            EditorGUIUtility.hierarchyMode = false;

            GUILayout.BeginVertical(Styles.styleBackground);
            {
                GUILayout.BeginHorizontal(GUILayout.Height(EditorGUIUtility.singleLineHeight));
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button(Styles.iconRefresh, EditorStyles.iconButton, GUILayout.ExpandWidth(false)))
                    {
                        m_CurrentObject.UpdateConstantBuffer(index);
                    }
                    GUILayout.Space(5f);
                    if (GUILayout.Button(Styles.iconRemove, EditorStyles.iconButton, GUILayout.ExpandWidth(false)))
                    {
                        parent.DeleteArrayElementAtIndex(index);
                        serializedObject.ApplyModifiedProperties();
                        GUIUtility.ExitGUI();
                    }
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);

                EditorGUI.BeginProperty(EditorGUILayout.BeginHorizontal(), null, folderObject);
                {
                    GUILayout.Label(Styles.iconFolder, Styles.styleIcon, GUILayout.Width(EditorGUIUtility.singleLineHeight), GUILayout.Height(EditorGUIUtility.singleLineHeight));
                    EditorGUILayout.PropertyField(folderObject);
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUI.EndProperty();

                GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);

                EditorGUI.BeginProperty(EditorGUILayout.BeginHorizontal(), null, materialIndex);
                {
                    GUILayout.Label(Styles.iconMaterial, Styles.styleIcon, GUILayout.Width(EditorGUIUtility.singleLineHeight), GUILayout.Height(EditorGUIUtility.singleLineHeight));
                    EditorGUI.BeginChangeCheck();
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.PropertyField(materialIndex);
                    if (GUILayout.Button(Styles.textAll, GUILayout.ExpandWidth(false)))
                    {
                        materialIndex.intValue = -1;
                    }
                    GUILayout.EndHorizontal();
                    if (EditorGUI.EndChangeCheck())
                    {
                        Debug.Log("changed");
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUI.EndProperty();

                GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);

                var constantBufferCount = m_CurrentObject.GetConstantBufferCount(index);
                if (constantBufferCount > 0)
                {
                    var constBufferTitle = TempGUIContent(string.Format(Styles.textConstantBuffer.text, constantBufferCount));
                    folderObject.isExpanded = EditorGUILayout.Foldout(folderObject.isExpanded, constBufferTitle, true);
                    if (folderObject.isExpanded)
                    {
                        var bufferList = m_CurrentObject.GetConstantBuffers(index);
                        if (bufferList != null)
                        {
                            ++EditorGUI.indentLevel;

                            for (int i = 0; i < bufferList.Count; ++i)
                            {
                                DrawCSVConstantBuffer(bufferList[i], index, i);
                            }

                            --EditorGUI.indentLevel;
                        }
                    }
                }
            }
            GUILayout.EndVertical();

            // resotre
            EditorGUIUtility.hierarchyMode = oldHierarchyMode;
        }

        private void DrawCSVConstantBuffer(CSVConstantBuffer csvBuffer, int index, int bufferIndex)
        {
            csvBuffer.Expanded = EditorGUILayout.Foldout(csvBuffer.Expanded, csvBuffer.BufferName, true);
            if (csvBuffer.Expanded)
            {
                GUILayout.BeginHorizontal();
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.ObjectField(Styles.textCSV, csvBuffer.CSVAsset, typeof(TextAsset), false);
                EditorGUI.EndDisabledGroup();
                if (GUILayout.Button(Styles.iconRefresh, EditorStyles.iconButton, GUILayout.ExpandWidth(false)))
                {
                    m_CurrentObject.UpdateConstantBuffer(index, bufferIndex);
                }
                GUILayout.EndHorizontal();

                var buffer = csvBuffer.Buffer;
                if (buffer != null && buffer.IsValid())
                {
                    NoAllocHelpers.EnsureListElemCount(s_BufferData, buffer.count);
                    var arrayData = NoAllocHelpers.ExtractArrayFromList(s_BufferData);
                    var arrayTag = BeginAsArrayOfLength(arrayData, buffer.count);

                    bool modify = false;
                    buffer.GetData(arrayData);
                    csvBuffer.ScrollPos = EditorGUILayout.BeginScrollView(csvBuffer.ScrollPos);
                    {
                        for (int i = 0; i < arrayData.Length; ++i)
                        {
                            EditorGUILayout.BeginHorizontal();
                            GUILayout.Label($"[{i}]", GUILayout.ExpandWidth(false));
                            if (s_ModifyMode)
                            {
                                var newValue = EditorGUILayout.Vector4Field(GUIContent.none, arrayData[i]);
                                if (newValue != arrayData[i])
                                {
                                    arrayData[i] = newValue;
                                    modify = true;
                                }
                            }
                            else
                            {
                                EditorGUILayout.LabelField($"{arrayData[i].x}, {arrayData[i].y}, {arrayData[i].z}, {arrayData[i].w}");
                                GUILayout.FlexibleSpace();

                                if (arrayData[i].x >= 0.0f && arrayData[i].x <= 1.0f &&
                                    arrayData[i].y >= 0.0f && arrayData[i].y <= 1.0f &&
                                    arrayData[i].z >= 0.0f && arrayData[i].z <= 1.0f &&
                                    arrayData[i].w >= 0.0f && arrayData[i].w <= 1.0f)
                                {
                                    if (GUILayout.Button("Hex", GUILayout.ExpandWidth(false)))
                                    {
                                        Color c = arrayData[i];
                                        EditorGUIUtility.systemCopyBuffer = ColorUtility.ToHtmlStringRGB(c);
                                    }
                                }
                                if (GUILayout.Button("Copy", GUILayout.ExpandWidth(false)))
                                {
                                    EditorGUIUtility.systemCopyBuffer = $"{arrayData[i].x},{arrayData[i].y},{arrayData[i].z},{arrayData[i].w}";
                                }
                            }
                            EditorGUILayout.EndHorizontal();
                        }
                    }
                    EditorGUILayout.EndScrollView();

                    if (modify)
                    {
                        buffer.SetData(arrayData);
                    }

                    EndAsArrayOfLength(arrayData, arrayTag);
                }
            }
        }

        #region Tool

        private static readonly GUIContent s_Text = new GUIContent();

        private static GUIContent TempGUIContent(string t)
        {
            s_Text.text = t;
            s_Text.tooltip = string.Empty;
            return s_Text;
        }

        #endregion
    }
}