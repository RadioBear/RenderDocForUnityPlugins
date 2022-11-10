using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;


namespace RenderDocPlugins
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(Renderer))]
    public class SetMultiConstantBuffer : MonoBehaviour
    {
        private static MaterialPropertyBlock s_MPB = null;
        private static readonly List<Material> s_MaterialList = new List<Material>();

        [Serializable]
        public class CBufferInfo
        {
            [SerializeField]
            public string m_Name = string.Empty;
            [SerializeField]
            public CSVMaterialConstantBuffer m_CSV;

            // Runtime
            private GraphicsBuffer m_TargetBuffer = null;
            private string m_TargetName = string.Empty;


            public bool NeedUpdate()
            {
                GraphicsBuffer graphicsBuffer = (m_CSV != null) ? m_CSV.Buffer : null;
                if (graphicsBuffer != m_TargetBuffer)
                {
                    return true;
                }
                if (!string.Equals(m_Name, m_TargetName, System.StringComparison.Ordinal))
                {
                    return true;
                }
                return false;
            }

            public void Set(MaterialPropertyBlock mpb)
            {
                GraphicsBuffer graphicsBuffer = (m_CSV != null) ? m_CSV.Buffer : null;
                if (graphicsBuffer != null && !string.IsNullOrEmpty(m_Name))
                {
                    mpb.SetConstantBuffer(m_Name, graphicsBuffer, 0, graphicsBuffer.count * graphicsBuffer.stride);
                    m_TargetBuffer = graphicsBuffer;
                    m_TargetName = m_Name;
                }
            }

            public void Clear(MaterialPropertyBlock mpb)
            {
                if (m_TargetBuffer != null && !string.IsNullOrEmpty(m_TargetName))
                {
                    mpb.SetConstantBuffer(m_TargetName, (GraphicsBuffer)null, 0, 0);
                }
                m_TargetName = string.Empty;
                m_TargetBuffer = null;
            }
        }


        [SerializeField]
        public int m_MaterialIndex = -1;
        [SerializeField]
        public List<CBufferInfo> m_CBufferInfoList;

        // Runtime
        private Renderer m_TargetRenderer = null;
        private int m_TargetMatIndex = -2;
        private Material m_TargetMaterial = null;

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
            bool needUpdate = false;
            do
            {
                if (m_TargetMatIndex != m_MaterialIndex)
                {
                    needUpdate = true;
                    break;
                }
                if (m_TargetMatIndex >= 0)
                {
                    var mat = GetMaterial(m_TargetMatIndex);
                    if (m_TargetMaterial != mat)
                    {
                        needUpdate = true;
                        break;
                    }
                }
                else
                {
                    if (m_TargetMaterial != null)
                    {
                        needUpdate = true;
                        break;
                    }
                }
                if (m_CBufferInfoList != null)
                {
                    for (int i = 0; i < m_CBufferInfoList.Count; ++i)
                    {
                        if (m_CBufferInfoList[i] != null)
                        {
                            if (m_CBufferInfoList[i].NeedUpdate())
                            {
                                needUpdate = true;
                                break;
                            }
                        }
                    }
                }
            }
            while (false);
            if (needUpdate)
            {
                SetProperty();
            }
        }

        private void ClearProperty()
        {
            if (m_TargetMatIndex == -2)
            {
                return;
            }
            var renderer = GetRenderer();
            if (renderer != null)
            {
                var mpb = GetMPB();
                using (new MaterialPropertyBlockScope(mpb))
                {
                    if (m_TargetMatIndex >= 0)
                    {
                        renderer.GetPropertyBlock(mpb, m_TargetMatIndex);
                    }
                    else
                    {
                        renderer.GetPropertyBlock(mpb);
                    }
                    for (int i = 0; i < m_CBufferInfoList.Count; ++i)
                    {
                        if (m_CBufferInfoList[i] != null)
                        {
                            m_CBufferInfoList[i].Clear(mpb);
                        }
                    }
                    if (m_TargetMatIndex >= 0)
                    {
                        renderer.SetPropertyBlock(mpb, m_TargetMatIndex);
                    }
                    else
                    {
                        renderer.SetPropertyBlock(mpb);
                    }
                }
            }
            m_TargetMatIndex = -2;
            m_TargetMaterial = null;
        }

        private void SetProperty()
        {
            int matIndex = m_MaterialIndex;
            var renderer = GetRenderer();
            if (renderer != null)
            {
                Material mat = null;
                if (matIndex >= 0)
                {
                    using (new MaterialLsistScope(s_MaterialList))
                    {
                        renderer.GetSharedMaterials(s_MaterialList);
                        if (matIndex >= 0 && matIndex < s_MaterialList.Count)
                        {
                            mat = s_MaterialList[matIndex];
                        }
                    }
                    if (mat == null)
                    {
                        ClearProperty();
                        return;
                    }
                }

                var mpb = GetMPB();
                using (new MaterialPropertyBlockScope(mpb))
                {
                    if (matIndex >= 0)
                    {
                        renderer.GetPropertyBlock(mpb, matIndex);
                    }
                    else
                    {
                        renderer.GetPropertyBlock(mpb);
                    }
                    if (m_CBufferInfoList != null)
                    {
                        for (int i = 0; i < m_CBufferInfoList.Count; ++i)
                        {
                            if (m_CBufferInfoList[i] != null)
                            {
                                m_CBufferInfoList[i].Set(mpb);
                            }
                        }
                    }
                    if (matIndex >= 0)
                    {
                        renderer.SetPropertyBlock(mpb, matIndex);
                        m_TargetMatIndex = matIndex;
                        m_TargetMaterial = mat;

                    }
                    else
                    {
                        renderer.SetPropertyBlock(mpb);
                        m_TargetMatIndex = -1;
                        m_TargetMaterial = null;
                    }
                }
            }
        }

        private Renderer GetRenderer()
        {
            if (m_TargetRenderer == null)
            {
                TryGetComponent<Renderer>(out m_TargetRenderer);
            }
            return m_TargetRenderer;
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