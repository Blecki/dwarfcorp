using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    /// <summary>
    /// A blackboard is a generic collection of named memory references. 
    /// Acts should read/write data from the blackboard. This makes data management 
    /// clearer and easier to control. All creatures have blackboards.
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class Blackboard
    {
        public Dictionary<string, ActData> Data { get; set; }

        public Blackboard()
        {
            Data = new Dictionary<string, ActData>();
        }

        public void Clear()
        {
            Data.Clear();
        }

        public T GetData<T>(string key)
        {
            if(Data.ContainsKey(key))
            {
                return Data[key].GetData<T>();
            }
            else
            {
                return default(T);
            }
        }

        public void SetData<T>(string key, T value)
        {
            Data[key] = new ActData(value);
        }


        public void Erase(string key)
        {
            if(Data.ContainsKey(key))
            {
                Data.Remove(key);
            }
        }

        public ActData this[string key]
        {
            get { return Data[key]; }
            set { Data[key] = value; }
        }
    }

}