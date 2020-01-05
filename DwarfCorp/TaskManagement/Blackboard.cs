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
    public class Blackboard
    {
        public Dictionary<string, ActData> Data { get; set; }

        public Blackboard()
        {
            Data = new Dictionary<string, ActData>();
        }

        public static implicit operator Blackboard(KeyValuePair<string, object> value)
        {
            return Create(value.Key, value.Value);
        }

        public static implicit operator Blackboard(KeyValuePair<string, float> value)
        {
            return Create(value.Key, value.Value);
        }

        public static implicit operator Blackboard(KeyValuePair<string, int> value)
        {
            return Create(value.Key, value.Value);
        }

        public static implicit operator Blackboard(KeyValuePair<string, bool> value)
        {
            return Create(value.Key, value.Value);
        }


        public static Blackboard Create<T>(string tag, T data)
        {
            Blackboard toReturn = new Blackboard();
            toReturn.SetData(tag, data);
            return toReturn;
        }

        public static Blackboard CreateDict<T>(Dictionary<string, T> dict)
        {
            Blackboard toReturn = new Blackboard();
            foreach (KeyValuePair<string, T> pair in dict)
            {
                toReturn.SetData(pair.Key, pair.Value);
            }

            return toReturn;
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
            return Has(key) ? Data[key].GetData<T>() : def;
        }

        public T GetData<T>(string key)
        {
            if(Has(key))
            {
                return Data[key].GetData<T>();
            }
            return default(T);
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