using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEditor;

namespace RenderDocPlugins
{
    public class CSVToMeshGenerator
    {

        private const int k_MaxTexcoord = 8;

        private const char k_CSVSpliter = ',';


        [System.Flags]
        public enum Flags
        {
            None = 0x0,
            AutoCalcNormalIfNotExist = 0x1,
            AutoCalcTangentIfNotExist = 0x2,
            OptimizesRendering = 0x4,
            ReadWriteEnable = 0x8,
            FlipVertexWindingOrder = 0x10,
        }

        public struct VertexAttributeMapping
        {
            public string Name;
            public VertexAttribute Attr;
            public bool Disable;
        }

        public struct GenSetting
        {
            public Flags flags;
            public ModelImporterMeshCompression compression;
            public VertexAttributeMapping[] vertexAttrMapping;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct Vec3Index
        {
            public int X;
            public int Y;
            public int Z;

            public void Reset()
            {
                X = -1;
                Y = -1;
                Z = -1;
            }

            public bool IsValidAllComponent()
            {
                return (X != -1) && (Y != -1) && (Z != -1);
            }

            public int Length
            {
                get { return 3; }
            }

            public bool IsValidIndex(int index)
            {
                return index >= 0 && index < 3;
            }

            public int this[int index]
            {
                get
                {
                    switch (index)
                    {
                        case 0: return X;
                        case 1: return Y;
                        case 2: return Z;
                        default:
                            throw new System.IndexOutOfRangeException(string.Format("Invalid Vec3Index index addressed: {0}!", index));
                    }
                }

                set
                {
                    switch (index)
                    {
                        case 0: X = value; break;
                        case 1: Y = value; break;
                        case 2: Z = value; break;
                        default:
                            throw new System.IndexOutOfRangeException(string.Format("Invalid Vec3Index index addressed: {0}!", index));
                    }
                }
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct Vec4Index
        {
            public int X;
            public int Y;
            public int Z;
            public int W;

            public void Reset()
            {
                X = -1;
                Y = -1;
                Z = -1;
                W = -1;
            }

            public bool IsValidAllComponent()
            {
                return (X != -1) && (Y != -1) && (Z != -1) && (W != -1);
            }

            public int GetValidComponentLength()
            {
                if (X != -1)
                {
                    if (Y != -1)
                    {
                        if (Z != -1)
                        {
                            if (W != -1)
                            {
                                return 4;
                            }
                            return 3;
                        }
                        return 2;
                    }
                    return 1;
                }
                return 0;
            }

            public int Length
            {
                get { return 4; }
            }

            public bool IsValidIndex(int index)
            {
                return index >= 0 && index < 4;
            }

            public int this[int index]
            {
                get
                {
                    switch (index)
                    {
                        case 0: return X;
                        case 1: return Y;
                        case 2: return Z;
                        case 3: return W;
                        default:
                            throw new System.IndexOutOfRangeException(string.Format("Invalid Vec4Index index addressed: {0}!", index));
                    }
                }

                set
                {
                    switch (index)
                    {
                        case 0: X = value; break;
                        case 1: Y = value; break;
                        case 2: Z = value; break;
                        case 3: W = value; break;
                        default:
                            throw new System.IndexOutOfRangeException(string.Format("Invalid Vec4Index index addressed: {0}!", index));
                    }
                }
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct CSVIndexData : System.IDisposable
        {
            public int VertexNumIndex;
            public int IndicesMumIndex;
            public Vec3Index Pos;
            public Vec3Index Normal;
            public Vec4Index Tangent;
            public Vec4Index Color;
            public NativeArray<Vec4Index> Texcoord;
            public Vec4Index BlendWeight;
            public Vec4Index BlendIndices;

            #region Get Data


            public static int GetDataInt(int index, string[] str, int defaultVal)
            {
                if (index != -1)
                {
                    var blockStr = str[index].Trim();
                    if (!string.IsNullOrEmpty(blockStr))
                    {
                        int val;
                        if (int.TryParse(blockStr, out val))
                        {
                            return val;
                        }
                    }
                }
                return defaultVal;
            }
            public static Vector3 GetDataVertor3(in Vec3Index vec3Index, string[] str, Vector3 defaultVal)
            {
                if (vec3Index.IsValidAllComponent())
                {
                    Vector3 data = defaultVal;
                    for (int i = 0; i < 3; ++i)
                    {
                        var blockStr = str[vec3Index[i]].Trim();
                        if (!string.IsNullOrEmpty(blockStr))
                        {
                            float val;
                            if (float.TryParse(blockStr, out val))
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
                                Debug.LogError($"float.TryParse fail {blockStr}");
                            }
                        }
                    }
                    return data;
                }
                return defaultVal;
            }
            public static Vector4 GetDataVertor4(in Vec4Index vec4Index, string[] str, Vector4 defaultVal)
            {
                if (vec4Index.IsValidAllComponent())
                {
                    Vector4 data = defaultVal;
                    for (int i = 0; i < 3; ++i)
                    {
                        var blockStr = str[vec4Index[i]].Trim();
                        if (!string.IsNullOrEmpty(blockStr))
                        {
                            float val;
                            if (float.TryParse(blockStr, out val))
                            {
                                if (float.IsNaN(val) || float.IsInfinity(val))
                                {
                                    Debug.LogError($"IsNaN Or IsInfinity {blockStr}");
                                    val = 0.0f;
                                }
                                data[i] = val;
                            }
                        }
                    }
                    return data;
                }
                return defaultVal;
            }
            public static Vector2 GetDataVertor2(in Vec4Index vec4Index, string[] str, Vector2 defaultVal)
            {
                if (vec4Index.GetValidComponentLength() >= 2)
                {
                    Vector2 data = defaultVal;
                    for (int i = 0; i < 2; ++i)
                    {
                        var blockStr = str[vec4Index[i]].Trim();
                        if (!string.IsNullOrEmpty(blockStr))
                        {
                            float val;
                            if (float.TryParse(blockStr, out val))
                            {
                                if (float.IsNaN(val) || float.IsInfinity(val))
                                {
                                    Debug.LogError($"IsNaN Or IsInfinity {blockStr}");
                                    val = 0.0f;
                                }
                                data[i] = val;
                            }
                        }
                    }
                    return data;
                }
                return defaultVal;
            }
            public static Vector3 GetDataVertor3(in Vec4Index vec4Index, string[] str, Vector3 defaultVal)
            {
                if (vec4Index.GetValidComponentLength() >= 3)
                {
                    Vector3 data = defaultVal;
                    for (int i = 0; i < 3; ++i)
                    {
                        var blockStr = str[vec4Index[i]].Trim();
                        if (!string.IsNullOrEmpty(blockStr))
                        {
                            float val;
                            if (float.TryParse(blockStr, out val))
                            {
                                if (float.IsNaN(val) || float.IsInfinity(val))
                                {
                                    Debug.LogError($"IsNaN Or IsInfinity {blockStr}");
                                    val = 0.0f;
                                }
                                data[i] = val;
                            }
                        }
                    }
                    return data;
                }
                return defaultVal;
            }
            public static Vector4Int GetDataVertor4Int(in Vec4Index vec4Index, string[] str, Vector4Int defaultVal)
            {
                if (vec4Index.IsValidAllComponent())
                {
                    Vector4Int data = defaultVal;
                    for (int i = 0; i < 4; ++i)
                    {
                        var blockStr = str[vec4Index[i]].Trim();
                        if (!string.IsNullOrEmpty(blockStr))
                        {
                            int val;
                            if (int.TryParse(blockStr, out val))
                            {
                                if (float.IsNaN(val) || float.IsInfinity(val))
                                {
                                    Debug.LogError($"IsNaN Or IsInfinity {blockStr}");
                                    val = 0;
                                }
                                data[i] = val;
                            }
                        }
                    }
                    return data;
                }
                return defaultVal;
            }
            #endregion

            public CSVIndexData(Allocator allocator)
            {
                VertexNumIndex = -1;
                IndicesMumIndex = -1;
                Pos = new Vec3Index();
                Normal = new Vec3Index();
                Tangent = new Vec4Index();
                Color = new Vec4Index();
                Texcoord = new NativeArray<Vec4Index>(k_MaxTexcoord, allocator);
                BlendWeight = new Vec4Index();
                BlendIndices = new Vec4Index();

                Reset();
            }

            public void SetPos(int index, int pos)
            {
                if (Pos.IsValidIndex(index))
                {
                    Pos[index] = pos;
                }
            }

            public void SetNormal(int index, int pos)
            {
                if (Normal.IsValidIndex(index))
                {
                    Normal[index] = pos;
                }
            }

            public void SetTangent(int index, int pos)
            {
                if (Tangent.IsValidIndex(index))
                {
                    Tangent[index] = pos;
                }
            }

            public void SetColor(int index, int pos)
            {
                if (Color.IsValidIndex(index))
                {
                    Color[index] = pos;
                }
            }

            public void SetTexcoord(int texIndex, int index, int pos)
            {
                if (texIndex >= 0 && texIndex < Texcoord.Length)
                {
                    if (Texcoord[texIndex].IsValidIndex(index))
                    {
                        var texcorrd = Texcoord[texIndex];
                        texcorrd[index] = pos;
                        Texcoord[texIndex] = texcorrd;
                    }
                }
            }

            public void SetBlendWeight(int index, int pos)
            {
                if (BlendWeight.IsValidIndex(index))
                {
                    BlendWeight[index] = pos;
                }
            }

            public void SetBlendIndices(int index, int pos)
            {
                if (BlendIndices.IsValidIndex(index))
                {
                    BlendIndices[index] = pos;
                }
            }

            public NativeArray<VertexAttributeDescriptor> GetVertexAttributes(Allocator allocator)
            {
                int count = 0;
                if (Pos.IsValidAllComponent()) { ++count; }
                if (Normal.IsValidAllComponent()) { ++count; }
                if (Tangent.IsValidAllComponent()) { ++count; }
                if (Color.IsValidAllComponent()) { ++count; }
                for (int i = 0; i < Texcoord.Length; ++i)
                {
                    var len = Texcoord[i].GetValidComponentLength();
                    if (len > 0)
                    {
                        ++count;
                    }
                }
                if (BlendWeight.IsValidAllComponent()) { ++count; }
                if (BlendIndices.IsValidAllComponent()) { ++count; }

                NativeArray<VertexAttributeDescriptor> array = new NativeArray<VertexAttributeDescriptor>(count, allocator);
                count = 0;
                if (Pos.IsValidAllComponent())
                {
                    array[count] = new VertexAttributeDescriptor(
                        VertexAttribute.Position,
                        VertexAttributeFormat.Float32,
                        3,
                        0
                        );
                    ++count;
                }
                if (Normal.IsValidAllComponent())
                {
                    array[count] = new VertexAttributeDescriptor(
                        VertexAttribute.Normal,
                        VertexAttributeFormat.Float32,
                        3,
                        0
                        );
                    ++count;
                }
                if (Tangent.IsValidAllComponent())
                {
                    array[count] = new VertexAttributeDescriptor(
                        VertexAttribute.Tangent,
                        VertexAttributeFormat.Float32,
                        4,
                        0
                        );
                    ++count;
                }
                if (Color.IsValidAllComponent())
                {
                    array[count] = new VertexAttributeDescriptor(
                        VertexAttribute.Color,
                        VertexAttributeFormat.Float32,
                        4,
                        0
                        );
                    ++count;
                }
                for (int i = 0; i < Texcoord.Length; ++i)
                {
                    var len = Texcoord[i].GetValidComponentLength();
                    if (len > 0)
                    {
                        array[count] = new VertexAttributeDescriptor(
                        GetTexcoordAttribute(i),
                        VertexAttributeFormat.Float32,
                        len,
                        0
                        );
                        ++count;
                    }
                }
                if (BlendWeight.IsValidAllComponent())
                {
                    array[count] = new VertexAttributeDescriptor(
                        VertexAttribute.BlendWeight,
                        VertexAttributeFormat.Float32,
                        4,
                        0
                        );
                    ++count;
                }
                if (BlendIndices.IsValidAllComponent())
                {
                    array[count] = new VertexAttributeDescriptor(
                        VertexAttribute.BlendIndices,
                        VertexAttributeFormat.SInt32,
                        4,
                        0
                        );
                    ++count;
                }
                return array;
            }

            public bool IsValid()
            {
                return (VertexNumIndex != -1) && (IndicesMumIndex != -1) && Pos.IsValidAllComponent();
            }

            public void Reset()
            {
                VertexNumIndex = -1;
                IndicesMumIndex = -1;
                Pos.Reset();
                Normal.Reset();
                Tangent.Reset();
                Color.Reset();
                for (int i = 0; i < Texcoord.Length; ++i)
                {
                    var texcoord = Texcoord[i];
                    texcoord.Reset();
                    Texcoord[i] = texcoord;
                }
                BlendWeight.Reset();
                BlendIndices.Reset();
            }

            public void Dispose()
            {
                Texcoord.Dispose();
            }

            public static VertexAttribute GetTexcoordAttribute(int index)
            {
                switch (index)
                {
                    case 0: return VertexAttribute.TexCoord0;
                    case 1: return VertexAttribute.TexCoord1;
                    case 2: return VertexAttribute.TexCoord2;
                    case 3: return VertexAttribute.TexCoord3;
                    case 4: return VertexAttribute.TexCoord4;
                    case 5: return VertexAttribute.TexCoord5;
                    case 6: return VertexAttribute.TexCoord6;
                    case 7: return VertexAttribute.TexCoord7;
                }
                throw new System.IndexOutOfRangeException();
            }

            public static int GetTexcoordIndex(VertexAttribute attr)
            {
                switch (attr)
                {
                    case VertexAttribute.TexCoord0: return 0;
                    case VertexAttribute.TexCoord1: return 1;
                    case VertexAttribute.TexCoord2: return 2;
                    case VertexAttribute.TexCoord3: return 3;
                    case VertexAttribute.TexCoord4: return 4;
                    case VertexAttribute.TexCoord5: return 5;
                    case VertexAttribute.TexCoord6: return 6;
                    case VertexAttribute.TexCoord7: return 7;
                }
                throw new System.IndexOutOfRangeException();
            }
        }

        struct CSVMeshInfo : System.IDisposable
        {
            public int BaseVertexIndex;
            public int VertexCount;
            public int IndexCount;
            public readonly int SubMeshCount;
            public NativeArray<SubMeshDescriptor> SubMeshDescArray;

            public CSVMeshInfo(int subMeshCount, Allocator allocator)
            {
                BaseVertexIndex = 0;
                VertexCount = 0;
                IndexCount = 0;
                SubMeshCount = subMeshCount;
                SubMeshDescArray = new NativeArray<SubMeshDescriptor>(subMeshCount, allocator);
            }

            public void PushSubMesh(NativeArray<CSVSubMeshInfo> subMeshInfoArray)
            {
                UnityEngine.Assertions.Assert.IsTrue(subMeshInfoArray.Length == SubMeshCount);

                int minVertexIndex = 0;
                int maxVertexIndex = 0;
                int indexCount = 0;
                for (int i = 0; i < subMeshInfoArray.Length; ++i)
                {
                    if (i == 0)
                    {
                        minVertexIndex = subMeshInfoArray[i].MinVertexIndex;
                        maxVertexIndex = subMeshInfoArray[i].MaxVertexIndex;
                    }
                    else
                    {
                        minVertexIndex = Mathf.Min(subMeshInfoArray[i].MinVertexIndex, minVertexIndex);
                        maxVertexIndex = Mathf.Max(subMeshInfoArray[i].MaxVertexIndex, maxVertexIndex);
                    }
                    var desc = new SubMeshDescriptor(indexCount, subMeshInfoArray[i].IndexCount, MeshTopology.Triangles);
                    SubMeshDescArray[i] = desc;
                    indexCount += subMeshInfoArray[i].IndexCount;
                }
                BaseVertexIndex = minVertexIndex;
                VertexCount = maxVertexIndex - minVertexIndex + 1;
                IndexCount = indexCount;
            }

            public void Dispose()
            {
                SubMeshDescArray.Dispose();
            }
        }

        struct CSVSubMeshInfo
        {
            public int MinVertexIndex;
            public int MaxVertexIndex;
            public int IndexCount;
        }

        struct VertexDataList : System.IDisposable
        {
            private bool dispose;
            private Mesh.MeshDataArray meshDataArray;
            private Mesh.MeshData meshData;
            private int indexCount;
            private int vertexIndexBase;
            private NativeArray<byte> vertexDataSet;
            private int vertexDataSetCount;
            private NativeArray<byte> indexDataSet;
            private int indexDataSetCount;
            private int offsetMappingBase;
            private NativeArray<int> offsetMapping;
            private int oneVertexDataTotalSize;

            public VertexDataList(NativeArray<VertexAttributeDescriptor> vertexDesc, int vertexCount, int indexCount, int vertexIndexBase, Allocator allocator)
            {
                dispose = false;
                meshDataArray = Mesh.AllocateWritableMeshData(1);
                meshData = meshDataArray[0];
                this.indexCount = indexCount;
                this.vertexIndexBase = vertexIndexBase;
                meshData.SetVertexBufferParams(vertexCount, vertexDesc);
                meshData.SetIndexBufferParams(indexCount, (indexCount > System.UInt16.MaxValue) ? IndexFormat.UInt32 : IndexFormat.UInt16);
                vertexDataSet = new NativeArray<byte>(vertexCount, allocator, NativeArrayOptions.ClearMemory);
                vertexDataSetCount = 0;
                indexDataSet = new NativeArray<byte>(indexCount, allocator, NativeArrayOptions.ClearMemory);
                indexDataSetCount = 0;
                {
                    int minIndex = 0;
                    int maxIndex = 0;
                    var enumArray = System.Enum.GetValues(typeof(VertexAttribute)) as int[];
                    for (int i = 0; i < enumArray.Length; ++i)
                    {
                        minIndex = Mathf.Min(enumArray[i], minIndex);
                        maxIndex = Mathf.Max(enumArray[i], maxIndex);
                    }
                    offsetMapping = new NativeArray<int>(maxIndex - minIndex + 1, allocator);
                    for (int i = 0; i < offsetMapping.Length; ++i)
                    {
                        offsetMapping[i] = -1;
                    }
                    int byteOffset = 0;
                    for (int i = 0; i < vertexDesc.Length; ++i)
                    {
                        var byteSize = Utils.GetVertexAttributeByteSize(vertexDesc[i]);
                        var attrValue = (int)vertexDesc[i].attribute;
                        offsetMapping[attrValue - minIndex] = byteOffset;
                        byteOffset += byteSize;
                    }
                    this.offsetMappingBase = minIndex;
                    this.oneVertexDataTotalSize = byteOffset;
                }
                unsafe
                {
                    var byteArray = meshData.GetVertexData<byte>(0);
                    UnsafeUtility.MemClear(NativeArrayUnsafeUtility.GetUnsafePtr(byteArray), byteArray.Length);
                }

                UnityEngine.Assertions.Assert.IsTrue(meshData.GetVertexData<byte>(0).Length == oneVertexDataTotalSize * vertexCount);
                UnityEngine.Assertions.Assert.IsTrue(meshData.GetIndexData<byte>().Length == ((meshData.indexFormat == IndexFormat.UInt32) ? (4 * indexCount) : (2 * indexCount)));
            }

            public IndexFormat GetIndexFormat()
            {
                return meshData.indexFormat;
            }

            public int GetVertexCount()
            {
                return meshData.vertexCount;
            }

            public int HasSetVertexDataCount()
            {
                return vertexDataSetCount;
            }

            public int HasSetIndexDataCount()
            {
                return indexDataSetCount;
            }

            public int GetIndexCount()
            {
                return indexCount;
            }

            public int GetVertexIndexBase()
            {
                return vertexIndexBase;
            }

            public bool IsSetVertexDataFinish()
            {
                return vertexDataSetCount == meshData.vertexCount;
            }
            public bool IsSetIndexDataFinish()
            {
                return indexDataSetCount == indexCount;
            }

            public void SetVertexData<T>(int index, VertexAttribute attr, T data) where T : struct
            {
                var byteOffset = offsetMapping[((int)attr) - offsetMappingBase];
                if (byteOffset != -1)
                {
                    var byteArray = meshData.GetVertexData<byte>(0);
                    byteArray.ReinterpretStore<T>((index * oneVertexDataTotalSize) + byteOffset, data);
                }
            }

            public void SetVertexIndexData(int indicesIndex, int vertexIndex)
            {
                if (GetIndexFormat() == IndexFormat.UInt16)
                {
                    SetVertexIndexData(indicesIndex, System.Convert.ToUInt16(vertexIndex));
                }
                else
                {
                    SetVertexIndexData(indicesIndex, System.Convert.ToUInt32(vertexIndex));
                }
            }

            private void SetVertexIndexData<T>(int index, T vertexIndex) where T : struct
            {
                var array = meshData.GetIndexData<T>();
                array[index] = vertexIndex;

                if (indexDataSet[index] == 0)
                {
                    ++indexDataSetCount;
                    indexDataSet[index] = 1;
                }
                else
                {
                    UnityEngine.Debug.LogError($"Repeat set index data. index:{index}");
                }
            }

            public void FlipVertexWindingOrder()
            {
                if (GetIndexFormat() == IndexFormat.UInt16)
                {
                    FlipVertexWindingOrder<System.UInt16>();
                }
                else
                {
                    FlipVertexWindingOrder<System.UInt32>();
                }
            }

            private void FlipVertexWindingOrder<T>() where T : struct
            {
                var array = meshData.GetIndexData<T>();
                int beginIndex = 0;
                while(beginIndex < array.Length)
                {
                    var order_0 = array[beginIndex];
                    var order_2 = array[beginIndex + 2];
                    array[beginIndex] = order_2;
                    array[beginIndex + 2] = order_0;
                    beginIndex += 3;
                }
            }

            public bool HasSetVertexData(int index)
            {
                return (vertexDataSet[index] != 0);
            }

            public int FindFirstNotSetVertexData()
            {
                for (int i = 0; i < vertexDataSet.Length; ++i)
                {
                    if (vertexDataSet[i] == 0)
                    {
                        return i;
                    }
                }
                return -1;
            }

            public int FindFirstNotSetIndexData()
            {
                for (int i = 0; i < indexDataSet.Length; ++i)
                {
                    if (indexDataSet[i] == 0)
                    {
                        return i;
                    }
                }
                return -1;
            }

            public void SetCompleteVertexData(int index)
            {
                if (vertexDataSet[index] == 0)
                {
                    ++vertexDataSetCount;
                    vertexDataSet[index] = 1;
                }
            }

            public void ApplyToMesh(Mesh mesh, in CSVMeshInfo csvMeshInfo)
            {
                meshData.subMeshCount = csvMeshInfo.SubMeshCount;
                for (int i = 0; i < csvMeshInfo.SubMeshCount; ++i)
                {
                    meshData.SetSubMesh(i, csvMeshInfo.SubMeshDescArray[i]);
                }
                Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, mesh);
                mesh.RecalculateBounds();
                dispose = true;
            }

            public void Dispose()
            {
                if (!dispose)
                {
                    meshDataArray.Dispose();
                    dispose = true;
                }
                vertexDataSet.Dispose();
                indexDataSet.Dispose();
                offsetMapping.Dispose();
            }

        }



        public static void GenerateMesh(string[] sourcePath, int subMeshCount, string targetAssetPath, in GenSetting genSetting, Allocator allocator)
        {
            for (int i = 0; i < subMeshCount; ++i)
            {
                if (!System.IO.File.Exists(sourcePath[i]))
                {
                    UnityEngine.Debug.LogError("not exist file:" + sourcePath[i]);
                    return;
                }
            }

            System.IO.StreamReader[] strReaderArray = new System.IO.StreamReader[subMeshCount];
            try
            {
                for (int i = 0; i < subMeshCount; ++i)
                {
                    strReaderArray[i] = new System.IO.StreamReader(sourcePath[i]);
                }

                bool succeed = false;
                var mesh = new Mesh();
                mesh.name = string.Empty;
                CSVIndexData csvIndexData = new CSVIndexData();
                CSVMeshInfo csvMeshInfo = new CSVMeshInfo();
                try
                {
                    csvIndexData = new CSVIndexData(allocator);
                    csvMeshInfo = new CSVMeshInfo(strReaderArray.Length, allocator);
                    if (!GetCSVIndexDataAndMeshInfo(strReaderArray, genSetting.vertexAttrMapping, ref csvIndexData, ref csvMeshInfo))
                    {
                        UnityEngine.Debug.LogError("Can not read csv properly.");
                        return;
                    }
                    if (csvMeshInfo.IndexCount < 3)
                    {
                        UnityEngine.Debug.LogError("Read vertexCount not properly.");
                        return;
                    }

                    var vertexAttrArray = csvIndexData.GetVertexAttributes(allocator);
                    var vertexDataList = new VertexDataList(vertexAttrArray, csvMeshInfo.VertexCount, csvMeshInfo.IndexCount, csvMeshInfo.BaseVertexIndex, allocator);
                    try
                    {
                        for (int i = 0; i < strReaderArray.Length; ++i)
                        {
                            var subMeshDesc = csvMeshInfo.SubMeshDescArray[i];
                            var strReader = strReaderArray[i];
                            var line = strReader.ReadLine();
                            while (!string.IsNullOrEmpty(line))
                            {
                                SetVertexData(line, in csvIndexData, in subMeshDesc, ref vertexDataList);
                                line = strReader.ReadLine();
                            }
                        }
                        //if (!vertexDataList.IsSetVertexDataFinish())
                        //{
                        //    UnityEngine.Debug.LogError($"not finish set Vertex Data. VertexCount:{vertexDataList.GetVertexCount()} SetCount:{vertexDataList.HasSetVertexDataCount()} FirstNotSet:{vertexDataList.FindFirstNotSetData()}");
                        //    return;
                        //}
                        if (!vertexDataList.IsSetIndexDataFinish())
                        {
                            UnityEngine.Debug.LogError($"not finish set Index Data. IndexCount:{vertexDataList.GetIndexCount()} SetCount:{vertexDataList.HasSetIndexDataCount()} FirstNotSet:{vertexDataList.FindFirstNotSetIndexData()}");
                            return;
                        }
                        if ((genSetting.flags & Flags.FlipVertexWindingOrder) != 0)
                        {
                            vertexDataList.FlipVertexWindingOrder();
                        }
                        vertexDataList.ApplyToMesh(mesh, csvMeshInfo);
                        if ((genSetting.flags & Flags.AutoCalcNormalIfNotExist) != 0)
                        {
                            if (!csvIndexData.Normal.IsValidAllComponent())
                            {
                                mesh.RecalculateNormals();
                            }
                        }
                        if ((genSetting.flags & Flags.AutoCalcTangentIfNotExist) != 0)
                        {
                            if (!csvIndexData.Tangent.IsValidAllComponent())
                            {
                                mesh.RecalculateTangents();
                            }
                        }
                        if ((genSetting.flags & Flags.OptimizesRendering) != 0)
                        {
                            MeshUtility.Optimize(mesh);
                        }
                        if (genSetting.compression != ModelImporterMeshCompression.Off)
                        {
                            MeshUtility.SetMeshCompression(mesh, genSetting.compression);
                        }
                    }
                    finally
                    {
                        vertexAttrArray.Dispose();
                        vertexDataList.Dispose();
                    }
                    succeed = true;
                    AssetDatabase.CreateAsset(mesh, targetAssetPath);
                    AssetDatabase.ImportAsset(targetAssetPath);
                    Selection.activeObject = mesh;
                    EditorGUIUtility.PingObject(mesh);
                }
                finally
                {
                    csvIndexData.Dispose();
                    csvMeshInfo.Dispose();
                    if (!succeed)
                    {
                        UnityEngine.Object.DestroyImmediate(mesh);
                    }
                }
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError(e);
            }
            finally
            {
                for (int i = 0; i < strReaderArray.Length; ++i)
                {
                    if (strReaderArray[i] != null)
                    {
                        strReaderArray[i].Close();
                        strReaderArray[i].Dispose();
                        strReaderArray[i] = null;
                    }
                }
            }
        }

        private static void SetVertexData(string line, in CSVIndexData indexData, in SubMeshDescriptor subMeshDesc, ref VertexDataList vertexDataList)
        {
            if (indexData.VertexNumIndex == -1)
            {
                return;
            }
            if (indexData.IndicesMumIndex == -1)
            {
                return;
            }
            var strArr = line.Split(k_CSVSpliter);
            var indicesIndex = CSVIndexData.GetDataInt(indexData.VertexNumIndex, strArr, -1);
            if (indicesIndex == -1)
            {
                return;
            }
            indicesIndex += subMeshDesc.indexStart;
            if (indicesIndex >= vertexDataList.GetIndexCount())
            {
                return;
            }
            var vertexIndex = CSVIndexData.GetDataInt(indexData.IndicesMumIndex, strArr, -1);
            if (vertexIndex == -1)
            {
                return;
            }
            vertexIndex = vertexIndex - vertexDataList.GetVertexIndexBase();
            if (vertexIndex >= vertexDataList.GetVertexCount())
            {
                return;
            }

            if (!vertexDataList.HasSetVertexData(vertexIndex))
            {
                if (indexData.Pos.IsValidAllComponent())
                {
                    var data = CSVIndexData.GetDataVertor3(in indexData.Pos, strArr, Vector3.zero);
                    vertexDataList.SetVertexData<Vector3>(vertexIndex, VertexAttribute.Position, data);
                }

                if (indexData.Normal.IsValidAllComponent())
                {
                    var data = CSVIndexData.GetDataVertor3(in indexData.Normal, strArr, Vector3.zero);
                    vertexDataList.SetVertexData<Vector3>(vertexIndex, VertexAttribute.Normal, data);
                }

                if (indexData.Tangent.IsValidAllComponent())
                {
                    var data = CSVIndexData.GetDataVertor4(in indexData.Tangent, strArr, Vector4.zero);
                    vertexDataList.SetVertexData<Vector4>(vertexIndex, VertexAttribute.Tangent, data);
                }

                if (indexData.Color.IsValidAllComponent())
                {
                    var data = CSVIndexData.GetDataVertor4(in indexData.Color, strArr, Vector4.zero);
                    vertexDataList.SetVertexData<Vector4>(vertexIndex, VertexAttribute.Color, data);
                }

                if (indexData.BlendWeight.IsValidAllComponent())
                {
                    var data = CSVIndexData.GetDataVertor4(in indexData.BlendWeight, strArr, Vector4.zero);
                    vertexDataList.SetVertexData<Vector4>(vertexIndex, VertexAttribute.BlendWeight, data);
                }

                if (indexData.BlendIndices.IsValidAllComponent())
                {
                    var data = CSVIndexData.GetDataVertor4Int(in indexData.BlendIndices, strArr, Vector4Int.zero);
                    vertexDataList.SetVertexData<Vector4Int>(vertexIndex, VertexAttribute.BlendIndices, data);
                }

                for (int i = 0; i < indexData.Texcoord.Length; ++i)
                {
                    var len = indexData.Texcoord[i].GetValidComponentLength();
                    if (len > 0)
                    {
                        switch (len)
                        {
                            case 2:
                                {
                                    var data = CSVIndexData.GetDataVertor2(indexData.Texcoord[i], strArr, Vector2.zero);
                                    vertexDataList.SetVertexData<Vector2>(vertexIndex, CSVIndexData.GetTexcoordAttribute(i), data);
                                }
                                break;
                            case 3:
                                {
                                    var data = CSVIndexData.GetDataVertor3(indexData.Texcoord[i], strArr, Vector3.zero);
                                    vertexDataList.SetVertexData<Vector3>(vertexIndex, CSVIndexData.GetTexcoordAttribute(i), data);
                                }
                                break;
                            case 4:
                                {
                                    var data = CSVIndexData.GetDataVertor4(indexData.Texcoord[i], strArr, Vector4.zero);
                                    vertexDataList.SetVertexData<Vector4>(vertexIndex, CSVIndexData.GetTexcoordAttribute(i), data);
                                }
                                break;
                        }
                    }
                }
                vertexDataList.SetCompleteVertexData(vertexIndex);
            }

            vertexDataList.SetVertexIndexData(indicesIndex, vertexIndex);
        }

        private static bool GetCSVIndexDataAndMeshInfo(System.IO.StreamReader[] strReader, VertexAttributeMapping[] mapping, ref CSVIndexData csvIndexData, ref CSVMeshInfo csvMeshInfo)
        {
            string line = string.Empty;
            for (int i = 0; i < strReader.Length; ++i)
            {
                var readLine = strReader[i].ReadLine();
                if (string.IsNullOrEmpty(readLine))
                {
                    return false;
                }
                if (string.IsNullOrEmpty(line))
                {
                    line = readLine;
                }
                else
                {
                    if (!string.Equals(readLine, line, System.StringComparison.Ordinal))
                    {
                        return false;
                    }
                }
            }
            GetCSVIndexData(line, mapping, ref csvIndexData);
            if (!csvIndexData.IsValid())
            {
                return false;
            }

            NativeArray<CSVSubMeshInfo> subMeshInfoArray = new NativeArray<CSVSubMeshInfo>(strReader.Length, Allocator.Temp);
            for (int i = 0; i < strReader.Length; ++i)
            {
                CSVSubMeshInfo subMeshInfo = new CSVSubMeshInfo();
                GetCSVSubMeshInfo(strReader[i], csvIndexData, ref subMeshInfo);
                subMeshInfoArray[i] = subMeshInfo;
            }
            csvMeshInfo.PushSubMesh(subMeshInfoArray);
            subMeshInfoArray.Dispose();
            return true;
        }

        private static void GetCSVSubMeshInfo(System.IO.StreamReader strReader, in CSVIndexData indexData, ref CSVSubMeshInfo csvSubMeshInfo)
        {
            int minTriangleIndex = int.MaxValue;
            int maxTriangleIndex = -1;
            int minVertexIndex = int.MaxValue;
            int maxVertexIndex = -1;
            var vertexNumIndex = indexData.VertexNumIndex;
            var indicesNumIndex = indexData.IndicesMumIndex;

            var line = strReader.ReadLine();
            while (!string.IsNullOrEmpty(line))
            {
                var curStr = CSV.GetCSVString(line, indicesNumIndex);
                if (!string.IsNullOrEmpty(curStr))
                {
                    int val;
                    if (int.TryParse(curStr.Trim(), out val))
                    {
                        minTriangleIndex = Mathf.Min(val, minTriangleIndex);
                        maxTriangleIndex = Mathf.Max(val, maxTriangleIndex);
                    }
                }
                curStr = CSV.GetCSVString(line, vertexNumIndex);
                if (!string.IsNullOrEmpty(curStr))
                {
                    int val;
                    if (int.TryParse(curStr.Trim(), out val))
                    {
                        minVertexIndex = Mathf.Min(val, minVertexIndex);
                        maxVertexIndex = Mathf.Max(val, maxVertexIndex);
                    }
                }
                line = strReader.ReadLine();
            }

            strReader.DiscardBufferedData();
            strReader.BaseStream.Seek(0, System.IO.SeekOrigin.Begin);
            // skip header
            strReader.ReadLine();
            csvSubMeshInfo.MinVertexIndex = minTriangleIndex;
            csvSubMeshInfo.MaxVertexIndex = maxTriangleIndex;
            csvSubMeshInfo.IndexCount = maxVertexIndex - minVertexIndex + 1;
        }

        private static void GetCSVIndexData(string csvLine, VertexAttributeMapping[] mapping, ref CSVIndexData data)
        {
            Dictionary<string, VertexAttributeMapping> mappingDict = null;
            if (mapping != null)
            {
                mappingDict = new Dictionary<string, VertexAttributeMapping>(mapping.Length);
                for (int i = 0; i < mapping.Length; ++i)
                {
                    mappingDict[mapping[i].Name] = mapping[i];
                }
            }

            var strArr = csvLine.Split(k_CSVSpliter);
            for (int i = 0; i < strArr.Length; ++i)
            {
                var curAttr = VertexAttribute.Position;
                bool findAttr = false;
                var cur = strArr[i].Trim();
                string compString = string.Empty;
                var headerString = RenderDocCSV.ParseVertexAttrName(cur, out compString);

                if (RenderDocCSV.ParseTriangleIndices(headerString))
                {
                    data.IndicesMumIndex = i;
                    continue;
                }

                if (RenderDocCSV.ParseVertexID(headerString))
                {
                    data.VertexNumIndex = i;
                    continue;
                }


                if (mappingDict != null)
                {
                    VertexAttributeMapping vertexMapping;
                    if (mappingDict.TryGetValue(headerString, out vertexMapping))
                    {
                        if (vertexMapping.Disable)
                        {
                            // Ignore
                            continue;
                        }
                        curAttr = vertexMapping.Attr;
                        findAttr = true;
                    }
                }
                if (!findAttr)
                {
                    // fallback
                    findAttr = RenderDocCSV.SpeculateVertextAttr(headerString, out curAttr);
                }
                if (!findAttr)
                {
                    UnityEngine.Debug.LogError($"Can not match Vertex Attribute: {cur}");
                    continue;
                }

                var compIndex = RenderDocCSV.ParseComponentIndex(compString);
                switch (curAttr)
                {
                    case VertexAttribute.Position:
                        data.SetPos(compIndex, i);
                        break;
                    case VertexAttribute.Normal:
                        data.SetNormal(compIndex, i);
                        break;
                    case VertexAttribute.Tangent:
                        data.SetTangent(compIndex, i);
                        break;
                    case VertexAttribute.Color:
                        data.SetColor(compIndex, i);
                        break;
                    case VertexAttribute.TexCoord0:
                    case VertexAttribute.TexCoord1:
                    case VertexAttribute.TexCoord2:
                    case VertexAttribute.TexCoord3:
                    case VertexAttribute.TexCoord4:
                    case VertexAttribute.TexCoord5:
                    case VertexAttribute.TexCoord6:
                    case VertexAttribute.TexCoord7:
                        data.SetTexcoord(curAttr - VertexAttribute.TexCoord0, compIndex, i);
                        break;
                    case VertexAttribute.BlendWeight:
                        data.SetBlendWeight(compIndex, i);
                        break;
                    case VertexAttribute.BlendIndices:
                        data.SetBlendIndices(compIndex, i);
                        break;
                    default:
                        UnityEngine.Debug.LogError($"Not support: {curAttr}");
                        break;
                }
            }

        }

    }
}