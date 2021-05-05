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
        private const string k_CSVHead_Position = "POSITION";
        private const string k_CSVHead_Normal = "NORMAL";
        private const string k_CSVHead_Tangent = "TANGENT";
        private const string k_CSVHead_Color = "COLOR";
        private const string k_CSVHead_Texcoord = "TEXCOORD";
        private const string k_CSVHead_BlendIndices = "BLENDINDICES";
        private const string k_CSVHead_BlendWeight = "BLENDWEIGHT";
        private const string k_CSVHead_idx = "IDX";
        private const string k_CSVHead_vtx = "VTX";

        private const char k_CSVCompSpliter = '.';
        private const string k_CSVComp_X = "x";
        private const string k_CSVComp_Y = "y";
        private const string k_CSVComp_Z = "z";
        private const string k_CSVComp_W = "w";

        [System.Flags]
        public enum Flags
        {
            None = 0x0,
            AutoCalcNormalIfNotExist = 0x1,
            AutoCalcTangentIfNotExist = 0x2,
            FlipUV_Y = 0x4,
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
        struct CSVIndexData
        {
            public int VertexIndex;
            public int IndicesIndex;
            public Vec3Index Pos;
            public Vec3Index Normal;
            public Vec4Index Tangent;
            public Vec4Index Color;
            public NativeArray<Vec4Index> Texcoord;

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
                                if(float.IsNaN(val) || float.IsInfinity(val))
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

            #endregion

            public CSVIndexData(Allocator allocator)
            {
                VertexIndex = -1;
                IndicesIndex = -1;
                Pos = new Vec3Index();
                Normal = new Vec3Index();
                Tangent = new Vec4Index();
                Color = new Vec4Index();
                Texcoord = new NativeArray<Vec4Index>(k_MaxTexcoord, allocator);
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
                return array;
            }

            public bool IsValid()
            {
                return (VertexIndex != -1) && (IndicesIndex != -1) && Pos.IsValidAllComponent();
            }

            public void Reset()
            {
                VertexIndex = -1;
                IndicesIndex = -1;
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

        struct VertexDataList : System.IDisposable
        {
            private bool dispose;
            private Mesh.MeshDataArray meshDataArray;
            private Mesh.MeshData meshData;
            private int indexCount;
            private int vertexIndexBase;
            private NativeArray<byte> vertexDataSet;
            private int vertexDataSetCount;
            private int streamMappingBase;
            private NativeArray<int> streamMapping;
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
                {
                    int minIndex = 0;
                    int maxIndex = 0;
                    var enumArray = System.Enum.GetValues(typeof(VertexAttribute)) as int[];
                    for (int i = 0; i < enumArray.Length; ++i)
                    {
                        minIndex = Mathf.Min(enumArray[i], minIndex);
                        maxIndex = Mathf.Max(enumArray[i], maxIndex);
                    }
                    streamMapping = new NativeArray<int>(maxIndex - minIndex + 1, allocator);
                    for (int i = 0; i < streamMapping.Length; ++i)
                    {
                        streamMapping[i] = -1;
                    }
                    int byteOffset = 0;
                    for (int i = 0; i < vertexDesc.Length; ++i)
                    {
                        var byteSize = GetByteSize(vertexDesc[i].format, vertexDesc[i].dimension);
                        var attrValue = (int)vertexDesc[i].attribute;
                        streamMapping[attrValue - minIndex] = byteOffset;
                        byteOffset += byteSize;
                    }
                    streamMappingBase = minIndex;
                    oneVertexDataTotalSize = byteOffset;
                }

                UnityEngine.Assertions.Assert.IsTrue(meshData.GetVertexData<byte>(0).Length == oneVertexDataTotalSize * vertexCount);
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

            public void SetVertexData<T>(int index, VertexAttribute attr, T data) where T : struct
            {
                var byteOffset = streamMapping[((int)attr) - streamMappingBase];
                if (byteOffset != -1)
                {
                    var byteArray = meshData.GetVertexData<byte>(0);
                    byteArray.ReinterpretStore<T>((index * oneVertexDataTotalSize) + byteOffset, data);
                }
            }



            public void SetVertexIndexData<T>(int index, T vertexIndex) where T : struct
            {
                var array = meshData.GetIndexData<T>();
                array[index] = vertexIndex;
            }

            public bool HasSetData(int index)
            {
                return (vertexDataSet[index] != 0);
            }

            public int FindFirstNotSetData()
            {
                for(int i = 0; i < vertexDataSet.Length; ++i)
                {
                    if(vertexDataSet[i] == 0)
                    {
                        return i;
                    }
                }
                return -1;
            }

            public void SetComplete(int index)
            {
                if (vertexDataSet[index] == 0)
                {
                    ++vertexDataSetCount;
                    vertexDataSet[index] = 1;
                }
            }

            public void ApplyToMesh(Mesh mesh)
            {
                meshData.subMeshCount = 1;
                meshData.SetSubMesh(0, new SubMeshDescriptor(0, indexCount, MeshTopology.Triangles));
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
                streamMapping.Dispose();
            }

            private static int GetByteSize(VertexAttributeFormat format)
            {
                switch (format)
                {
                    case VertexAttributeFormat.Float32: 
                    case VertexAttributeFormat.UInt32:
                    case VertexAttributeFormat.SInt32:
                        return 4;
                    case VertexAttributeFormat.Float16: 
                    case VertexAttributeFormat.UNorm16:
                    case VertexAttributeFormat.SNorm16:
                    case VertexAttributeFormat.UInt16:
                    case VertexAttributeFormat.SInt16:
                        return 2;
                    case VertexAttributeFormat.UNorm8:
                    case VertexAttributeFormat.SNorm8:
                    case VertexAttributeFormat.UInt8:
                    case VertexAttributeFormat.SInt8:
                        return 1;
                }
                throw new System.IndexOutOfRangeException();
            }

            private static int GetByteSize(VertexAttributeFormat format, int dimension)
            {
                return GetByteSize(format) * dimension;
            }
        }



        public static void GenerateMesh(string sourcePath, string targetAssetPath, Flags flags, Allocator allocator)
        {
            if (!System.IO.File.Exists(sourcePath))
            {
                return;
            }

            var mesh = new Mesh();
            mesh.name = string.Empty;

            using (System.IO.StreamReader strReader = new System.IO.StreamReader(sourcePath))
            {
                CSVIndexData indexData = new CSVIndexData(allocator);
                indexData.Reset();

                var line = strReader.ReadLine();
                GetCSVIndexData(line, ref indexData);
                if (!indexData.IsValid())
                {
                    UnityEngine.Debug.LogError("Can not read csv properly.");
                    return;
                }

                int vertexCount;
                int indexCount;
                int vertexIndexBase;
                GetVertexCountFromCSV(strReader, in indexData, out vertexCount, out indexCount, out vertexIndexBase);
                if (indexCount < 3)
                {
                    UnityEngine.Debug.LogError("Read vertexCount not properly.");
                    return;
                }

                var vertexAttrArray = indexData.GetVertexAttributes(allocator);
                var vertexDataList = new VertexDataList(vertexAttrArray, vertexCount, indexCount, vertexIndexBase, allocator);
                try
                {
                    line = strReader.ReadLine();
                    while (!string.IsNullOrEmpty(line))
                    {
                        SetVertexData(line, in indexData, flags, ref vertexDataList);
                        line = strReader.ReadLine();
                    }
                    if (!vertexDataList.IsSetVertexDataFinish())
                    {
                        UnityEngine.Debug.LogError($"not finish set Vertex Data. VertexCount:{vertexDataList.GetVertexCount()} SetCount:{vertexDataList.HasSetVertexDataCount()} FirstNotSet:{vertexDataList.FindFirstNotSetData()}");
                        return;
                    }
                    vertexDataList.ApplyToMesh(mesh);
                    if ((flags & Flags.AutoCalcNormalIfNotExist) != 0)
                    {
                        if (!indexData.Normal.IsValidAllComponent())
                        {
                            mesh.RecalculateNormals();
                        }
                    }
                    if ((flags & Flags.AutoCalcTangentIfNotExist) != 0)
                    {
                        if (!indexData.Tangent.IsValidAllComponent())
                        {
                            mesh.RecalculateTangents();
                        }
                    }
                }
                finally
                {
                    vertexAttrArray.Dispose();
                    vertexDataList.Dispose();
                }
            }

            AssetDatabase.CreateAsset(mesh, targetAssetPath);
            AssetDatabase.ImportAsset(targetAssetPath);
            Selection.activeObject = mesh;
            EditorGUIUtility.PingObject(mesh);
        }

        private static void SetVertexData(string line, in CSVIndexData indexData, Flags flags, ref VertexDataList vertexDataList)
        {
            if (indexData.VertexIndex == -1)
            {
                return;
            }
            if (indexData.IndicesIndex == -1)
            {
                return;
            }
            var strArr = line.Split(k_CSVSpliter);
            var indicesIndex = CSVIndexData.GetDataInt(indexData.VertexIndex, strArr, -1);
            if (indicesIndex == -1)
            {
                return;
            }
            if(indicesIndex >= vertexDataList.GetIndexCount())
            {
                return;
            }
            var vertexIndex = CSVIndexData.GetDataInt(indexData.IndicesIndex, strArr, -1);
            if (vertexIndex == -1)
            {
                return;
            }
            vertexIndex = vertexIndex - vertexDataList.GetVertexIndexBase();
            if (vertexIndex >= vertexDataList.GetVertexCount())
            {
                return;
            }

            if (!vertexDataList.HasSetData(vertexIndex))
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
                                    if ((flags & Flags.FlipUV_Y) != 0)
                                    {
                                        data.y = -data.y;
                                    }
                                    vertexDataList.SetVertexData<Vector2>(vertexIndex, CSVIndexData.GetTexcoordAttribute(i), data);
                                }
                                break;
                            case 3:
                                {
                                    var data = CSVIndexData.GetDataVertor3(indexData.Texcoord[i], strArr, Vector3.zero);
                                    if ((flags & Flags.FlipUV_Y) != 0)
                                    {
                                        data.y = -data.y;
                                    }
                                    vertexDataList.SetVertexData<Vector3>(vertexIndex, CSVIndexData.GetTexcoordAttribute(i), data);
                                }
                                break;
                            case 4:
                                {
                                    var data = CSVIndexData.GetDataVertor4(indexData.Texcoord[i], strArr, Vector4.zero);
                                    if ((flags & Flags.FlipUV_Y) != 0)
                                    {
                                        data.y = -data.y;
                                    }
                                    vertexDataList.SetVertexData<Vector4>(vertexIndex, CSVIndexData.GetTexcoordAttribute(i), data);
                                }
                                break;
                        }
                    }
                }
                vertexDataList.SetComplete(vertexIndex);
            }

            if (vertexDataList.GetIndexFormat() == IndexFormat.UInt16)
            {
                vertexDataList.SetVertexIndexData(indicesIndex, System.Convert.ToUInt16(vertexIndex));
            }
            else
            {
                vertexDataList.SetVertexIndexData(indicesIndex, System.Convert.ToUInt32(vertexIndex));
            }
        }

        private static void GetVertexCountFromCSV(System.IO.StreamReader strReader, in CSVIndexData indexData, out int vertexCount, out int indexCount, out int vertexIndexBase)
        {
            int minVertexIndex = int.MaxValue;
            int maxVertexIndex = -1;
            int maxIndicesIndex = -1;
            var vertexIndex = indexData.VertexIndex;
            var indicesIndex = indexData.IndicesIndex;

            var line = strReader.ReadLine();
            while (!string.IsNullOrEmpty(line))
            {
                var curStr = GetCSVString(line, indicesIndex);
                if (!string.IsNullOrEmpty(curStr))
                {
                    int val;
                    if (int.TryParse(curStr.Trim(), out val))
                    {
                        minVertexIndex = Mathf.Min(val, minVertexIndex);
                        maxVertexIndex = Mathf.Max(val, maxVertexIndex);
                    }
                }
                curStr = GetCSVString(line, vertexIndex);
                if (!string.IsNullOrEmpty(curStr))
                {
                    int val;
                    if (int.TryParse(curStr.Trim(), out val))
                    {
                        maxIndicesIndex = Mathf.Max(val, maxIndicesIndex);
                    }
                }
                line = strReader.ReadLine();
            }

            strReader.DiscardBufferedData();
            strReader.BaseStream.Seek(0, System.IO.SeekOrigin.Begin);
            strReader.ReadLine();
            vertexIndexBase = minVertexIndex;
            vertexCount = maxVertexIndex - minVertexIndex + 1;
            indexCount = maxIndicesIndex + 1;
        }

        private static string GetCSVString(string line, int index)
        {
            int beginCharIndex = 0;
            int lastCharIndex = -2;
            while (index >= 0)
            {
                beginCharIndex = lastCharIndex + 2;
                var find = line.IndexOf(k_CSVSpliter, beginCharIndex);
                if (find == -1)
                {
                    if (index == 0)
                    {
                        // 最后一个是没有分隔
                        lastCharIndex = line.Length - 1;
                        break;
                    }
                    return string.Empty;
                }
                lastCharIndex = find - 1;
                --index;
            }
            if (lastCharIndex >= beginCharIndex)
            {
                return line.Substring(beginCharIndex, lastCharIndex - beginCharIndex + 1);
            }
            return string.Empty;
        }

        private static void GetCSVIndexData(string csvLine, ref CSVIndexData data)
        {
            var strArr = csvLine.Split(k_CSVSpliter);
            for (int i = 0; i < strArr.Length; ++i)
            {
                var cur = strArr[i].Trim();
                if (cur.StartsWith(k_CSVHead_Position, System.StringComparison.Ordinal))
                {
                    data.SetPos(ParseComponentIndex(cur, k_CSVHead_Position.Length), i);
                }
                else if (cur.StartsWith(k_CSVHead_Normal, System.StringComparison.Ordinal))
                {
                    data.SetNormal(ParseComponentIndex(cur, k_CSVHead_Normal.Length), i);
                }
                else if (cur.StartsWith(k_CSVHead_Tangent, System.StringComparison.Ordinal))
                {
                    data.SetTangent(ParseComponentIndex(cur, k_CSVHead_Tangent.Length), i);
                }
                else if (cur.StartsWith(k_CSVHead_Color, System.StringComparison.Ordinal))
                {
                    data.SetColor(ParseComponentIndex(cur, k_CSVHead_Color.Length), i);
                }
                else if (cur.StartsWith(k_CSVHead_Texcoord, System.StringComparison.Ordinal))
                {
                    int len;
                    var texcoordIndex = ParseTexcoordIndex(cur, k_CSVHead_Texcoord.Length, out len);
                    if (texcoordIndex != -1)
                    {
                        data.SetTexcoord(texcoordIndex, ParseComponentIndex(cur, k_CSVHead_Texcoord.Length + len), i);
                    }
                }
                else if (cur.Equals(k_CSVHead_idx, System.StringComparison.Ordinal))
                {
                    data.IndicesIndex = i;
                }
                else if (cur.Equals(k_CSVHead_vtx, System.StringComparison.Ordinal))
                {
                    data.VertexIndex = i;
                }
                else if (cur.StartsWith(k_CSVHead_BlendIndices, System.StringComparison.Ordinal))
                {
                    // not thing
                }
                else if (cur.StartsWith(k_CSVHead_BlendWeight, System.StringComparison.Ordinal))
                {
                    // not thing
                }
                else
                {
                    UnityEngine.Debug.LogError($"Not support: {cur}");
                }
            }

        }

        private static int ParseTexcoordIndex(string str, int begin, out int len)
        {
            int end = begin;
            while ((end < str.Length) && char.IsDigit(str[end]))
            {
                ++end;
            }
            if (end > begin)
            {
                int outIndex;
                if (int.TryParse(str.Substring(begin, end - begin), out outIndex))
                {
                    len = end - begin;
                    return outIndex;
                }
            }
            len = 0;
            return -1;
        }

        private static int ParseComponentIndex(string str, int begin)
        {
            if (begin >= 0 && begin < str.Length)
            {
                if (str[begin] == k_CSVCompSpliter)
                {
                    var compStr = str.Substring(begin + 1);
                    if (compStr.Equals(k_CSVComp_X, System.StringComparison.Ordinal))
                    {
                        return 0;
                    }
                    else if (compStr.Equals(k_CSVComp_Y, System.StringComparison.Ordinal))
                    {
                        return 1;
                    }
                    else if (compStr.Equals(k_CSVComp_Z, System.StringComparison.Ordinal))
                    {
                        return 2;
                    }
                    else if (compStr.Equals(k_CSVComp_W, System.StringComparison.Ordinal))
                    {
                        return 3;
                    }
                }
            }
            return -1;
        }
    }
}