using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEditor;
using System;
using System.Security.Policy;

namespace RenderDocPlugins
{

    [System.Serializable]
    public struct VertexAttributeMapping
    {
        public enum Manipulation
        {
            Nothing,
            Negation,
        }
        public const int k_ComponentCount = 4;
        public static readonly string[] k_ComponentName = new string[]
        {
            "X",
            "Y",
            "Z",
            "W",
        };
        public enum Swizzle
        {
            [InspectorName("X (R)")]
            X,
            [InspectorName("Y (G)")]
            Y,
            [InspectorName("Z (B)")]
            Z,
            [InspectorName("W (A)")]
            W,
        }
        [System.Serializable]
        public struct ModifyData
        {
            // whitch value get
            [SerializeField]
            public Swizzle Swizzle;
            [SerializeField]
            public Manipulation Manipulation;

            public ModifyData(Swizzle index)
            {
                Swizzle = index;
                Manipulation = Manipulation.Nothing;
            }

            public void Reset(Swizzle index)
            {
                Swizzle = index;
                Manipulation = Manipulation.Nothing;
            }

            private static float GetDataValue(in UnityEngine.Vector2 data, Swizzle index, float defaultVal)
            {
                return index switch
                {
                    Swizzle.X => data.x,
                    Swizzle.Y => data.y,
                    _ => defaultVal,
                };
            }

            private static float GetDataValue(in UnityEngine.Vector3 data, Swizzle index, float defaultVal)
            {
                return index switch
                {
                    Swizzle.X => data.x,
                    Swizzle.Y => data.y,
                    Swizzle.Z => data.z,
                    _ => defaultVal,
                };
            }

            private static float GetDataValue(in UnityEngine.Vector4 data, Swizzle index, float defaultVal)
            {
                return index switch
                {
                    Swizzle.X => data.x,
                    Swizzle.Y => data.y,
                    Swizzle.Z => data.z,
                    Swizzle.W => data.w,
                    _ => defaultVal,
                };
            }

            private static int GetDataValue(in Vector4Int data, Swizzle index, int defaultVal)
            {
                return index switch
                {
                    Swizzle.X => data.x,
                    Swizzle.Y => data.y,
                    Swizzle.Z => data.z,
                    Swizzle.W => data.w,
                    _ => defaultVal,
                };
            }

            private static float ApplyManipulation(Manipulation man, float val)
            {
                return man switch
                {
                    Manipulation.Negation => -val,
                    _ => val,
                };
            }

            private static int ApplyManipulation(Manipulation man, int val)
            {
                return man switch
                {
                    Manipulation.Negation => -val,
                    _ => val,
                };
            }

            public float ApplyData(float data)
            {
                return ApplyManipulation(Manipulation, data);
            }

            public float ApplyData(in UnityEngine.Vector2 data)
            {
                float val = GetDataValue(data, Swizzle, 0.0f);
                val = ApplyManipulation(Manipulation, val);
                return val;
            }

            public float ApplyData(in UnityEngine.Vector3 data)
            {
                float val = GetDataValue(data, Swizzle, 0.0f);
                val = ApplyManipulation(Manipulation, val);
                return val;
            }

            public float ApplyData(in UnityEngine.Vector4 data)
            {
                float val = GetDataValue(data, Swizzle, 0.0f);
                val = ApplyManipulation(Manipulation, val);
                return val;
            }

            public int ApplyData(in Vector4Int data)
            {
                int val = GetDataValue(data, Swizzle, 0);
                val = ApplyManipulation(Manipulation, val);
                return val;
            }
        }

        public struct ModifyDataPreset
        {
            public ModifyData ComponentX;
            public ModifyData ComponentY;
            public ModifyData ComponentZ;
            public ModifyData ComponentW;
        }

        public static readonly GUIContent[] k_ModifyDataPresetName = new GUIContent[]
        {
            new GUIContent("DirectX <=> OpenGL"),
        };
        public readonly static ModifyDataPreset[] k_ModifyDataPreset =
        {
            new ModifyDataPreset
            {
                ComponentX = 
                {
                    Swizzle = Swizzle.Z,
                    Manipulation = Manipulation.Negation,
                },
                ComponentY =
                {
                    Swizzle = Swizzle.X,
                    Manipulation = Manipulation.Negation,
                },
                ComponentZ =
                {
                    Swizzle = Swizzle.Y,
                    Manipulation = Manipulation.Nothing,
                },
                ComponentW =
                {
                    Swizzle = Swizzle.W,
                    Manipulation = Manipulation.Nothing,
                },
            },
        };

        [SerializeField]
        public string Name;
        [SerializeField]
        public VertexAttribute Attr;
        [SerializeField]
        public bool Enable;
        [SerializeField]
        public bool Modify;
        [SerializeField]
        public ModifyData ModifyComponentX;
        [SerializeField]
        public ModifyData ModifyComponentY;
        [SerializeField]
        public ModifyData ModifyComponentZ;
        [SerializeField]
        public ModifyData ModifyComponentW;

        public void Reset()
        {
            Name = String.Empty;
            Attr = VertexAttribute.Position;
            Enable = false;
            Modify = false;
            ModifyComponentX.Reset(Swizzle.X);
            ModifyComponentY.Reset(Swizzle.Y);
            ModifyComponentZ.Reset(Swizzle.Z);
            ModifyComponentW.Reset(Swizzle.W);
        }

        public void ResetModify()
        {
            ModifyComponentX.Swizzle = Swizzle.X;
            ModifyComponentY.Swizzle = Swizzle.Y;
            ModifyComponentZ.Swizzle = Swizzle.Z;
            ModifyComponentW.Swizzle = Swizzle.W;
        }

        public void ApplyModifyPreset(in ModifyDataPreset preset)
        {
            ModifyComponentX = preset.ComponentX;
            ModifyComponentY = preset.ComponentY;
            ModifyComponentZ = preset.ComponentZ;
            ModifyComponentW = preset.ComponentW;
        }

        public ModifyData GetModifyData(int index)
        {
            return index switch
            {
                0 => ModifyComponentX,
                1 => ModifyComponentY,
                2 => ModifyComponentZ,
                3 => ModifyComponentW,
                _ => throw new System.IndexOutOfRangeException(string.Format("Invalid index addressed: {0}!", index)),
            };
        }

        public static ref ModifyData GetModifyData(ref VertexAttributeMapping foo, int index)
        {
            switch (index)
            {
                case 0: return ref foo.ModifyComponentX;
                case 1: return ref foo.ModifyComponentY;
                case 2: return ref foo.ModifyComponentZ;
                case 3: return ref foo.ModifyComponentW;
            }
            throw new System.IndexOutOfRangeException(string.Format("Invalid index addressed: {0}!", index));
        }

    }

    public class CSVToMeshGenerator
    {

        private const int k_MaxTexcoord = 8;

        private const char k_CSVSpliter = ',';


        [System.Flags]
        public enum Flags
        {
            None = 0x0,
            OptimizesRendering = 0x1,
            ReadWriteEnable = 0x2,
            FlipVertexWindingOrder = 0x4,
        }

        public enum AutoCalcMode
        {
            [InspectorName("Don't Calculate")]
            NotCalc,
            [InspectorName("If Not Exist")]
            IfNotExist,
            [InspectorName("Force")]
            Force,
        }

        

        public struct GenSetting
        {
            public Flags flags;
            public AutoCalcMode calcNormal;
            public AutoCalcMode calcTangent;
            public ModelImporterMeshCompression compression;
            public VertexAttributeMapping[] vertexAttrMapping;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct Vec3Index
        {
            public int X;
            public int Y;
            public int Z;
            public bool Modify;
            public VertexAttributeMapping.ModifyData ModifyX;
            public VertexAttributeMapping.ModifyData ModifyY;
            public VertexAttributeMapping.ModifyData ModifyZ;

            public void Reset()
            {
                X = -1;
                Y = -1;
                Z = -1;
                Modify = false;
                ModifyX.Reset(VertexAttributeMapping.Swizzle.X);
                ModifyY.Reset(VertexAttributeMapping.Swizzle.Y);
                ModifyZ.Reset(VertexAttributeMapping.Swizzle.Z);
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
                    return index switch
                    {
                        0 => X,
                        1 => Y,
                        2 => Z,
                        _ => throw new System.IndexOutOfRangeException(string.Format("Invalid Vec3Index index addressed: {0}!", index)),
                    };
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

            public VertexAttributeMapping.ModifyData GetModifyData(int index)
            {
                return index switch
                {
                    0 => ModifyX,
                    1 => ModifyY,
                    2 => ModifyZ,
                    _ => throw new System.IndexOutOfRangeException(string.Format("Invalid Vec3Index index addressed: {0}!", index)),
                };
            }

            public void SetModifyData(int index, in VertexAttributeMapping.ModifyData data)
            {
                switch(index)
                {
                    case 0: ModifyX = data; break;
                    case 1: ModifyY = data; break;
                    case 2: ModifyZ = data; break;
                    default:
                        throw new System.IndexOutOfRangeException(string.Format("Invalid Vec3Index index addressed: {0}!", index));
                }
            }

            public void ApplyModifyData(ref Vector3 data)
            {
                if(Modify)
                {
                    var oldData = data;
                    data.x = ModifyX.ApplyData(oldData);
                    data.y = ModifyY.ApplyData(oldData);
                    data.z = ModifyZ.ApplyData(oldData);
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
            public bool Modify;
            public VertexAttributeMapping.ModifyData ModifyX;
            public VertexAttributeMapping.ModifyData ModifyY;
            public VertexAttributeMapping.ModifyData ModifyZ;
            public VertexAttributeMapping.ModifyData ModifyW;

            public void Reset()
            {
                X = -1;
                Y = -1;
                Z = -1;
                W = -1;
                Modify = false;
                ModifyX.Reset(VertexAttributeMapping.Swizzle.X);
                ModifyY.Reset(VertexAttributeMapping.Swizzle.Y);
                ModifyZ.Reset(VertexAttributeMapping.Swizzle.Z);
                ModifyW.Reset(VertexAttributeMapping.Swizzle.W);
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
                    return index switch
                    {
                        0 => X,
                        1 => Y,
                        2 => Z,
                        3 => W,
                        _ => throw new System.IndexOutOfRangeException(string.Format("Invalid Vec4Index index addressed: {0}!", index)),
                    };
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

            public VertexAttributeMapping.ModifyData GetModifyData(int index)
            {
                return index switch
                {
                    0 => ModifyX,
                    1 => ModifyY,
                    2 => ModifyZ,
                    3 => ModifyW,
                    _ => throw new System.IndexOutOfRangeException(string.Format("Invalid Vec3Index index addressed: {0}!", index)),
                };
            }

            public void SetModifyData(int index, in VertexAttributeMapping.ModifyData data)
            {
                switch (index)
                {
                    case 0: ModifyX = data; break;
                    case 1: ModifyY = data; break;
                    case 2: ModifyZ = data; break;
                    case 3: ModifyW = data; break;
                    default:
                        throw new System.IndexOutOfRangeException(string.Format("Invalid Vec3Index index addressed: {0}!", index));
                }
            }

            public void ApplyModifyData(ref float data)
            {
                if (Modify)
                {
                    data = ModifyX.ApplyData(data);
                }
            }

            public void ApplyModifyData(ref Vector2 data)
            {
                if (Modify)
                {
                    var oldData = data;
                    data.x = ModifyX.ApplyData(oldData);
                    data.y = ModifyY.ApplyData(oldData);
                }
            }

            public void ApplyModifyData(ref Vector3 data)
            {
                if (Modify)
                {
                    var oldData = data;
                    data.x = ModifyX.ApplyData(oldData);
                    data.y = ModifyY.ApplyData(oldData);
                    data.z = ModifyZ.ApplyData(oldData);
                }
            }

            public void ApplyModifyData(ref Vector4 data)
            {
                if (Modify)
                {
                    var oldData = data;
                    data.x = ModifyX.ApplyData(oldData);
                    data.y = ModifyY.ApplyData(oldData);
                    data.z = ModifyZ.ApplyData(oldData);
                    data.w = ModifyW.ApplyData(oldData);
                }
            }

            public void ApplyModifyData(ref Vector4Int data)
            {
                if (Modify)
                {
                    var oldData = data;
                    data.x = ModifyX.ApplyData(oldData);
                    data.y = ModifyY.ApplyData(oldData);
                    data.z = ModifyZ.ApplyData(oldData);
                    data.w = ModifyW.ApplyData(oldData);
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
                        if (int.TryParse(blockStr, out int val))
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
                        int interIndex = vec3Index[i];
                        var blockStr = str[interIndex].Trim();
                        if (!string.IsNullOrEmpty(blockStr))
                        {
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
                    for (int i = 0; i < 4; ++i)
                    {
                        int interIndex = vec4Index[i];
                        var blockStr = str[interIndex].Trim();
                        if (!string.IsNullOrEmpty(blockStr))
                        {
                            if (float.TryParse(blockStr, out float val))
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
            public static float GetDataFloat(in Vec4Index vec4Index, string[] str, float defaultVal)
            {
                if (vec4Index.GetValidComponentLength() >= 1)
                {
                    float data = defaultVal;
                    int interIndex = vec4Index[0];
                    var blockStr = str[interIndex].Trim();
                    if (!string.IsNullOrEmpty(blockStr))
                    {
                        if (float.TryParse(blockStr, out float val))
                        {
                            if (float.IsNaN(val) || float.IsInfinity(val))
                            {
                                Debug.LogError($"IsNaN Or IsInfinity {blockStr}");
                                val = 0.0f;
                            }
                            data = val;
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
                        int interIndex = vec4Index[i];
                        var blockStr = str[interIndex].Trim();
                        if (!string.IsNullOrEmpty(blockStr))
                        {
                            if (float.TryParse(blockStr, out float val))
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
                        int interIndex = vec4Index[i];
                        var blockStr = str[interIndex].Trim();
                        if (!string.IsNullOrEmpty(blockStr))
                        {
                            if (float.TryParse(blockStr, out float val))
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
                        int interIndex = vec4Index[i];
                        var blockStr = str[interIndex].Trim();
                        if (!string.IsNullOrEmpty(blockStr))
                        {
                            if (int.TryParse(blockStr, out int val))
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
                Pos = new();
                Normal = new();
                Tangent = new();
                Color = new();
                Texcoord = new(k_MaxTexcoord, allocator);
                BlendWeight = new();
                BlendIndices = new();

                Reset();
            }

            public void SetPos(int index, int csvPos, in VertexAttributeMapping data)
            {
                if (Pos.IsValidIndex(index))
                {
                    Pos[index] = csvPos;
                    Pos.Modify = data.Modify;
                    Pos.SetModifyData(index, data.GetModifyData(index));
                }
            }

            public void SetNormal(int index, int csvPos, in VertexAttributeMapping data)
            {
                if (Normal.IsValidIndex(index))
                {
                    Normal[index] = csvPos;
                    Normal.Modify = data.Modify;
                    Normal.SetModifyData(index, data.GetModifyData(index));
                }
            }

            public void SetTangent(int index, int csvPos, in VertexAttributeMapping data)
            {
                if (Tangent.IsValidIndex(index))
                {
                    Tangent[index] = csvPos;
                    Tangent.Modify = data.Modify;
                    Tangent.SetModifyData(index, data.GetModifyData(index));
                }
            }

            public void SetColor(int index, int csvPos, in VertexAttributeMapping data)
            {
                if (Color.IsValidIndex(index))
                {
                    Color[index] = csvPos;
                    Color.Modify = data.Modify;
                    Color.SetModifyData(index, data.GetModifyData(index));
                }
            }

            public void SetTexcoord(int texIndex, int index, int csvPos, in VertexAttributeMapping data)
            {
                if (texIndex >= 0 && texIndex < Texcoord.Length)
                {
                    if (Texcoord[texIndex].IsValidIndex(index))
                    {
                        var texcorrd = Texcoord[texIndex];
                        texcorrd[index] = csvPos;
                        texcorrd.Modify = data.Modify;
                        texcorrd.SetModifyData(index, data.GetModifyData(index));
                        Texcoord[texIndex] = texcorrd;
                    }
                }
            }

            public void SetBlendWeight(int index, int csvPos, in VertexAttributeMapping data)
            {
                if (BlendWeight.IsValidIndex(index))
                {
                    BlendWeight[index] = csvPos;
                    BlendWeight.Modify = data.Modify;
                    BlendWeight.SetModifyData(index, data.GetModifyData(index));
                }
            }

            public void SetBlendIndices(int index, int csvPos, in VertexAttributeMapping data)
            {
                if (BlendIndices.IsValidIndex(index))
                {
                    BlendIndices[index] = csvPos;
                    BlendIndices.Modify = data.Modify;
                    BlendIndices.SetModifyData(index, data.GetModifyData(index));
                }
            }

            public NativeArray<VertexAttributeDescriptor> GetVertexAttributes(Allocator allocator)
            {
                // For a mesh to be compatible with a SkinnedMeshRenderer, it must have multiple vertex streams: one for deformed data (positions, normals, tangents), one for static data (colors and texture coordinates), and one for skinning data (blend weights and blend indices).
                // Within each stream, attributes of a vertex are laid out one after another, in this order:
                // VertexAttribute.Position, VertexAttribute.Normal, VertexAttribute.Tangent, VertexAttribute.Color, VertexAttribute.TexCoord0, ..., VertexAttribute.TexCoord7, VertexAttribute.BlendWeight, VertexAttribute.BlendIndices.
                // Not all format and dimension combinations are valid. Specifically, the data size of a vertex attribute must be a multiple of 4 bytes.For example, a VertexAttributeFormat.Float16 format with dimension 3 is not valid.See Also: SystemInfo.SupportsVertexAttributeFormat.
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

                NativeArray<VertexAttributeDescriptor> array = new(count, allocator);
                int index = 0;
                int stream = 0;
                int beginIndex = index;
                if (Pos.IsValidAllComponent())
                {
                    array[index] = new VertexAttributeDescriptor(
                        VertexAttribute.Position,
                        VertexAttributeFormat.Float32,
                        3,
                        stream
                        );
                    ++index;
                }
                if (Normal.IsValidAllComponent())
                {
                    array[index] = new VertexAttributeDescriptor(
                        VertexAttribute.Normal,
                        VertexAttributeFormat.Float32,
                        3,
                        stream
                        );
                    ++index;
                }
                if (Tangent.IsValidAllComponent())
                {
                    array[index] = new VertexAttributeDescriptor(
                        VertexAttribute.Tangent,
                        VertexAttributeFormat.Float32,
                        4,
                        stream
                        );
                    ++index;
                }

                if (beginIndex != index)
                {
                    ++stream;
                }
                beginIndex = index;
                if (Color.IsValidAllComponent())
                {
                    array[index] = new VertexAttributeDescriptor(
                        VertexAttribute.Color,
                        VertexAttributeFormat.Float32,
                        4,
                        stream
                        );
                    ++index;
                }
                for (int i = 0; i < Texcoord.Length; ++i)
                {
                    var len = Texcoord[i].GetValidComponentLength();
                    if (len > 0)
                    {
                        array[index] = new VertexAttributeDescriptor(
                        GetTexcoordAttribute(i),
                        VertexAttributeFormat.Float32,
                        len,
                        stream
                        );
                        ++index;
                    }
                }

                if (beginIndex != index)
                {
                    ++stream;
                }
                if (BlendWeight.IsValidAllComponent())
                {
                    array[index] = new VertexAttributeDescriptor(
                        VertexAttribute.BlendWeight,
                        VertexAttributeFormat.Float32,
                        4,
                        stream
                        );
                    ++index;
                }
                if (BlendIndices.IsValidAllComponent())
                {
                    array[index] = new VertexAttributeDescriptor(
                        VertexAttribute.BlendIndices,
                        VertexAttributeFormat.SInt32,
                        4,
                        stream
                        );
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
                if (Texcoord.IsCreated)
                {
                    Texcoord.Dispose();
                }
            }

            public static VertexAttribute GetTexcoordAttribute(int index)
            {
                return index switch
                {
                    0 => VertexAttribute.TexCoord0,
                    1 => VertexAttribute.TexCoord1,
                    2 => VertexAttribute.TexCoord2,
                    3 => VertexAttribute.TexCoord3,
                    4 => VertexAttribute.TexCoord4,
                    5 => VertexAttribute.TexCoord5,
                    6 => VertexAttribute.TexCoord6,
                    7 => VertexAttribute.TexCoord7,
                    _ => throw new System.IndexOutOfRangeException(),
                };
            }

            public static int GetTexcoordIndex(VertexAttribute attr)
            {
                return attr switch
                {
                    VertexAttribute.TexCoord0 => 0,
                    VertexAttribute.TexCoord1 => 1,
                    VertexAttribute.TexCoord2 => 2,
                    VertexAttribute.TexCoord3 => 3,
                    VertexAttribute.TexCoord4 => 4,
                    VertexAttribute.TexCoord5 => 5,
                    VertexAttribute.TexCoord6 => 6,
                    VertexAttribute.TexCoord7 => 7,
                    _ => throw new System.IndexOutOfRangeException(),
                };
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
            private readonly int indexCount;
            private readonly int vertexIndexBase;
            private NativeArray<byte> vertexDataSet;
            private int vertexDataSetCount;
            private NativeArray<byte> indexDataSet;
            private int indexDataSetCount;
            //private readonly int offsetMappingBase;
            //private NativeArray<int>[] offsetMapping;
            //private readonly int oneVertexDataTotalSize;

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
                //{
                //    // calc min max VertexAttribute value
                //    int minIndex = 0;
                //    int maxIndex = 0;
                //    var enumArray = System.Enum.GetValues(typeof(VertexAttribute)) as int[];
                //    for (int i = 0; i < enumArray.Length; ++i)
                //    {
                //        minIndex = Mathf.Min(enumArray[i], minIndex);
                //        maxIndex = Mathf.Max(enumArray[i], maxIndex);
                //    }
                //    offsetMapping = new NativeArray<int>(maxIndex - minIndex + 1, allocator);
                //    for (int i = 0; i < offsetMapping.Length; ++i)
                //    {
                //        offsetMapping[i] = -1;
                //    }
                //    int byteOffset = 0;
                //    for (int i = 0; i < vertexDesc.Length; ++i)
                //    {
                //        var byteSize = Utils.GetVertexAttributeByteSize(vertexDesc[i]);
                //        var attrValue = (int)vertexDesc[i].attribute;
                //        offsetMapping[attrValue - minIndex] = byteOffset;
                //        byteOffset += byteSize;
                //    }
                //    this.offsetMappingBase = minIndex;
                //    this.oneVertexDataTotalSize = byteOffset;
                //}
                unsafe
                {
                    var byteArray = meshData.GetVertexData<byte>(0);
                    UnsafeUtility.MemClear(NativeArrayUnsafeUtility.GetUnsafePtr(byteArray), byteArray.Length);
                }

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
                var stream = meshData.GetVertexAttributeStream(attr);
                if (stream == -1)
                {
                    return;
                }
                var byteOffset = meshData.GetVertexAttributeOffset(attr);
                if (byteOffset == -1)
                {
                    return;
                }
                var stride = meshData.GetVertexBufferStride(stream);
                if (stride <= 0)
                {
                    return;
                }
                var byteArray = meshData.GetVertexData<byte>(stream);
                byteArray.ReinterpretStore<T>((index * stride) + byteOffset, data);
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
                var flags = MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontNotifyMeshUsers | MeshUpdateFlags.DontResetBoneBounds;
                meshData.subMeshCount = csvMeshInfo.SubMeshCount;
                for (int i = 0; i < csvMeshInfo.SubMeshCount; ++i)
                {
                    // flags do not set MeshUpdateFlags.DontRecalculateBounds, otherwise unity will crash
                    meshData.SetSubMesh(i, csvMeshInfo.SubMeshDescArray[i], flags);
                }
                flags &= MeshUpdateFlags.DontRecalculateBounds;
                Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, mesh, flags);
                mesh.RecalculateBounds(flags);
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
                //offsetMapping.Dispose();
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
                var mesh = new Mesh
                {
                    name = string.Empty
                };
                CSVIndexData csvIndexData = new();
                CSVMeshInfo csvMeshInfo = new();
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
                        // for csv every line set vertexDataList
                        for (int i = 0; i < strReaderArray.Length; ++i)
                        {
                            var subMeshDesc = csvMeshInfo.SubMeshDescArray[i];
                            var strReader = strReaderArray[i];
                            var line = strReader.ReadLine();
                            while (!string.IsNullOrEmpty(line))
                            {
                                SetVertexDataOneLine(line, in csvIndexData, in subMeshDesc, ref vertexDataList);
                                line = strReader.ReadLine();
                            }
                        }
                        if (!vertexDataList.IsSetVertexDataFinish())
                        {
                            UnityEngine.Debug.LogError($"not finish set Vertex Data. VertexCount:{vertexDataList.GetVertexCount()} SetCount:{vertexDataList.HasSetVertexDataCount()} FirstNotSet:{vertexDataList.FindFirstNotSetVertexData()}");
                            return;
                        }
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
                        {
                            bool calcNormal = false;
                            switch (genSetting.calcNormal)
                            {
                                case AutoCalcMode.IfNotExist:
                                    if (!csvIndexData.Normal.IsValidAllComponent())
                                    {
                                        calcNormal = true;
                                    }
                                    break;
                                case AutoCalcMode.Force:
                                    calcNormal = true;
                                    break;
                            }
                            if (calcNormal)
                            {
                                mesh.RecalculateNormals(MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontResetBoneBounds | MeshUpdateFlags.DontNotifyMeshUsers);
                            }
                        }
                        {
                            bool calcTangent = false;
                            switch (genSetting.calcTangent)
                            {
                                case AutoCalcMode.IfNotExist:
                                    if (!csvIndexData.Tangent.IsValidAllComponent())
                                    {
                                        calcTangent = true;
                                    }
                                    break;
                                case AutoCalcMode.Force:
                                    calcTangent = true;
                                    break;
                            }
                            if (calcTangent)
                            {
                                mesh.RecalculateTangents(MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontResetBoneBounds | MeshUpdateFlags.DontNotifyMeshUsers);
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
                    catch (System.Exception e)
                    {
                        UnityEngine.Debug.LogError(e);
                    }
                    finally
                    {
                        vertexAttrArray.Dispose();
                        vertexDataList.Dispose();
                    }
                    succeed = true;
                    FileUtility.MakeSureAssetDirectoryExist(targetAssetPath);
                    AssetDatabase.CreateAsset(mesh, targetAssetPath);
                    AssetDatabase.ImportAsset(targetAssetPath);
                    Selection.activeObject = mesh;
                    EditorGUIUtility.PingObject(mesh);
                }
                catch (System.Exception e)
                {
                    UnityEngine.Debug.LogError(e);
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

        private static void SetVertexDataOneLine(string line, in CSVIndexData indexData, in SubMeshDescriptor subMeshDesc, ref VertexDataList vertexDataList)
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
            vertexIndex -= vertexDataList.GetVertexIndexBase();
            if (vertexIndex >= vertexDataList.GetVertexCount())
            {
                return;
            }

            if (!vertexDataList.HasSetVertexData(vertexIndex))
            {
                if (indexData.Pos.IsValidAllComponent())
                {
                    var data = CSVIndexData.GetDataVertor3(in indexData.Pos, strArr, Vector3.zero);
                    indexData.Pos.ApplyModifyData(ref data);
                    vertexDataList.SetVertexData<Vector3>(vertexIndex, VertexAttribute.Position, data);
                }

                if (indexData.Normal.IsValidAllComponent())
                {
                    var data = CSVIndexData.GetDataVertor3(in indexData.Normal, strArr, Vector3.zero);
                    indexData.Normal.ApplyModifyData(ref data);
                    vertexDataList.SetVertexData<Vector3>(vertexIndex, VertexAttribute.Normal, data);
                }

                if (indexData.Tangent.IsValidAllComponent())
                {
                    var data = CSVIndexData.GetDataVertor4(in indexData.Tangent, strArr, Vector4.zero);
                    indexData.Tangent.ApplyModifyData(ref data);
                    vertexDataList.SetVertexData<Vector4>(vertexIndex, VertexAttribute.Tangent, data);
                }

                if (indexData.Color.IsValidAllComponent())
                {
                    var data = CSVIndexData.GetDataVertor4(in indexData.Color, strArr, Vector4.zero);
                    indexData.Color.ApplyModifyData(ref data);
                    vertexDataList.SetVertexData<Vector4>(vertexIndex, VertexAttribute.Color, data);
                }

                if (indexData.BlendWeight.IsValidAllComponent())
                {
                    var data = CSVIndexData.GetDataVertor4(in indexData.BlendWeight, strArr, Vector4.zero);
                    indexData.BlendWeight.ApplyModifyData(ref data);
                    vertexDataList.SetVertexData<Vector4>(vertexIndex, VertexAttribute.BlendWeight, data);
                }

                if (indexData.BlendIndices.IsValidAllComponent())
                {
                    var data = CSVIndexData.GetDataVertor4Int(in indexData.BlendIndices, strArr, Vector4Int.zero);
                    indexData.BlendIndices.ApplyModifyData(ref data);
                    vertexDataList.SetVertexData<Vector4Int>(vertexIndex, VertexAttribute.BlendIndices, data);
                }

                for (int i = 0; i < indexData.Texcoord.Length; ++i)
                {
                    var len = indexData.Texcoord[i].GetValidComponentLength();
                    if (len > 0)
                    {
                        switch (len)
                        {
                            case 1:
                                {
                                    var data = CSVIndexData.GetDataFloat(indexData.Texcoord[i], strArr, 0.0f);
                                    indexData.Texcoord[i].ApplyModifyData(ref data);
                                    vertexDataList.SetVertexData<float>(vertexIndex, CSVIndexData.GetTexcoordAttribute(i), data);
                                }
                                break;
                            case 2:
                                {
                                    var data = CSVIndexData.GetDataVertor2(indexData.Texcoord[i], strArr, Vector2.zero);
                                    indexData.Texcoord[i].ApplyModifyData(ref data);
                                    vertexDataList.SetVertexData<Vector2>(vertexIndex, CSVIndexData.GetTexcoordAttribute(i), data);
                                }
                                break;
                            case 3:
                                {
                                    var data = CSVIndexData.GetDataVertor3(indexData.Texcoord[i], strArr, Vector3.zero);
                                    indexData.Texcoord[i].ApplyModifyData(ref data);
                                    vertexDataList.SetVertexData<Vector3>(vertexIndex, CSVIndexData.GetTexcoordAttribute(i), data);
                                }
                                break;
                            case 4:
                                {
                                    var data = CSVIndexData.GetDataVertor4(indexData.Texcoord[i], strArr, Vector4.zero);
                                    indexData.Texcoord[i].ApplyModifyData(ref data);
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

            NativeArray<CSVSubMeshInfo> subMeshInfoArray = new(strReader.Length, Allocator.Temp);
            for (int i = 0; i < strReader.Length; ++i)
            {
                CSVSubMeshInfo subMeshInfo = new();
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
                    if (int.TryParse(curStr.Trim(), out int val))
                    {
                        minTriangleIndex = Mathf.Min(val, minTriangleIndex);
                        maxTriangleIndex = Mathf.Max(val, maxTriangleIndex);
                    }
                }
                curStr = CSV.GetCSVString(line, vertexNumIndex);
                if (!string.IsNullOrEmpty(curStr))
                {
                    if (int.TryParse(curStr.Trim(), out int val))
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
                var headerString = RenderDocCSV.ParseVertexAttrName(cur, out string compString);

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

                VertexAttributeMapping vertexMapping = new()
                {
                    Enable = false
                };
                if (mappingDict != null)
                {
                    if (mappingDict.TryGetValue(headerString, out vertexMapping))
                    {
                        if (!vertexMapping.Enable)
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
                        data.SetPos(compIndex, i, vertexMapping);
                        break;
                    case VertexAttribute.Normal:
                        data.SetNormal(compIndex, i, vertexMapping);
                        break;
                    case VertexAttribute.Tangent:
                        data.SetTangent(compIndex, i, vertexMapping);
                        break;
                    case VertexAttribute.Color:
                        data.SetColor(compIndex, i, vertexMapping);
                        break;
                    case VertexAttribute.TexCoord0:
                    case VertexAttribute.TexCoord1:
                    case VertexAttribute.TexCoord2:
                    case VertexAttribute.TexCoord3:
                    case VertexAttribute.TexCoord4:
                    case VertexAttribute.TexCoord5:
                    case VertexAttribute.TexCoord6:
                    case VertexAttribute.TexCoord7:
                        data.SetTexcoord(curAttr - VertexAttribute.TexCoord0, compIndex, i, vertexMapping);
                        break;
                    case VertexAttribute.BlendWeight:
                        data.SetBlendWeight(compIndex, i, vertexMapping);
                        break;
                    case VertexAttribute.BlendIndices:
                        data.SetBlendIndices(compIndex, i, vertexMapping);
                        break;
                    default:
                        UnityEngine.Debug.LogError($"Not support: {curAttr}");
                        break;
                }
            }

        }

    }
}