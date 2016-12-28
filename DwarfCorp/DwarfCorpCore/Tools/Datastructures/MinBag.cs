// MinBag.cs
// 
//  Modified MIT License (MIT)
//  
//  Copyright (c) 2015 Completely Fair Games Ltd.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// The following content pieces are considered PROPRIETARY and may not be used
// in any derivative works, commercial or non commercial, without explicit 
// written permission from Completely Fair Games:
// 
// * Images (sprites, textures, etc.)
// * 3D Models
// * Sound Effects
// * Music
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System.Collections.Generic;

namespace DwarfCorp
{
    /// <summary>
    ///     This data structure always has the minimum K elements (not necessarily sorted). It throws
    ///     out all other elements.
    /// </summary>
    /// <typeparam Name="T">The type of object stored in the bag.</typeparam>
    public class MinBag<T>
    {
        private KeyValuePair<T, float> Max;

        public MinBag(int maxSize)
        {
            MaxSize = maxSize;
            Queue = new PriorityQueue<T, float>();
            Data = new List<T>();
        }

        public int MaxSize { get; set; }
        public PriorityQueue<T, float> Queue { get; set; }
        public List<T> Data { get; set; }


        public void Clear()
        {
            Queue.Clear();
            Data.Clear();
        }

        public bool Add(T element, float value)
        {
            if (Queue.Count < MaxSize)
            {
                Queue.Enqueue(element, -value);
                Data.Add(element);
                Max = Queue.Peek();
                return true;
            }

            if (!(value < -Max.Value))
            {
                return false;
            }

            Data.Remove(Queue.Dequeue());
            Queue.Enqueue(element, -value);
            Data.Add(element);
            Max = Queue.Peek();

            return true;
        }
    }
}