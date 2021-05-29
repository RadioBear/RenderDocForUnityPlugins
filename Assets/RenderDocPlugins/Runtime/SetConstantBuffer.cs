using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace RenderDocPlugins
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(Renderer))]
    public class SetConstantBuffer : MonoBehaviour
    {
        private static MaterialPropertyBlock s_MPB;

        public CSVMaterialConstantBuffer m_CSVBuffer;
        public string m_CBufferName;

        private Renderer m_Renderer;
        private GraphicsBuffer m_SetBuffer;
        private string m_SetBufferName;


        private void Update()
        {
            CheckBuffer();
        }

        private void OnDisable()
        {
            ClearProperty();
        }

        private void CheckBuffer()
        {
            if (m_CSVBuffer == null)
            {
                ClearProperty();
            }
            else
            {

                if ((m_CSVBuffer.Buffer != m_SetBuffer)  || (!string.Equals(m_CBufferName, m_SetBufferName, System.StringComparison.Ordinal)))
                {
                    SetProperty(m_CSVBuffer.Buffer, m_CBufferName);
                }
            }
        }

        private void ClearProperty()
        {
            if (m_SetBuffer != null && !string.IsNullOrEmpty(m_SetBufferName))
            {
                var renderer = GetRenderer();
                if (renderer != null)
                {
                    var mpb = GetMPB();
                    renderer.GetPropertyBlock(mpb);
                    mpb.SetConstantBuffer(m_SetBufferName, (GraphicsBuffer)null, 0, 0);
                    renderer.SetPropertyBlock(mpb);
                }

                m_SetBufferName = string.Empty;
                m_SetBuffer = null;
            }
        }

        private void SetProperty(GraphicsBuffer buffer, string name)
        {
            var renderer = GetRenderer();
            if (renderer != null)
            {
                var mpb = GetMPB();
                renderer.GetPropertyBlock(mpb);

                if (m_SetBuffer != null && !string.IsNullOrEmpty(m_SetBufferName))
                {
                    mpb.SetConstantBuffer(m_SetBufferName, (GraphicsBuffer)null, 0, 0);
                    m_SetBuffer = null;
                    m_SetBufferName = string.Empty;
                }
                if (buffer != null && !string.IsNullOrEmpty(name))
                {
                    mpb.SetConstantBuffer(name, buffer, 0, buffer.count * buffer.stride);
                    m_SetBuffer = buffer;
                    m_SetBufferName = name;
                }
                renderer.SetPropertyBlock(mpb);
            }
        }

        private Renderer GetRenderer()
        {
            if(m_Renderer == null)
            {
                TryGetComponent<Renderer>(out m_Renderer);
            }
            return m_Renderer;
        }

        private MaterialPropertyBlock GetMPB()
        {
            if(s_MPB == null)
            {
                s_MPB = new MaterialPropertyBlock();
            }
            return s_MPB;
        }
    }
}