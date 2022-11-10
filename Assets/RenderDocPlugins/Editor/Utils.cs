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
            return format switch
            {
                VertexAttributeFormat.Float32 or VertexAttributeFormat.UInt32 or VertexAttributeFormat.SInt32 => 4,
                VertexAttributeFormat.Float16 or VertexAttributeFormat.UNorm16 or VertexAttributeFormat.SNorm16 or VertexAttributeFormat.UInt16 or VertexAttributeFormat.SInt16 => 2,
                VertexAttributeFormat.UNorm8 or VertexAttributeFormat.SNorm8 or VertexAttributeFormat.UInt8 or VertexAttributeFormat.SInt8 => 1,
                _ => throw new System.IndexOutOfRangeException(),
            };
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
