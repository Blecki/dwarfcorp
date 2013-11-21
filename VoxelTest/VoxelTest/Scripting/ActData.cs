using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{

    public class ActData
    {
        public object Data { get; set; }

        public ActData(object data)
        {
            Data = data;
        }


        public T GetData<T>()
        {
            if(Data is T)
            {
                return (T) Data;
            }
            else
            {
                return default(T);
            }
        }
    }

}