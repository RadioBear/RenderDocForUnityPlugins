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
        private const int k_WindowWidth = 500;
        private const int k_WindowHeight = 400;

        [SerializeField]
        private string m_SourcePath;

        [SerializeField]
        private string m_DestPath;

        [SerializeField]
        private bool m_AutoCalcNormalIfNotExist;

        [SerializeField]
        private bool m_AutoCalcTangentIfNotExist;


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
        }


        private void OnGUI()
        {
            GUILayout.Space(10f);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.MinHeight(50f));
            {
                GUILayout.FlexibleSpace();
                EditorGUILayout.BeginHorizontal();
                {
                    GUILayout.Label("Source CSV", GUILayout.ExpandWidth(false));
                    GUILayout.Space(10f);
                    if (string.IsNullOrEmpty(m_SourcePath))
                    {
                        GUILayout.FlexibleSpace();
                        GUILayout.Label("not select source csv file. Or you can drop csv file to here.");
                        GUILayout.FlexibleSpace();
                    }
                    else
                    {
                        GUILayout.Label(m_SourcePath);
                    }
                    GUILayout.Space(10f);


                    if (GUILayout.Button("Select File...", GUILayout.ExpandWidth(false)))
                    {
                        string fileSelected = EditorUtility.OpenFilePanelWithFilters("Select source csv file", Application.dataPath, new string[] { "csv files", "csv", "All files", "*" });
                        if (!string.IsNullOrEmpty(fileSelected))
                        {
                            DoChangeSourceCSV(fileSelected);
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();
                GUILayout.FlexibleSpace();
            }
            EditorGUILayout.EndVertical();
            HandleDragAndDropGUI(GUILayoutUtility.GetLastRect());


            GUILayout.Space(10f);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.MinHeight(50f));
            {
                GUILayout.FlexibleSpace();
                EditorGUILayout.BeginHorizontal();
                {
                    GUILayout.Label("Destination Mesh", GUILayout.ExpandWidth(false));
                    GUILayout.Space(10f);
                    GUILayout.TextField(m_DestPath);
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

            m_AutoCalcNormalIfNotExist = EditorGUILayout.ToggleLeft("Auto cal normal if not exist", m_AutoCalcNormalIfNotExist);
            m_AutoCalcTangentIfNotExist = EditorGUILayout.ToggleLeft("Auto cal normal if not exist", m_AutoCalcTangentIfNotExist);

            GUILayout.FlexibleSpace();

            EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(m_SourcePath) || string.IsNullOrEmpty(m_DestPath));
            {
                if (GUILayout.Button("Generate Mesh", GUILayout.MinHeight(50f)))
                {
                    DoGenerateMesh();
                }
            }
            EditorGUI.EndDisabledGroup();

        }

        private void HandleDragAndDropGUI(Rect dragdropArea)
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
                                DoChangeSourceCSV(path);
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


        private void DoChangeSourceCSV(string fileFullPath)
        {
            m_SourcePath = fileFullPath;

        }

        private void DoChangeDestMesh(string assetPath)
        {
            m_DestPath = assetPath;
        }

        private void DoGenerateMesh()
        {
            CSVToMeshGenerator.Flags flags = CSVToMeshGenerator.Flags.None;
            if(m_AutoCalcNormalIfNotExist)
            {
                flags |= CSVToMeshGenerator.Flags.AutoCalcNormalIfNotExist;
            }
            if (m_AutoCalcTangentIfNotExist)
            {
                flags |= CSVToMeshGenerator.Flags.AutoCalcTangentIfNotExist;
            }
            CSVToMeshGenerator.GenerateMesh(m_SourcePath, m_DestPath, flags, Allocator.Temp);
        }
    }




}