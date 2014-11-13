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