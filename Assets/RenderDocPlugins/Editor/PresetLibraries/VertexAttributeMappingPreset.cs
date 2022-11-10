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
        public VertexAttributeMapping Mapping;
    }

    [System.Serializable]
    public class VertexAttributeMappingPreset
    {
        [SerializeField]
        public List<VertexAttributeMappingData> m_Datas = new();

        public VertexAttributeMappingData GetData(string attrName)
        {
            for(int i = 0; i < m_Datas.Count; ++i)
            {
                if(string.Equals(m_Datas[i].Mapping.Name, attrName, System.StringComparison.Ordinal))
                {
                    return m_Datas[i];
                }
            }
            return null;
        }
    }

    public class VertexAttributeMappingPresetManager
    {
        private const string k_PresetExt = ".vam";
        private const string k_PresetPath = "VertexAttrMappingPreset";

        internal static string PreferencesPath => System.IO.Path.Combine(System.IO.Path.GetDirectoryName(InternalEditorUtility.GetAssetsFolder()), k_PresetPath);

        public static void GetAllPresetNames(List<string> list)
        {
            if (System.IO.Directory.Exists(PreferencesPath))
            {
                var presetPaths = System.IO.Directory.GetFiles(PreferencesPath).Where(path => path.EndsWith(k_PresetExt)).ToArray();
                foreach (var presetPath in presetPaths)
                {
                    var name = System.IO.Path.GetFileNameWithoutExtension(presetPath);
                    list.Add(name);
                }
            }
        }

        public static string GetPresetFullPath(string name)
        {
            return System.IO.Path.Combine(PreferencesPath, name + k_PresetExt);
        }

        public static VertexAttributeMappingPreset ReadPreset(string name)
        {
            string path = GetPresetFullPath(name);
            if(System.IO.File.Exists(path))
            {
                var json = System.IO.File.ReadAllText(path);
                if(!string.IsNullOrEmpty(json))
                {
                    VertexAttributeMappingPreset preset = new();
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
            if (!System.IO.Directory.Exists(VertexAttributeMappingPresetManager.PreferencesPath))
            {
                System.IO.Directory.CreateDirectory(VertexAttributeMappingPresetManager.PreferencesPath);
            }

            string path = VertexAttributeMappingPresetManager.GetPresetFullPath(name);

            var json = EditorJsonUtility.ToJson(preset, true);
            System.IO.File.WriteAllText(path, json);
        }
    }
}