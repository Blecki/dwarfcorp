using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Collections.Concurrent;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
namespace DwarfCorp
{
    class Datastructures
    {
        public static Vector2 SafeMeasure(SpriteFont font, string text)
        {
            Vector2 extents = Vector2.One;

            if (text == null)
            {
                return extents;
            }

            try
            {
                extents = font.MeasureString(text);
            }
            catch (ArgumentException e)
            {
                Console.Error.WriteLine(e.Message);
                extents.X = text.Length * 20;
            }

            return extents;
        }

        public static EventWaitHandle WaitFor(EventWaitHandle[] waitHandles)
        {
            int iHandle = WaitHandle.WaitAny(waitHandles);
            EventWaitHandle wh = waitHandles[iHandle];

            return wh;
        }

        public static List<int> RandomIndices(int max)
        {
            List<int> toReturn = new List<int>(max);
            List<int> indices = new List<int>(max);

            for (int i = 0; i < max; i++)
            {
                indices.Add(i);
            }

            for (int i = 0; i < max; i++)
            {
                int r = PlayState.random.Next(indices.Count);

                toReturn.Add(indices[r]);
                indices.RemoveAt(r);
            }

            return toReturn;
        }

        public static IEnumerable<TKey> RandomKeys<TKey, TValue>(IDictionary<TKey, TValue> dict)
        {
            Random rand = new Random();
            List<TKey> values = Enumerable.ToList(dict.Keys);



            int size = dict.Count;

            if (size > 0)
            {
                while (true)
                {
                    yield return values[rand.Next(size)];
                }
            }
        }

        public static T[,] RotateClockwise<T>(T[,] A)
        {
            int nr = A.GetLength(0);
            int nc = A.GetLength(1);

            T[,] toReturn = new T[nc, nr];

            for (int r = 0; r < nc; r++)
            {
                for (int c = 0; c < nr; c++)
                {
                    toReturn[r, c] = A[c, r];
                }
            }

            for (int r = 0; r < nc; r++)
            {
                for (int c = 0; c < nr / 2; c++)
                {
                    Swap(ref toReturn[r, c], ref toReturn[r, nr - c - 1]);
                }
            }

            return toReturn;
        }

        public static void Swap<T>(ref T a, ref T b)
        {
            T temp = a;
            a = b;
            b = temp;
        }
    }
}
