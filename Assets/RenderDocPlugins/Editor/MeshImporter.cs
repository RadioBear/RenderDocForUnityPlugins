using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEditor;

namespace RenderDocPlugins
{
    public class MeshImporter : EditorWindow
    {
        private const int k_MinSubMeshCount = 1;
        private const int k_MaxSubMeshCount = 8;

        private const int k_WindowWidth = 500;
        private const int k_WindowHeight = 400;

        [SerializeField]
        private Vector2 m_ScrollPos;

        [SerializeField]
        private int m_SubMeshCount = k_MinSubMeshCount;

        [SerializeField]
        private string[] m_SourcePath;

        [SerializeField]
        private string m_DestPath = string.Empty;

        [SerializeField]
        private bool m_AutoCalcNormalIfNotExist = false;

        [SerializeField]
        private bool m_AutoCalcTangentIfNotExist = false;

        [SerializeField]
        private bool m_OptimizesRendering = true;

        [SerializeField]
        private ModelImporterMeshCompression m_MeshCompression = ModelImporterMeshCompression.Off;

        [MenuItem("RenderDocPlugins/Import Mesh From CSV")]
        public static void DoImportMeshFromCSV()
        {
            var win = EditorWindow.GetWindow<MeshImporter>();
            if (win != null)
            {
                win.ShowAuxWindow();
            }
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("Mesh Importer");
            minSize = new Vector2(k_WindowWidth, k_WindowHeight);
            if (m_SourcePath == null || m_SourcePath.Length != k_MaxSubMeshCount)
            {
                m_SourcePath = new string[k_MaxSubMeshCount];
            }
        }


        private void OnGUI()
        {
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
                            GUILayout.Label("not select source csv file. Or you can drop csv file to here.", GUILayout.ExpandWidth(false), GUILayout.MinWidth(0f));
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
                    }
                }
                EditorGUILayout.EndHorizontal();
                GUILayout.FlexibleSpace();
            }
            EditorGUILayout.EndVertical();

            m_AutoCalcNormalIfNotExist = EditorGUILayout.ToggleLeft("Auto calculate normal if not exist", m_AutoCalcNormalIfNotExist);
            m_AutoCalcTangentIfNotExist = EditorGUILayout.ToggleLeft("Auto calculate tangent if not exist", m_AutoCalcTangentIfNotExist);
            m_OptimizesRendering = EditorGUILayout.ToggleLeft("Optimizes the Mesh data to improve rendering performance.", m_OptimizesRendering);
            m_MeshCompression = (ModelImporterMeshCompression)EditorGUILayout.EnumPopup("Mesh Compression", m_MeshCompression, GUILayout.ExpandWidth(false));


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


        private void DoChangeSourceCSV(int index, string fileFullPath)
        {
            if (index >= 0 && index < m_SourcePath.Length)
            {
                m_SourcePath[index] = fileFullPath;
            }
        }

        private void DoChangeDestMesh(string assetPath)
        {
            m_DestPath = assetPath;
        }

        private void DoGenerateMesh()
        {
            var setting = new CSVToMeshGenerator.GenSetting();
            setting.flags = CSVToMeshGenerator.Flags.None;
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
            setting.compression = m_MeshCompression;

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
    }




}