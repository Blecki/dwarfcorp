// Blackboard.cs
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