using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RenderDocPlugins
{
    public class RenderDocCSV
    {
        private const char k_CSVSpliter = ',';
        private const char k_CSVCompSpliter = '.';

        private const string k_CSVComp_X = "x";
        private const string k_CSVComp_Y = "y";
        private const string k_CSVComp_Z = "z";
        private const string k_CSVComp_W = "w";

        public static class Mesh
        {
            public const string k_Head_idx = "IDX";
            public const string k_Head_vtx = "VTX";

            public const string k_Head_Position = "POSITION";
            public const string k_Head_Normal = "NORMAL";
            public const string k_Head_Tangent = "TANGENT";
            public const string k_Head_Color = "COLOR";
            public const string k_Head_Texcoord = "TEXCOORD";
            public const string k_Head_BlendIndices = "BLENDINDICES";
            public const string k_Head_BlendWeight = "BLENDWEIGHT";
        }

        public static bool CheckAllMeshCSVHeader(string[] sourcePath)
        {
            if(sourcePath.Length <= 1)
            {
                return true;
            }
            System.IO.StreamReader[] strReaderArray = new System.IO.StreamReader[sourcePath.Length];
            try
            {
                for (int i = 0; i < sourcePath.Length; ++i)
                {
                    if(string.IsNullOrEmpty(sourcePath[i]))
                    {
                        continue;
                    }
                    strReaderArray[i] = new System.IO.StreamReader(sourcePath[i]);
                }
                string line = string.Empty;
                for (int i = 0; i < strReaderArray.Length; ++i)
                {
                    if(strReaderArray[i] == null)
                    {
                        continue;
                    }
                    var readLine = strReaderArray[i].ReadLine();
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
            return true;
        }

        public static void GetMeshCSVVertexAttrList(string csvPath, List<string> outList, List<int> compCountList)
        {
            using (var strReader = new System.IO.StreamReader(csvPath))
            {
                var readLine = strReader.ReadLine();
                if (string.IsNullOrEmpty(readLine))
                {
                    return ;
                }

                var strArr = readLine.Split(k_CSVSpliter);
                for (int i = 0; i < strArr.Length; ++i)
                {
                    var cur = strArr[i].Trim();
                    if (cur.Equals(Mesh.k_Head_idx, System.StringComparison.Ordinal))
                    {
                        continue;
                    }
                    else if (cur.Equals(Mesh.k_Head_vtx, System.StringComparison.Ordinal))
                    {
                        continue;
                    }
                    else
                    {
                        var compIndex = cur.IndexOf(k_CSVCompSpliter);
                        if (compIndex != -1)
                        {
                            cur = cur.Substring(0, compIndex);
                        }
                        var index = outList.IndexOf(cur);
                        if (index == -1)
                        {
                            outList.Add(cur);
                            compCountList.Add(1);
                        }
                        else
                        {
                            compCountList[index] += 1;
                        }
                    }
                }
            }
        }


        public static bool SpeculateVertextAttr(string name, out UnityEngine.Rendering.VertexAttribute vertexAttribute)
        {
            if (name.StartsWith(Mesh.k_Head_Position, System.StringComparison.Ordinal))
            {
                vertexAttribute = UnityEngine.Rendering.VertexAttribute.Position;
                return true;
            }
            else if (name.StartsWith(Mesh.k_Head_Normal, System.StringComparison.Ordinal))
            {
                vertexAttribute = UnityEngine.Rendering.VertexAttribute.Normal;
                return true;
            }
            else if (name.StartsWith(Mesh.k_Head_Tangent, System.StringComparison.Ordinal))
            {
                vertexAttribute = UnityEngine.Rendering.VertexAttribute.Tangent;
                return true;
            }
            else if (name.StartsWith(Mesh.k_Head_Color, System.StringComparison.Ordinal))
            {
                vertexAttribute = UnityEngine.Rendering.VertexAttribute.Color;
                return true;
            }
            else if (name.StartsWith(Mesh.k_Head_Texcoord, System.StringComparison.Ordinal))
            {
                int len;
                var texcoordIndex = ParseHeaderIndex(name, Mesh.k_Head_Texcoord.Length, out len);
                if (texcoordIndex != -1)
                {
                    var count = (UnityEngine.Rendering.VertexAttribute.TexCoord7 - UnityEngine.Rendering.VertexAttribute.TexCoord0) + 1;
                    if(texcoordIndex >= 0 && texcoordIndex < count)
                    {
                        vertexAttribute = UnityEngine.Rendering.VertexAttribute.TexCoord0 + texcoordIndex;
                        return true;
                    }
                }
            }
            else if (name.StartsWith(Mesh.k_Head_BlendIndices, System.StringComparison.Ordinal))
            {
                vertexAttribute = UnityEngine.Rendering.VertexAttribute.BlendIndices;
                return true;
            }
            else if (name.StartsWith(Mesh.k_Head_BlendWeight, System.StringComparison.Ordinal))
            {
                vertexAttribute = UnityEngine.Rendering.VertexAttribute.BlendWeight;
                return true;
            }
            vertexAttribute = UnityEngine.Rendering.VertexAttribute.Position;
            return false;
        }

        private static int ParseHeaderIndex(string str, int begin, out int len)
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


        public static string ParseVertexAttrName(string cur, out string compString)
        {
            var compIndex = cur.IndexOf(k_CSVCompSpliter);
            if (compIndex != -1)
            {
                compString = cur.Substring(compIndex + 1);
                cur = cur.Substring(0, compIndex);
            }
            else
            {
                compString = string.Empty;
            }
            return cur;
        }

        public static int ParseComponentIndex(string compStr)
        {
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
            return -1;
        }

        public static bool ParseTriangleIndices(string cur)
        {
            return cur.Equals(Mesh.k_Head_idx, System.StringComparison.Ordinal);
        }

        public static bool ParseVertexID(string cur)
        {
            return cur.Equals(Mesh.k_Head_vtx, System.StringComparison.Ordinal);
        }
    }
}
