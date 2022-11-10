using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace RenderDocPlugins
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(Renderer))]
    public class SetConstantBuffer : MonoBehaviour
    {
        private static MaterialPropertyBlock s_MPB = null;
        private static readonly List<Material> s_MaterialList = new List<Material>();

        public CSVMaterialConstantBuffer m_CSVBuffer;
        public string m_CBufferName;
        public int m_CBufferMaterialIndex = -1;

        private Renderer m_Renderer = null;
        private GraphicsBuffer m_SetBuffer = null;
        private string m_SetBufferName = string.Empty;
        private int m_SetBufferMatIndex = -1;
        private Material m_SetMaterial = null;

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

        private class MaterialLsistScope : Scope
        {
            private readonly List<Material> m_TargetList;
            public MaterialLsistScope(List<Material> list)
            {
                m_TargetList = list;
            }

            protected override void CloseScope()
            {
                m_TargetList.Clear();
            }
        }
        private class MaterialPropertyBlockScope : Scope
        {
            private readonly MaterialPropertyBlock m_Target;
            public MaterialPropertyBlockScope(MaterialPropertyBlock target)
            {
                m_Target = target;
            }

            protected override void CloseScope()
            {
                m_Target.Clear();
            }
        }

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
                bool needReset = false;
                do
                {
                    if (m_CSVBuffer.Buffer != m_SetBuffer)
                    {
                        needReset = true;
                        break;
                    }
                    if (!string.Equals(m_CBufferName, m_SetBufferName, System.StringComparison.Ordinal))
                    {
                        needReset = true;
                        break;
                    }
                    if (m_SetBufferMatIndex != m_CBufferMaterialIndex)
                    {
                        needReset = true;
                        break;
                    }
                    var mat = GetMaterial(m_SetBufferMatIndex);
                    if (m_SetMaterial != mat)
                    {
                        needReset = true;
                        break;
                    }
                }
                while (false);
               
                if (needReset)
                {
                    SetProperty(m_CSVBuffer.Buffer, m_CBufferName, m_CBufferMaterialIndex);
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
                    using (new MaterialPropertyBlockScope(mpb))
                    {
                        if (m_SetBufferMatIndex >= 0)
                        {
                            renderer.GetPropertyBlock(mpb, m_SetBufferMatIndex);
                        }
                        else
                        {
                            renderer.GetPropertyBlock(mpb);
                        }
                        mpb.SetConstantBuffer(m_SetBufferName, (GraphicsBuffer)null, 0, 0);
                        if (m_SetBufferMatIndex >= 0)
                        {
                            renderer.SetPropertyBlock(mpb, m_SetBufferMatIndex);
                        }
                    }
                }

                m_SetBufferName = string.Empty;
                m_SetBuffer = null;
                m_SetBufferMatIndex = -1;
                m_SetMaterial = null;
            }
        }

        private void SetProperty(GraphicsBuffer buffer, string name, int matIndex)
        {
            ClearProperty();
            var renderer = GetRenderer();
            if (renderer != null)
            {
                int matCount = 0;
                Material mat = null;
                using (new MaterialLsistScope(s_MaterialList))
                {
                    renderer.GetSharedMaterials(s_MaterialList);
                    matCount = s_MaterialList.Count;
                    if (matIndex >= 0 && matIndex < matCount)
                    {
                        mat = s_MaterialList[matIndex];
                    }
                }
                    
                var mpb = GetMPB();
                using (new MaterialPropertyBlockScope(mpb))
                {
                    if (matIndex >= 0)
                    {
                        if (mat == null)
                        {
                            return;
                        }
                        renderer.GetPropertyBlock(mpb, matIndex);
                    }
                    else
                    {
                        renderer.GetPropertyBlock(mpb);
                    }

                    if (buffer != null && !string.IsNullOrEmpty(name))
                    {
                        mpb.SetConstantBuffer(name, buffer, 0, buffer.count * buffer.stride);
                        m_SetBuffer = buffer;
                        m_SetBufferName = name;
                    }
                    if (matIndex >= 0)
                    {
                        renderer.SetPropertyBlock(mpb, matIndex);
                        m_SetBufferMatIndex = matIndex;
                    }
                    else
                    {
                        renderer.SetPropertyBlock(mpb);
                        m_SetBufferMatIndex = -1;
                    }
                }
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
            s_MPB ??= new MaterialPropertyBlock();
            return s_MPB;
        }

        private Material GetMaterial(int index)
        {
            var renderer = GetRenderer();
            if (renderer != null)
            {
                renderer.GetSharedMaterials(s_MaterialList);
                if (index >= 0 && index < s_MaterialList.Count)
                {
                    var mat = s_MaterialList[index];
                    s_MaterialList.Clear();
                    return mat;
                }
            }
            return null;
        }
    }
}