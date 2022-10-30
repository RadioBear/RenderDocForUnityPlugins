using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using System.Runtime.InteropServices;

namespace RenderDocPlugins
{
    public interface ICBufferC
    {

    }

    [ExecuteInEditMode]
    public class CSVMaterialConstantBuffer : MonoBehaviour
    {
        private const string k_CSVHead_Name = "Name";
        private const string k_CSVHead_Value = "Value";
        private const string k_CSVHead_Type = "Type";
        private const string k_CSVHead_ByteOffset = "Byte Offset";

        public TextAsset m_CSV;

        private TextAsset m_TargetCSV;
        private GraphicsBuffer m_Buffer;
        public GraphicsBuffer Buffer
        {
            get { return m_Buffer; }
        }


        private void OnEnable()
        {
            
        }

        private void OnDisable()
        {
            ReleaseBuffer();
            m_TargetCSV = null;
        }

        private void Update()
        {
            CheckBuffer();
        }

        private void CheckBuffer()
        {
            if(m_CSV != m_TargetCSV)
            {
                m_TargetCSV = m_CSV;
                CreateBuffer();
            }
        }

        private void CreateBuffer()
        {
            ReleaseBuffer();
            if(m_TargetCSV == null)
            {
                return;
            }
            var data = CreateDataFromCSV(m_TargetCSV.text, Allocator.Temp);
            if (data.Length > 0)
            {
                m_Buffer = new GraphicsBuffer(GraphicsBuffer.Target.Constant, data.Length * 4, 4);
                m_Buffer.SetData<Vector4>(data);
            }
            data.Dispose();
        }

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

        private NativeArray<Vector4> CreateDataFromCSV(string csvStr, Allocator allocator)
        {
            int dataCount = 0;
            CSVIndexData indexData = new CSVIndexData();

            if (!GetDataCountFromCSV(csvStr, ref indexData, out dataCount))
            {
                return new NativeArray<Vector4>();
            }
            if(dataCount <= 0)
            {
                return new NativeArray<Vector4>();
            }
            if(indexData.ValueIndex == -1)
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
                    if(!string.IsNullOrEmpty(valStr))
                    {
                        data[dataCount] = GetValueFromCSV(valStr);
                    }
                    ++dataCount;
                    line = strReader.ReadLine();
                }
            }
            return data;
        }

        private bool GetDataCountFromCSV(string csvStr,ref CSVIndexData indexData, out int outCount)
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
                    UnityEngine.Debug.LogError($"Not support: {cur}");
                }
            }

        }

        private Vector4 GetValueFromCSV(string strValue)
        {
            strValue = strValue.Trim();
            strValue = strValue.Trim('"');
            var valueStrArray = strValue.Split(CSV.k_Spliter);
            Vector4 data = Vector4.zero;
            for(int i = 0; i < 4; ++i)
            {
                if(i >= valueStrArray.Length)
                {
                    break;
                }
                var blockStr = valueStrArray[i];
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
            return data;
        }
    }
}