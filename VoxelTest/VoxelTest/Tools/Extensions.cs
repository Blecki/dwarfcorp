using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    /// <summary>
    /// A special static class which "extends" several different XNA objects.
    /// This is an obscure C# feature which allows you to add methods to existing objects.
    /// </summary>
    public static class Extensions
    {
        public static T[] SubArray<T>(this T[] data, int index, int length)
        {
            T[] result = new T[length];
            Array.Copy(data, index, result, 0, length);
            return result;
        }

        public static BoundingBox Expand(this BoundingBox box, float amount)
        {
            BoundingBox expandedBoundingBox = box;
            expandedBoundingBox.Min -= new Vector3(amount, amount, amount);
            expandedBoundingBox.Max += new Vector3(amount, amount, amount);

            return expandedBoundingBox;
        }

        public static Vector3 Center(this BoundingBox box)
        {
            return (box.Max + box.Min) * 0.5f;
        }

        public static Vector3 Extents(this BoundingBox box)
        {
            return (box.Max - box.Min) * 0.5f;
        }
    }
}
