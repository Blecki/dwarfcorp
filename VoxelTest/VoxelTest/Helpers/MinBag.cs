﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{

    public class MinBag<T>
    {
        public int MaxSize { get; set; }
        public PriorityQueue<T, float> Queue { get; set; }
        public List<T> Data { get; set; }
        private KeyValuePair<T, float> Max;

        public MinBag(int maxSize)
        {
            MaxSize = maxSize;
            Queue = new PriorityQueue<T, float>();
            Data = new List<T>();
        }


        public void Clear()
        {
            Queue.Clear();
            Data.Clear();
        }

        public bool Add(T element, float value)
        {
            if(Queue.Count < MaxSize)
            {
                Queue.Enqueue(element, -value);
                Data.Add(element);
                Max = Queue.Peek();
                return true;
            }

            if(!(value < -Max.Value))
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