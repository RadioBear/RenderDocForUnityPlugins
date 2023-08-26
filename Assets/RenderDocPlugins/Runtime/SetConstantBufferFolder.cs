using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;


namespace RenderDocPlugins
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(Renderer))]
    public class SetConstantBufferFolder : MonoBehaviour
    {
        private static MaterialPropertyBlock s_MPB = null;
        private static readonly List<Material> s_MaterialList = new List<Material>();

        private static MaterialPropertyBlock CreateOrGetMPB()
        {
            s_MPB ??= new MaterialPropertyBlock();
            return s_MPB;
        }
        private abstract class Scope : IDisposable
        {
            private bool m_Disposed;

            internal virtual void Dispose(bool disposing)
            {
                if (!m_Disposed)
                {
                    if (disposing)
                    {
                        CloseScope();
                    }

                    m_Disposed = true;
                }
            }

            ~Scope()
            {
                if (!m_Disposed)
                {
                    Console.WriteLine(GetType().Name + " was not disposed! You should use the 'using' keyword or manually call Dispose.");
                }

                Dispose(disposing: false);
            }

            public void Dispose()
            {
                Dispose(disposing: true);
                GC.SuppressFinalize(this);
            }

            protected abstract void CloseScope();
        }
        private class MaterialPropertyBlockScope : Scope
        {
            public MaterialPropertyBlockScope(out MaterialPropertyBlock target)
            {
                target = CreateOrGetMPB();
            }

            protected override void CloseScope()
            {
                s_MPB.Clear();
            }
        }

        [System.Serializable]
        public class CSVConstantBuffer
        {
            // render doc csv format
            private const string k_CSVHead_Name = "Name";
            private const string k_CSVHead_Value = "Value";
            private const string k_CSVHead_Type = "Type";
            private const string k_CSVHead_ByteOffset = "Byte Offset";

            // read from csv asset
            private TextAsset m_CSV;
            public TextAsset CSVAsset
            {
                get { return m_CSV; }
            }

            // constant buffer name to set
            private string m_BufferName = string.Empty;
            public string BufferName
            {
                get { return m_BufferName; }
            }

            private GraphicsBuffer m_Buffer = null;
            public GraphicsBuffer Buffer
            {
                get { return m_Buffer; }
            }

            private bool m_Expanded = false;
            public bool Expanded
            {
                get { return m_Expanded; }
                set { m_Expanded = value; }
            }

            private Vector2 m_ScrollPos = Vector2.zero;
            public Vector2 ScrollPos
            {
                get { return m_ScrollPos; }
                set { m_ScrollPos = value; }
            }

            public CSVConstantBuffer(TextAsset text)
            {
                m_CSV = text;
                UpdateBuffer();
            }

            public void SetTo(MaterialPropertyBlock mpb)
            {
                if (m_Buffer != null && !string.IsNullOrEmpty(m_BufferName))
                {
                    mpb.SetConstantBuffer(m_BufferName, m_Buffer, 0, m_Buffer.count * m_Buffer.stride);
                }
            }

            //public NativeArray<Vector4> LockBufferForWrite()
            //{
            //    if (m_Buffer != null)
            //    {
            //        return m_Buffer.LockBufferForWrite<Vector4>(0, m_Buffer.count);
            //    }
            //    return new NativeArray<Vector4>();
            //}

            //public void UnlockBufferAfterWrite(int countWritten)
            //{
            //    if (m_Buffer != null)
            //    {
            //        m_Buffer.UnlockBufferAfterWrite<Vector4>(countWritten);
            //    }
            //}

            public void Dispose()
            {
                ReleaseBuffer();
                m_CSV = null;
                m_BufferName = string.Empty;
            }

            /// <summary>
            /// read csv and create GraphicsBuffer
            /// </summary>
            public void UpdateBuffer()
            {
                ReleaseBuffer();
                if (m_CSV == null)
                {
                    return;
                }
                var data = CreateDataFromCSV(m_CSV.text, Allocator.Temp);
                if (data.Length > 0)
                {
                    // Number of elements in the buffer.
                    // Size of one element in the buffer.
                    m_Buffer = new GraphicsBuffer(GraphicsBuffer.Target.Constant, GraphicsBuffer.UsageFlags.None, data.Length, UnsafeUtility.SizeOf<Vector4>());
                    m_Buffer.SetData<Vector4>(data);
                    m_Buffer.name = m_CSV.name;
                    m_BufferName = m_CSV.name;
                }
                if (data.IsCreated)
                {
                    data.Dispose();
                }
            }

            /// <summary>
            /// release GraphicsBuffer
            /// </summary>
            private void ReleaseBuffer()
            {
                if (m_Buffer != null)
                {
                    m_Buffer.Dispose();
                    m_Buffer = null;
                }
            }

            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            struct CSVIndexData
            {
                public int NameIndex;
                public int ValueIndex;
                public int TypeIndex;
                public int ByteOffset;

                public void Reset()
                {
                    NameIndex = -1;
                    ValueIndex = -1;
                    TypeIndex = -1;
                    ByteOffset = -1;
                }
            }

            /// <summary>
            /// read csv to NativeArray
            /// </summary>
            /// <param name="csvStr"></param>
            /// <param name="allocator"></param>
            /// <returns></returns>
            private NativeArray<Vector4> CreateDataFromCSV(string csvStr, Allocator allocator)
            {
                CSVIndexData indexData = new();

                if (!GetDataCountFromCSV(csvStr, ref indexData, out int dataCount))
                {
                    return new NativeArray<Vector4>();
                }
                if (dataCount <= 0)
                {
                    return new NativeArray<Vector4>();
                }
                if (indexData.ValueIndex == -1)
                {
                    return new NativeArray<Vector4>();
                }
                var data = new NativeArray<Vector4>(dataCount, allocator, NativeArrayOptions.ClearMemory);
                using (System.IO.StringReader strReader = new System.IO.StringReader(csvStr))
                {
                    dataCount = 0;
                    strReader.ReadLine();
                    var line = strReader.ReadLine();
                    while (!string.IsNullOrEmpty(line) && (dataCount < data.Length))
                    {
                        var valStr = CSV.GetCSVString(line, indexData.ValueIndex);
                        if (!string.IsNullOrEmpty(valStr))
                        {
                            data[dataCount] = GetValueFromCSV(valStr);
                        }
                        ++dataCount;
                        line = strReader.ReadLine();
                    }
                }
                return data;
            }

            private bool GetDataCountFromCSV(string csvStr, ref CSVIndexData indexData, out int outCount)
            {
                indexData.Reset();

                int dataCount = 0;
                using (System.IO.StringReader strReader = new System.IO.StringReader(csvStr))
                {
                    var line = strReader.ReadLine();
                    GetCSVIndexData(line, ref indexData);
                    line = strReader.ReadLine();
                    while (!string.IsNullOrEmpty(line))
                    {
                        ++dataCount;
                        line = strReader.ReadLine();
                    }
                }

                outCount = dataCount;
                return true;
            }

            private static void GetCSVIndexData(string csvLine, ref CSVIndexData data)
            {
                var strArr = csvLine.Split(CSV.k_Spliter);
                for (int i = 0; i < strArr.Length; ++i)
                {
                    var cur = strArr[i].Trim();
                    if (cur.Equals(k_CSVHead_Value, System.StringComparison.Ordinal))
                    {
                        data.ValueIndex = i;
                    }
                    else if (cur.Equals(k_CSVHead_Name, System.StringComparison.Ordinal))
                    {
                        data.NameIndex = i;
                    }
                    else if (cur.Equals(k_CSVHead_Type, System.StringComparison.Ordinal))
                    {
                        data.TypeIndex = i;
                    }
                    else if (cur.Equals(k_CSVHead_ByteOffset, System.StringComparison.Ordinal))
                    {
                        data.ByteOffset = i;
                    }
                    else
                    {
                        UnityEngine.Debug.LogError($"Not support csv herder: {cur}");
                    }
                }

            }

            /// <summary>
            /// can get value from string float or float2 or float3 or float4
            /// </summary>
            /// <param name="strValue"></param>
            /// <returns></returns>
            private Vector4 GetValueFromCSV(string strValue)
            {
                strValue = strValue.Trim();
                strValue = strValue.Trim('"');
                var valueStrArray = strValue.Split(CSV.k_Spliter);
                Vector4 data = Vector4.zero;
                for (int i = 0; i < 4; ++i)
                {
                    if (i >= valueStrArray.Length)
                    {
                        break;
                    }
                    var blockStr = valueStrArray[i].Trim();
                    if (float.TryParse(blockStr, out float val))
                    {
                        if (float.IsNaN(val) || float.IsInfinity(val))
                        {
                            Debug.LogError($"IsNaN Or IsInfinity {blockStr}");
                            val = 0.0f;
                        }
                        data[i] = val;
                    }
                    else
                    {
                        Debug.LogError($"float.TryParse fail {blockStr}, from '{strValue}'");
                    }

                }
                return data;
            }
        }

        [System.Serializable]
        class MaterialConstantBuffer
        {
            public const int k_INDEX_ALL = -1;

            // what folder contain csv asset
            [SerializeField]
            private UnityEditor.DefaultAsset m_FolderObject = null;

            // which material to sets
            // -1 is all material
            [SerializeField]
            private int m_MaterialIndex = k_INDEX_ALL;

            // 记录之前设置的目录
            [System.NonSerialized]
            private UnityEditor.DefaultAsset m_TargetFolderObject = null;

            // MaterialPropertyBlock数据记录在renderer中，切换material变化并不影响数据
            [System.NonSerialized]
            private Renderer m_TargetRenderer = null;
            // which material has been set
            [System.NonSerialized]
            private int m_TargetMaterialIndex = k_INDEX_ALL;
            // GraphicBuffer container
            [System.NonSerialized]
            private readonly List<CSVConstantBuffer> m_BufferList = new();

            public void CheckUpdate(Renderer renderer)
            {
                bool update = false;
                if (m_TargetFolderObject != m_FolderObject || !UnityEngine.Object.ReferenceEquals(m_TargetFolderObject, m_FolderObject))
                {
                    // folder is changed
                    m_TargetFolderObject = m_FolderObject;
                    LoadAllBuffer();
                    update = true;
                }
                if (m_TargetRenderer != renderer || !UnityEngine.Object.ReferenceEquals(m_TargetRenderer, renderer))
                {
                    m_TargetRenderer = renderer;
                    update = true;
                }
                if (m_MaterialIndex != m_TargetMaterialIndex)
                {
                    m_TargetMaterialIndex = m_MaterialIndex;
                    update = true;
                }
                if (update)
                {
                    UpdateAllConstantBuffer();
                }
            }

            public int GetConstantBufferCount()
            {
                return m_BufferList.Count;
            }

            public List<CSVConstantBuffer> GetConstantBuffers()
            {
                return m_BufferList;
            }

            public void UpdateConstantBuffer(Renderer renderer)
            {
                m_TargetFolderObject = m_FolderObject;
                LoadAllBuffer();
                m_TargetRenderer = renderer;
                m_TargetMaterialIndex = m_MaterialIndex;
                UpdateAllConstantBuffer();
            }

            public void UpdateConstantBuffer(Renderer renderer, int bufferIndex)
            {
                if (bufferIndex >= 0 && bufferIndex < m_BufferList.Count)
                {
                    m_BufferList[bufferIndex].UpdateBuffer();
                    using (new MaterialPropertyBlockScope(out MaterialPropertyBlock mpb))
                    {
                        if (m_TargetRenderer.HasPropertyBlock())
                        {
                            if (m_TargetMaterialIndex >= 0)
                            {
                                m_TargetRenderer.GetPropertyBlock(mpb, m_TargetMaterialIndex);
                            }
                            else
                            {
                                m_TargetRenderer.GetPropertyBlock(mpb);
                            }
                        }
                        m_BufferList[bufferIndex].SetTo(mpb);
                        if (m_TargetMaterialIndex >= 0)
                        {
                            m_TargetRenderer.SetPropertyBlock(mpb, m_TargetMaterialIndex);
                        }
                        else
                        {
                            m_TargetRenderer.SetPropertyBlock(mpb);
                        }
                    }
                }
            }

            /// <summary>
            /// set GraphicBuffer to Renderer
            /// </summary>
            private void UpdateAllConstantBuffer()
            {
                if(!m_TargetRenderer)
                {
                    return;
                }
                if (m_TargetMaterialIndex >= 0)
                {
                    int count = GetMaterialCountInRenderer(m_TargetRenderer);
                    if (m_TargetMaterialIndex >= count)
                    {
                        return;
                    }
                }

                if(m_BufferList.Count > 0)
                {
                    using (new MaterialPropertyBlockScope(out MaterialPropertyBlock mpb))
                    {
                        if (m_TargetRenderer.HasPropertyBlock())
                        {
                            if (m_TargetMaterialIndex >= 0)
                            {
                                m_TargetRenderer.GetPropertyBlock(mpb, m_TargetMaterialIndex);
                            }
                            else
                            {
                                m_TargetRenderer.GetPropertyBlock(mpb);
                            }
                        }
                        for(int i = 0; i < m_BufferList.Count; ++i)
                        {
                            m_BufferList[i].SetTo(mpb);
                        }
                        if (m_TargetMaterialIndex >= 0)
                        {
                            m_TargetRenderer.SetPropertyBlock(mpb, m_TargetMaterialIndex);
                        }
                        else
                        {
                            m_TargetRenderer.SetPropertyBlock(mpb);
                        }
                    }
                }
            }

            private void LoadAllBuffer()
            {
                foreach (var buffer in m_BufferList)
                {
                    buffer.Dispose();
                }
                m_BufferList.Clear();
                if (m_TargetFolderObject == null)
                {
                    return;
                }
                var asset_path = UnityEditor.AssetDatabase.GetAssetPath(m_TargetFolderObject);
                if (!UnityEditor.AssetDatabase.IsValidFolder(asset_path))
                {
                    // not a folder
                    return;
                }
                var paths = System.IO.Directory.GetFiles(asset_path, "*.csv", System.IO.SearchOption.TopDirectoryOnly);
                if (paths != null && paths.Length > 0)
                {
                    for (int i = 0; i < paths.Length; ++i)
                    {
                        var text_asset = UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>(paths[i]);
                        if (text_asset)
                        {
                            m_BufferList.Add(new CSVConstantBuffer(text_asset));
                        }
                    }
                }
            }
        }

        [SerializeField]
        private List<MaterialConstantBuffer> m_MaterialList = new();

        // Runtime
        private GameObject m_SelfGO = null;


        private void OnEnable()
        {
            m_SelfGO = this.gameObject;
        }

        private void Update()
        {
            CheckUpdate();
        }

        private static int GetMaterialCountInRenderer(Renderer renderer)
        {
            int count = 0;
            renderer.GetSharedMaterials(s_MaterialList);
            count = s_MaterialList.Count;
            s_MaterialList.Clear();
            return count;
        }

        private static Material GetMaterialInRenderer(Renderer renderer, int matIndex)
        {
            Material mat = null;
            if (matIndex >= 0)
            {
                renderer.GetSharedMaterials(s_MaterialList);
                if (matIndex < s_MaterialList.Count)
                {
                    mat = s_MaterialList[matIndex];
                }
                s_MaterialList.Clear();
            }
            return mat;
        }

        private void CheckUpdate()
        {
            if (m_MaterialList.Count > 0)
            {
                m_SelfGO.TryGetComponent<Renderer>(out Renderer targetRenderer);
                for (int i = 0; i < m_MaterialList.Count; ++i)
                {
                    if (m_MaterialList[i] != null)
                    {
                        m_MaterialList[i].CheckUpdate(targetRenderer);
                    }
                }
            }
        }

        public void UpdateConstantBuffer(int index)
        {
            if (index >= 0 && index < m_MaterialList.Count)
            {
                m_SelfGO.TryGetComponent<Renderer>(out Renderer targetRenderer);
                m_MaterialList[index].UpdateConstantBuffer(targetRenderer);
            }
        }

        public void UpdateConstantBuffer(int index, int bufferIndex)
        {
            if (index >= 0 && index < m_MaterialList.Count)
            {
                m_SelfGO.TryGetComponent<Renderer>(out Renderer targetRenderer);
                m_MaterialList[index].UpdateConstantBuffer(targetRenderer, bufferIndex);
            }
        }

        public int GetConstantBufferCount(int index)
        {
            if (index >= 0 && index < m_MaterialList.Count)
            {
                return m_MaterialList[index].GetConstantBufferCount();
            }
            return 0;
        }

        public List<CSVConstantBuffer> GetConstantBuffers(int index)
        {
            if (index >= 0 && index < m_MaterialList.Count)
            {
                return m_MaterialList[index].GetConstantBuffers();
            }
            return null;
        }
    }
}