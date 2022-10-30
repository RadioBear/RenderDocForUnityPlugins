using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEditor;

namespace RenderDocPlugins
{
    public class MeshImporterWindow : EditorWindow
    {
        private const int k_MinSubMeshCount = 1;
        private const int k_MaxSubMeshCount = 8;

        private const int k_WindowWidth = 500;
        private const int k_WindowHeight = 600;


        static class Styles
        {
            public static readonly GUIStyle dropdown = "Dropdown";

            private static Texture2D s_InfoIcon;
            private static Texture2D s_WarningIcon;
            private static Texture2D s_ErrorIcon;
            internal static Texture2D infoIcon
            {
                get
                {
                    if (s_InfoIcon == null)
                        s_InfoIcon = EditorGUIUtility.FindTexture("console.infoicon");
                    return s_InfoIcon;
                }
            }
            internal static Texture2D warningIcon
            {
                get
                {
                    if (s_WarningIcon == null)
                        s_WarningIcon = EditorGUIUtility.FindTexture("console.warnicon");
                    return s_WarningIcon;
                }
            }

            internal static Texture2D errorIcon
            {
                get
                {
                    if (s_ErrorIcon == null)
                        s_ErrorIcon = EditorGUIUtility.FindTexture("console.erroricon");
                    return s_ErrorIcon;
                }
            }

            internal static string GetHelpIcon(MessageType type)
            {
                switch (type)
                {
                    case MessageType.Info:
                        return "console.infoicon";
                    case MessageType.Warning:
                        return "console.warnicon";
                    case MessageType.Error:
                        return "console.erroricon";
                }
                return string.Empty;
            }

            static GUIContent s_PresetContent;
            internal static GUIContent PresetContent
            {
                get
                {
                    if (s_PresetContent == null)
                        s_PresetContent = EditorGUIUtility.TrTextContent("Preset", "Apply preset already saved.");
                    return s_PresetContent;
                }
            }
        }

        [System.Serializable]
        struct VertexAttrSetting
        {
            [SerializeField]
            public bool Ignore;
            [SerializeField]
            public string CSVName;
            [SerializeField]
            public int CompCount;
            [SerializeField]
            public UnityEngine.Rendering.VertexAttribute Attr;
        }

        [SerializeField]
        private Vector2 m_ScrollPos;

        [SerializeField]
        private int m_SubMeshCount = k_MinSubMeshCount;

        [SerializeField]
        private string[] m_SourcePath;

        [SerializeField]
        private string m_DestPath = string.Empty;

        [SerializeField]
        private string m_ErrorMessage = string.Empty;

        [SerializeField]
        private bool m_FlipVertexWindingOrder = false;

        [SerializeField]
        private List<VertexAttrSetting> m_VertexAttrList = new List<VertexAttrSetting>();

        [SerializeField]
        private bool m_AutoCalcNormalIfNotExist = false;

        [SerializeField]
        private bool m_AutoCalcTangentIfNotExist = false;

        [SerializeField]
        private bool m_OptimizesRendering = true;

        [SerializeField]
        private bool m_ReadWriteEnable = true;

        [SerializeField]
        private ModelImporterMeshCompression m_MeshCompression = ModelImporterMeshCompression.Off;

        // Overlay a notification message over the window.
        const double kWarningFadeoutWait = 4;
        const double kWarningFadeoutTime = 1;

        internal GUIContent m_MessageNotification = null;
        Vector2 m_MessageNotificationSize;
        internal float m_MessageFadeoutTime = 0;

        static MeshImporterWindow s_Instance;
        public static MeshImporterWindow Instance { get { return s_Instance; } }

        [MenuItem("RenderDocPlugins/Import Mesh From CSV")]
        public static void DoImportMeshFromCSV()
        {
            var win = EditorWindow.GetWindow<MeshImporterWindow>();
            if (win != null)
            {
                win.ShowAuxWindow();
            }
        }

        private void OnEnable()
        {
            s_Instance = this;

            titleContent = new GUIContent("Mesh Importer");
            minSize = new Vector2(k_WindowWidth, k_WindowHeight);
            if (m_SourcePath == null || m_SourcePath.Length != k_MaxSubMeshCount)
            {
                m_SourcePath = new string[k_MaxSubMeshCount];
            }
        }

        private void OnDisable()
        {
            s_Instance = null;
        }


        private void OnGUI()
        {
            if (m_MessageNotification != null)
            {
                DrawMessage();
            }

            m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos);

            GUILayout.Space(10f);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.BeginHorizontal();
                {
                    GUILayout.Label("Sub mesh count", GUILayout.ExpandWidth(false));
                    var count = EditorGUILayout.IntSlider(m_SubMeshCount, k_MinSubMeshCount, k_MaxSubMeshCount);
                    if (count != m_SubMeshCount)
                    {
                        m_SubMeshCount = count;
                        TrimSourcePathFromSubMeshCount();
                    }
                }
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(10f);

                for (int subMeshIndex = 0; subMeshIndex < m_SubMeshCount; ++subMeshIndex)
                {
                    if (subMeshIndex != 0)
                    {
                        GUILayout.Space(5f);
                    }
                    EditorGUILayout.BeginHorizontal();
                    {
                        GUILayout.Label($"SubMesh #{subMeshIndex + 1} CSV", GUILayout.ExpandWidth(false));
                        GUILayout.Space(10f);
                        if (string.IsNullOrEmpty(m_SourcePath[subMeshIndex]))
                        {
                            GUILayout.FlexibleSpace();
                            GUILayout.Label("Have not selected source csv file. Or you can drop csv file to here.", GUILayout.ExpandWidth(false), GUILayout.MinWidth(0f));
                            GUILayout.FlexibleSpace();
                        }
                        else
                        {
                            GUILayout.Label(m_SourcePath[subMeshIndex], GUILayout.ExpandWidth(false), GUILayout.MinWidth(0f));
                        }
                        GUILayout.Space(10f);

                        GUILayout.FlexibleSpace();

                        if (GUILayout.Button("Select File...", GUILayout.ExpandWidth(false)))
                        {
                            string fileSelected = EditorUtility.OpenFilePanelWithFilters("Select source csv file", string.IsNullOrEmpty(m_SourcePath[subMeshIndex]) ? Application.dataPath : m_SourcePath[subMeshIndex], new string[] { "csv files", "csv", "All files", "*" });
                            if (!string.IsNullOrEmpty(fileSelected))
                            {
                                DoChangeSourceCSV(subMeshIndex, fileSelected);
                            }
                        }
                    }
                    EditorGUILayout.EndHorizontal();

                    HandleDragAndDropGUI(GUILayoutUtility.GetLastRect(), subMeshIndex);

                }
            }
            EditorGUILayout.EndVertical();

            if (!string.IsNullOrEmpty(m_ErrorMessage))
            {
                EditorGUILayout.HelpBox(m_ErrorMessage, MessageType.Error);
            }

            GUILayout.Space(10f);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.MinHeight(50f));
            {
                GUILayout.FlexibleSpace();
                EditorGUILayout.BeginHorizontal();
                {
                    GUILayout.Label("Destination Mesh", GUILayout.ExpandWidth(false));
                    GUILayout.Space(10f);
                    m_DestPath = GUILayout.TextField(m_DestPath);
                    GUILayout.Space(10f);

                    if (GUILayout.Button("Select Path...", GUILayout.ExpandWidth(false)))
                    {
                        string fileSelected = EditorUtility.SaveFilePanelInProject("Select destination mesh file", "New Mesh", "asset", "file to save");
                        if (!string.IsNullOrEmpty(fileSelected))
                        {
                            DoChangeDestMesh(fileSelected);
                        }
                        GUIUtility.ExitGUI();
                    }
                }
                EditorGUILayout.EndHorizontal();
                GUILayout.FlexibleSpace();
            }
            EditorGUILayout.EndVertical();

            GUILayout.Space(10f);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.MinHeight(50f));
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Vertex Attribute Mapping (Determined by the CSV of the first submesh)");
                GUILayout.FlexibleSpace();
                if (m_VertexAttrList.Count > 0)
                {
                    DoPresetsDropDown();
                }
                EditorGUILayout.EndHorizontal();

                if (m_VertexAttrList.Count <= 0)
                {
                    GUILayout.Label("Waiting set CSV File...");
                }
                else
                {
                    for (int i = 0; i < m_VertexAttrList.Count; ++i)
                    {
                        bool changed = false;
                        var data = m_VertexAttrList[i];
                        EditorGUILayout.BeginHorizontal();
                        var newIgnore = EditorGUILayout.ToggleLeft($"{data.CSVName} ({data.CompCount})", !data.Ignore, GUILayout.ExpandWidth(false));
                        if (newIgnore == data.Ignore)
                        {
                            data.Ignore = !newIgnore;
                            changed = true;
                        }
                        EditorGUI.BeginDisabledGroup(data.Ignore);
                        var newAttr = (UnityEngine.Rendering.VertexAttribute)EditorGUILayout.EnumPopup(string.Empty, data.Attr, GUILayout.ExpandWidth(false));
                        EditorGUI.EndDisabledGroup();
                        if (newAttr != data.Attr)
                        {
                            data.Attr = newAttr;
                            changed = true;
                        }
                        EditorGUILayout.EndHorizontal();
                        if (changed)
                        {
                            m_VertexAttrList[i] = data;
                        }
                    }
                }

            }
            EditorGUILayout.EndVertical();


            GUILayout.Space(10f);

            m_FlipVertexWindingOrder = EditorGUILayout.ToggleLeft(new GUIContent("Flip Vertex Winding Order", "vertices of triangles in counter-clockwise order (or in clockwise order in DirectX)"), m_FlipVertexWindingOrder, GUILayout.ExpandWidth(false));
            m_AutoCalcNormalIfNotExist = EditorGUILayout.ToggleLeft(new GUIContent("Auto calculate normal if not exist", "Source of mesh normals. If a mesh has no normals, they will be calculated instead."), m_AutoCalcNormalIfNotExist, GUILayout.ExpandWidth(false));
            m_AutoCalcTangentIfNotExist = EditorGUILayout.ToggleLeft(new GUIContent("Auto calculate tangent if not exist", "Source of mesh tangents. If a mesh has no tangents, they will be calculated instead."), m_AutoCalcTangentIfNotExist, GUILayout.ExpandWidth(false));
            m_OptimizesRendering = EditorGUILayout.ToggleLeft(new GUIContent("Optimize Mesh", "Reorder vertices and/or polygons for better GPU performance."), m_OptimizesRendering, GUILayout.ExpandWidth(false));
            m_ReadWriteEnable = EditorGUILayout.ToggleLeft(new GUIContent("Read/Write Enabled", "Allow vertices and indices to be accessed from script."), m_ReadWriteEnable, GUILayout.ExpandWidth(false));
            m_MeshCompression = (ModelImporterMeshCompression)EditorGUILayout.EnumPopup(new GUIContent("Mesh Compression", "Higher compression ratio means lower mesh precision. If enabled, the mesh bounds and a lower bit depth per component are used to compress the mesh data."), m_MeshCompression, GUILayout.ExpandWidth(false));


            EditorGUILayout.EndScrollView();

            GUILayout.FlexibleSpace();

            EditorGUI.BeginDisabledGroup(!IsAllSourcePathReady() || string.IsNullOrEmpty(m_DestPath));
            {
                if (GUILayout.Button("Generate Mesh", GUILayout.MinHeight(50f)))
                {
                    DoGenerateMesh();
                }
            }
            EditorGUI.EndDisabledGroup();

        }

        #region  Preset

        /// <summary>
        /// Get Preset From UI Setting
        /// </summary>
        /// <returns></returns>
        public VertexAttributeMappingPreset GetCurrentVertexAttributeMappingPreset()
        {
            if(m_VertexAttrList.Count > 0)
            {
                VertexAttributeMappingPreset preset = new VertexAttributeMappingPreset();
                for (int i = 0; i < m_VertexAttrList.Count; ++i)
                {
                    var data = new VertexAttributeMappingData();
                    data.m_Enable = !m_VertexAttrList[i].Ignore;
                    data.m_VertexAttributeName = m_VertexAttrList[i].CSVName;
                    data.m_Attr = m_VertexAttrList[i].Attr;

                    preset.m_Datas.Add(data);
                }
                return preset;
            }
            return null;
        }

        public void ApplyVertexAttributeMappingPreset(VertexAttributeMappingPreset preset)
        {
            if (preset == null)
            {
                return;
            }

            for(int i = 0; i < m_VertexAttrList.Count; ++i)
            {
                var name = m_VertexAttrList[i].CSVName;
                var data = preset.GetData(name);
                if(data != null)
                {
                    var newData = m_VertexAttrList[i];
                    newData.Ignore = !data.m_Enable;
                    newData.Attr = data.m_Attr;
                    m_VertexAttrList[i] = newData;
                }
            }
        }

        void DoPresetsDropDown()
        {
            var rect = GUILayoutUtility.GetRect(Styles.PresetContent, Styles.dropdown);
            if (EditorGUI.DropdownButton(rect, Styles.PresetContent, FocusType.Passive, Styles.dropdown))
            {
                if (PresetWindow.ShowAtPosition(rect))
                {
                    GUIUtility.ExitGUI();
                }
            }
        }


        #endregion

        private void HandleDragAndDropGUI(Rect dragdropArea, int index)
        {
            Event evt = Event.current;
            switch (evt.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (!dragdropArea.Contains(evt.mousePosition))
                    {
                        return;
                    }

                    if (DragAndDrop.paths != null && DragAndDrop.paths.Length > 0)
                    {
                        var path = DragAndDrop.paths[0];
                        if (!string.IsNullOrEmpty(path))
                        {
                            if (IsValidCSVFile(path))
                            {
                                DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
                            }
                            else
                            {
                                DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                            }
                        }
                    }

                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();

                        if (DragAndDrop.paths != null && DragAndDrop.paths.Length > 0)
                        {
                            var path = DragAndDrop.paths[0];
                            if (IsValidCSVFile(path))
                            {
                                DoChangeSourceCSV(index, path);
                            }
                        }
                    }
                    break;
            }
        }

        private bool IsValidCSVFile(string path)
        {
            System.IO.FileAttributes attr = System.IO.File.GetAttributes(path);
            if ((attr & System.IO.FileAttributes.Directory) != System.IO.FileAttributes.Directory)
            {
                if (System.IO.Path.GetExtension(path).Equals(".csv", System.StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsExistSourceCSV()
        {
            if(m_SourcePath.Length > 0)
            {
                if (!string.IsNullOrEmpty(m_SourcePath[0]))
                {
                    return true;
                }
            }
            return false;
        }

        private void DoChangeSourceCSV(int index, string fileFullPath)
        {
            if (index >= 0 && index < m_SourcePath.Length)
            {
                m_SourcePath[index] = fileFullPath;

                DoCheckCSVError();

                if (index == 0)
                {
                    m_VertexAttrList.Clear();
                    if (!string.IsNullOrEmpty(m_SourcePath[index]))
                    {
                        List<string> headerList = new List<string>();
                        List<int> compCountList = new List<int>();
                        RenderDocCSV.GetMeshCSVVertexAttrList(m_SourcePath[index], headerList, compCountList);
                        for (int i = 0; i < headerList.Count; ++i)
                        {
                            var attr = new VertexAttrSetting();
                            attr.CSVName = headerList[i];
                            attr.CompCount = compCountList[i];
                            RenderDocCSV.SpeculateVertextAttr(headerList[i], out attr.Attr);
                            m_VertexAttrList.Add(attr);
                        }
                    }
                }
            }
        }

        private bool DoCheckVertexAttributeRepeat()
        {
            HashSet<VertexAttribute> set = new HashSet<VertexAttribute>();
            for (int i = 0; i < m_VertexAttrList.Count; ++i)
            {
                var data = m_VertexAttrList[i];
                if (!data.Ignore)
                {
                    if (set.Contains(data.Attr))
                    {
                        return false;
                    }
                    set.Add(data.Attr);
                }
            }
            return true;
        }

        private void DoCheckCSVError()
        {
            m_ErrorMessage = string.Empty;
            if (!RenderDocCSV.CheckAllMeshCSVHeader(m_SourcePath))
            {
                m_ErrorMessage = "CSV Head not all the same!";
            }
        }

        private void DoChangeDestMesh(string assetPath)
        {
            m_DestPath = assetPath;
        }

        private void DoGenerateMesh()
        {
            if (!DoCheckVertexAttributeRepeat())
            {
                ShowMessage("Vertex Attribute Repeat", MessageType.Error);
                return;
            }

            var setting = new CSVToMeshGenerator.GenSetting();
            setting.flags = CSVToMeshGenerator.Flags.None;
            if (m_FlipVertexWindingOrder)
            {
                setting.flags |= CSVToMeshGenerator.Flags.FlipVertexWindingOrder;
            }
            if (m_AutoCalcNormalIfNotExist)
            {
                setting.flags |= CSVToMeshGenerator.Flags.AutoCalcNormalIfNotExist;
            }
            if (m_AutoCalcTangentIfNotExist)
            {
                setting.flags |= CSVToMeshGenerator.Flags.AutoCalcTangentIfNotExist;
            }
            if (m_OptimizesRendering)
            {
                setting.flags |= CSVToMeshGenerator.Flags.OptimizesRendering;
            }
            if (m_ReadWriteEnable)
            {
                setting.flags |= CSVToMeshGenerator.Flags.ReadWriteEnable;
            }
            setting.compression = m_MeshCompression;
            setting.vertexAttrMapping = new CSVToMeshGenerator.VertexAttributeMapping[m_VertexAttrList.Count];
            for (int i = 0; i < m_VertexAttrList.Count; ++i)
            {
                var mapping = new CSVToMeshGenerator.VertexAttributeMapping();
                mapping.Disable = m_VertexAttrList[i].Ignore;
                mapping.Name = m_VertexAttrList[i].CSVName;
                mapping.Attr = m_VertexAttrList[i].Attr;
                setting.vertexAttrMapping[i] = mapping;
            }

            CSVToMeshGenerator.GenerateMesh(m_SourcePath, m_SubMeshCount, m_DestPath, setting, Allocator.Temp);
        }

        private int GetCorrectSubMeshCount()
        {
            m_SubMeshCount = Mathf.Clamp(m_SubMeshCount, k_MinSubMeshCount, k_MaxSubMeshCount);
            return m_SubMeshCount;
        }

        private void TrimSourcePathFromSubMeshCount()
        {
            var subMeshCount = GetCorrectSubMeshCount();
            if (subMeshCount < k_MaxSubMeshCount)
            {
                if (!string.IsNullOrEmpty(m_SourcePath[subMeshCount]))
                {
                    for (int i = subMeshCount; i < m_SourcePath.Length; ++i)
                    {
                        m_SourcePath[i] = string.Empty;
                    }
                }
            }
        }

        private bool IsAllSourcePathReady()
        {
            for (int i = 0; i < m_SubMeshCount; ++i)
            {
                if (string.IsNullOrEmpty(m_SourcePath[i]))
                {
                    return false;
                }
            }
            return true;
        }

        // Show a notification message.
        private void ShowMessage(string message, MessageType type)
        {
            InternalShowMessage(EditorGUIUtility.TrTextContentWithIcon(message, string.Empty, Styles.GetHelpIcon(type)), kWarningFadeoutTime);
        }
        private void InternalShowMessage(GUIContent notification, double fadeoutWait)
        {
            m_MessageNotification = new GUIContent(notification);
            if (m_MessageFadeoutTime < Mathf.Epsilon)
                EditorApplication.update += CheckForWindowRepaint;
            m_MessageFadeoutTime = (float)(EditorApplication.timeSinceStartup + fadeoutWait);
        }

        // Stop showing notification message.
        private void RemoveMessage()
        {
            if (m_MessageFadeoutTime == 0)
                return;
            EditorApplication.update -= CheckForWindowRepaint;
            m_MessageNotification = null;
            m_MessageFadeoutTime = 0;
        }

        internal void CheckForWindowRepaint()
        {
            double time = EditorApplication.timeSinceStartup;
            if (time < m_MessageFadeoutTime)
                return;
            if (time > m_MessageFadeoutTime + kWarningFadeoutTime)
            {
                RemoveMessage();
                return;
            }
            Repaint();
        }

        internal void DrawMessage()
        {
            if (Event.current.type != EventType.Repaint)
                return;

            m_MessageNotificationSize = EditorStyles.helpBox.CalcSize(m_MessageNotification);

            Vector2 warningSize = m_MessageNotificationSize;
            float targetWidth = position.width - EditorStyles.helpBox.margin.horizontal;
            float targetHeight = position.height - EditorStyles.helpBox.margin.vertical /*- 20*/;

            // See if we can fit horizontally. If not, rescale down.
            GUIStyle scaledNotificationText = EditorStyles.helpBox;
            if (targetWidth < m_MessageNotificationSize.x)
            {
                float scale = targetWidth / m_MessageNotificationSize.x;

                scaledNotificationText = new GUIStyle(EditorStyles.helpBox);
                scaledNotificationText.fontSize = Mathf.FloorToInt(scaledNotificationText.font.fontSize * scale);

                warningSize = scaledNotificationText.CalcSize(m_MessageNotification);
            }

            warningSize.x += 1; //we'll give the text a little room to breathe to avoid word-wrapping issues with drop shadows

            if (warningSize.y > targetHeight)
                warningSize.y = targetHeight;

            Rect r = new Rect((position.width - warningSize.x) * .5f, 20 + (position.height - 20 - warningSize.y) * .7f, warningSize.x, warningSize.y);

            double time = EditorApplication.timeSinceStartup;
            var oldColor = GUI.color;
            if (time > m_MessageFadeoutTime)
                GUI.color = new Color(1, 1, 1, 1 - (float)((time - m_MessageFadeoutTime) / kWarningFadeoutTime));
            GUI.Label(r, m_MessageNotification, EditorStyles.helpBox);
            GUI.color = oldColor;
        }
    }




}