using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public interface IActData
    {
    }

    public class ActData<T> : IActData
    {
        public T Data { get; set; }

        public ActData(T data)
        {
            Data = data;
        }
    }
}