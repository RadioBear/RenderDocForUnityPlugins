using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace RenderDocPlugins
{
    [System.Serializable]
    public class VertexAttributeMappingData
    {
        [SerializeField]
        public bool m_Enable;

        [SerializeField]
        public string m_VertexAttributeName;

        [SerializeField]
        public UnityEngine.Rendering.VertexAttribute m_Attr;
    }

    [System.Serializable]
    public class VertexAttributeMappingPreset
    {
        [SerializeField]
        public List<VertexAttributeMappingData> m_Datas = new List<VertexAttributeMappingData>();

        public VertexAttributeMappingData GetData(string attrName)
        {
            for(int i = 0; i < m_Datas.Count; ++i)
            {
                if(string.Equals(m_Datas[i].m_VertexAttributeName, attrName, System.StringComparison.Ordinal))
                {
                    return m_Datas[i];
                }
            }
            return null;
        }
    }

    public class VertexAttributeMapping
    {
        private const string k_PresetExt = ".vam";
        private const string k_PresetPath = "VertexAttrMappingPreset";

        internal static string preferencesPath => System.IO.Path.Combine(System.IO.Path.GetDirectoryName(InternalEditorUtility.GetAssetsFolder()), k_PresetPath);

        public static void GetAllPresetNames(List<string> list)
        {
            if (System.IO.Directory.Exists(preferencesPath))
            {
                var presetPaths = System.IO.Directory.GetFiles(preferencesPath).Where(path => path.EndsWith(k_PresetExt)).ToArray();
                foreach (var presetPath in presetPaths)
                {
                    var name = System.IO.Path.GetFileNameWithoutExtension(presetPath);
                    list.Add(name);
                }
            }
        }

        public static string GetPresetFullPath(string name)
        {
            return System.IO.Path.Combine(preferencesPath, name + k_PresetExt);
        }

        public static VertexAttributeMappingPreset ReadPreset(string name)
        {
            string path = VertexAttributeMapping.GetPresetFullPath(name);
            if(System.IO.File.Exists(path))
            {
                var json = System.IO.File.ReadAllText(path);
                if(!string.IsNullOrEmpty(json))
                {
                    VertexAttributeMappingPreset preset = new VertexAttributeMappingPreset();
                    EditorJsonUtility.FromJsonOverwrite(json, preset);
                    return preset;
                }
            }
            return null;
        }

        public static void DeletePreset(string name)
        {
            var path = GetPresetFullPath(name);
            if(System.IO.File.Exists(path))
            {
                System.IO.File.Delete(path);
            }
        }

        public static void SavePreset(VertexAttributeMappingPreset preset, string name)
        {
            if (preset == null)
            {
                return;
            }
            if (string.IsNullOrEmpty(name))
            {
                return;
            }
            if (!System.IO.Directory.Exists(VertexAttributeMapping.preferencesPath))
            {
                System.IO.Directory.CreateDirectory(VertexAttributeMapping.preferencesPath);
            }

            string path = VertexAttributeMapping.GetPresetFullPath(name);

            var json = EditorJsonUtility.ToJson(preset, true);
            System.IO.File.WriteAllText(path, json);
        }
    }
}