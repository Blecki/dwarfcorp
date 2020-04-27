using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class Blackboard
    {
        [JsonProperty]
        private Dictionary<string, IActData> Data = new Dictionary<string, IActData>();

        public Blackboard()
        {
        }

        public void Clear()
        {
            Data.Clear();
        }

        public bool Has(string key)
        {
            return Data.ContainsKey(key);
        }

        public T GetData<T>(string key, T def)
        {
            if (Data.ContainsKey(key) && Data[key] is ActData<T> wrapper)
                return wrapper.Data;
            return def;
        }

        public T GetData<T>(string key)
        {
            if(Data.ContainsKey(key) && Data[key] is ActData<T> wrapper)
                return wrapper.Data;
            return default(T);
        }

        public void SetData<T>(string key, T value)
        {
            Data[key] = new ActData<T>(value);
        }


        public void Erase(string key)
        {
            if(Data.ContainsKey(key))
                Data.Remove(key);
        }
    }
}