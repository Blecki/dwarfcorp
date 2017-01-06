// PriorityQueue.cs
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{

    /// <summary>
    /// Convenience priority queue which assumes float values.
    /// </summary>
    /// <typeparam Name="TValue">The kind of thing to be stored in the queue.</typeparam>
    public class PriorityQueue<TValue> : PriorityQueue<TValue, float>
    {
    }

    /// <summary>
    /// This data structure maintains a sorted list of items put into it.
    /// </summary>
    /// <typeparam Name="TValue">The type stored in the queue</typeparam>
    /// <typeparam Name="TPriority">The type to be used for comparison</typeparam>
    public class PriorityQueue<TValue, TPriority>
        where TPriority : IComparable
    {
        private SortedDictionary<TPriority, Queue<TValue>> dict = new SortedDictionary<TPriority, Queue<TValue>>();

        public int Count { get; private set; }

        public bool Empty
        {
            get { return Count == 0; }
        }

        public PriorityQueue()
        {
        }

        public void Enqueue(TValue val)
        {
            Enqueue(val, default(TPriority));
        }

        public void Enqueue(TValue val, TPriority pri)
        {
            ++Count;
            if(!dict.ContainsKey(pri))
            {
                dict[pri] = new Queue<TValue>();
            }
            dict[pri].Enqueue(val);
        }

        public TValue Dequeue()
        {
            --Count;
            var item = dict.First();
            if(item.Value.Count == 1)
            {
                dict.Remove(item.Key);
            }
            return item.Value.Dequeue();
        }

        public KeyValuePair<TValue, TPriority> Peek()
        {
            var item = dict.First();
            return new KeyValuePair<TValue, TPriority>(item.Value.Peek(), item.Key);
        }

        public void Clear()
        {
            Count = 0;
            dict.Clear();
        }
    }

}