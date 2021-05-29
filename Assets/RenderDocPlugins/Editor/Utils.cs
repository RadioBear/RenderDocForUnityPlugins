using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;


namespace RenderDocPlugins
{
    public class Utils
    {
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

        public static int GetVertexAttributeByteSize(VertexAttributeDescriptor desc)
        {
            return GetByteSize(desc.format, desc.dimension);
        }
    }
}
