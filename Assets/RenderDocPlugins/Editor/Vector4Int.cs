using System;
using System.Globalization;
using System.Runtime.InteropServices;
using UnityEngine;
using System.Runtime.CompilerServices;

namespace RenderDocPlugins
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Vector4Int : IEquatable<Vector4Int>, IFormattable
    {
        public int x { get { return m_X; } set { m_X = value; } }
        public int y { get { return m_Y; } set { m_Y = value; } }
        public int z { get { return m_Z; } set { m_Z = value; } }
        public int w { get { return m_W; } set { m_W = value; } }

        private int m_X;
        private int m_Y;
        private int m_Z;
        private int m_W;

        [MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public Vector4Int(int x, int y, int z, int w)
        {
            m_X = x;
            m_Y = y;
            m_Z = z;
            m_W = w;
        }

        // Set x, y and z components of an existing Vector.
        [MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public void Set(int x, int y, int z, int w)
        {
            m_X = x;
            m_Y = y;
            m_Z = z;
            m_W = w;
        }

        // Access the /x/, /y/, /z/ or /w/ component using [0], [1], [2] or [3] respectively.
        public int this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0: return x;
                    case 1: return y;
                    case 2: return z;
                    case 3: return w;
                    default:
                        throw new IndexOutOfRangeException(string.Format("Invalid Vector4Int index addressed: {0}!", index));
                }
            }

            set
            {
                switch (index)
                {
                    case 0: x = value; break;
                    case 1: y = value; break;
                    case 2: z = value; break;
                    case 3: w = value; break;
                    default:
                        throw new IndexOutOfRangeException(string.Format("Invalid Vector4Int index addressed: {0}!", index));
                }
            }
        }

        // Returns the length of this vector (RO).
        public float magnitude { get { return Mathf.Sqrt((float)(x * x + y * y + z * z + w * w)); } }

        // Returns the squared length of this vector (RO).
        public int sqrMagnitude { get { return x * x + y * y + z * z + w * w; } }
        
        // Returns the distance between /a/ and /b/.
        [MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static float Distance(Vector4Int a, Vector4Int b) { return (a - b).magnitude; }

        // Returns a vector that is made from the smallest components of two vectors.
        [MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static Vector4Int Min(Vector4Int lhs, Vector4Int rhs) { return new Vector4Int(Mathf.Min(lhs.x, rhs.x), Mathf.Min(lhs.y, rhs.y), Mathf.Min(lhs.z, rhs.z), Mathf.Min(lhs.w, rhs.w)); }

        // Returns a vector that is made from the largest components of two vectors.
        [MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static Vector4Int Max(Vector4Int lhs, Vector4Int rhs) { return new Vector4Int(Mathf.Max(lhs.x, rhs.x), Mathf.Max(lhs.y, rhs.y), Mathf.Max(lhs.z, rhs.z), Mathf.Max(lhs.w, rhs.w)); }

        // Multiplies two vectors component-wise.
        [MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static Vector4Int Scale(Vector4Int a, Vector4Int b) { return new Vector4Int(a.x * b.x, a.y * b.y, a.z * b.z, a.w * b.w); }

        // Multiplies every component of this vector by the same component of /scale/.
        [MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public void Scale(Vector4Int scale) { x *= scale.x; y *= scale.y; z *= scale.z; w *= scale.w; }

        [MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public void Clamp(Vector4Int min, Vector4Int max)
        {
            x = Math.Max(min.x, x);
            x = Math.Min(max.x, x);
            y = Math.Max(min.y, y);
            y = Math.Min(max.y, y);
            z = Math.Max(min.z, z);
            z = Math.Min(max.z, z);
            w = Math.Max(min.w, w);
            w = Math.Min(max.w, w);
        }

        // Converts a Vector4Int to a [[Vector4]].
        [MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static implicit operator Vector4(Vector4Int v)
        {
            return new Vector4(v.x, v.y, v.z, v.w);
        }

        // Converts a Vector4Int to a [[Vector3Int]].
        [MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static explicit operator Vector3Int(Vector4Int v)
        {
            return new Vector3Int(v.x, v.y, v.w);
        }

        // Converts a Vector4Int to a [[Vector2Int]].
        [MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static explicit operator Vector2Int(Vector4Int v)
        {
            return new Vector2Int(v.x, v.y);
        }

        [MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static Vector4Int FloorToInt(Vector4 v)
        {
            return new Vector4Int(
                Mathf.FloorToInt(v.x),
                Mathf.FloorToInt(v.y),
                Mathf.FloorToInt(v.z),
                Mathf.FloorToInt(v.w)
            );
        }

        [MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static Vector4Int CeilToInt(Vector4 v)
        {
            return new Vector4Int(
                Mathf.CeilToInt(v.x),
                Mathf.CeilToInt(v.y),
                Mathf.CeilToInt(v.z),
                Mathf.CeilToInt(v.w)
            );
        }

        [MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static Vector4Int RoundToInt(Vector4 v)
        {
            return new Vector4Int(
                Mathf.RoundToInt(v.x),
                Mathf.RoundToInt(v.y),
                Mathf.RoundToInt(v.z),
                Mathf.RoundToInt(v.w)
            );
        }

        [MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static Vector4Int operator +(Vector4Int a, Vector4Int b)
        {
            return new Vector4Int(a.x + b.x, a.y + b.y, a.z + b.z, a.w + b.w);
        }

        [MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static Vector4Int operator -(Vector4Int a, Vector4Int b)
        {
            return new Vector4Int(a.x - b.x, a.y - b.y, a.z - b.z, a.w - b.w);
        }

        [MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static Vector4Int operator *(Vector4Int a, Vector4Int b)
        {
            return new Vector4Int(a.x * b.x, a.y * b.y, a.z * b.z, a.w * b.w);
        }

        [MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static Vector4Int operator -(Vector4Int a)
        {
            return new Vector4Int(-a.x, -a.y, -a.z, -a.w);
        }

        [MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static Vector4Int operator *(Vector4Int a, int b)
        {
            return new Vector4Int(a.x * b, a.y * b, a.z * b, a.w * b);
        }

        [MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static Vector4Int operator *(int a, Vector4Int b)
        {
            return new Vector4Int(a * b.x, a * b.y, a * b.z, a * b.w);
        }

        [MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static Vector4Int operator /(Vector4Int a, int b)
        {
            return new Vector4Int(a.x / b, a.y / b, a.z / b, a.w / b);
        }

        [MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Vector4Int lhs, Vector4Int rhs)
        {
            return lhs.x == rhs.x && lhs.y == rhs.y && lhs.z == rhs.z && lhs.w == rhs.w;
        }

        [MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Vector4Int lhs, Vector4Int rhs)
        {
            return !(lhs == rhs);
        }

        [MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object other)
        {
            if (!(other is Vector4Int)) return false;

            return Equals((Vector4Int)other);
        }

        [MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public bool Equals(Vector4Int other)
        {
            return this == other;
        }

        public override int GetHashCode()
        {
            var yHash = y.GetHashCode();
            var zHash = z.GetHashCode();
            var wHash = w.GetHashCode();
            return x.GetHashCode() ^ (yHash << 4) ^ (yHash >> 28) ^ (zHash >> 4) ^ (zHash << 28) ^ (wHash << 4) ^ (wHash >> 28);
        }

        public override string ToString()
        {
            return ToString(null, CultureInfo.InvariantCulture.NumberFormat);
        }

        public string ToString(string format)
        {
            return ToString(format, CultureInfo.InvariantCulture.NumberFormat);
        }

        public string ToString(string format, IFormatProvider formatProvider)
        {
            return string.Format("({0}, {1}, {2}, {3})", x.ToString(format, formatProvider), y.ToString(format, formatProvider), z.ToString(format, formatProvider), w.ToString(format, formatProvider));
        }

        public static Vector4Int zero { get { return s_Zero; } }
        public static Vector4Int one { get { return s_One; } }

        private static readonly Vector4Int s_Zero = new Vector4Int(0, 0, 0, 0);
        private static readonly Vector4Int s_One = new Vector4Int(1, 1, 1, 1);
    }
}
