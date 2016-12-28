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

using System.Collections.Generic;
using Newtonsoft.Json;

namespace DwarfCorp
{
    /// <summary>
    ///     A blackboard is a generic collection of named memory references.
    ///     Acts should read/write data from the blackboard. This makes data management
    ///     clearer and easier to control. All creatures have blackboards.
    /// </summary>
    [JsonObject(IsReference = true)]
    public class Blackboard
    {
        public Blackboard()
        {
            Data = new Dictionary<string, ActData>();
        }

        /// <summary>
        ///     Generic mapping from name to object.
        /// </summary>
        public Dictionary<string, ActData> Data { get; set; }

        /// <summary>
        ///     Generic accessor for a named data value.
        /// </summary>
        /// <param name="key">The name of the data.</param>
        /// <returns>The data if it exists. Throws KeyNotFound exception otherwise.</returns>
        public ActData this[string key]
        {
            get { return Data[key]; }
            set { Data[key] = value; }
        }


        /// <summary>
        ///     Create a blackboard using a single key-value pair.
        /// </summary>
        /// <param name="value">Name-to-object mapping.</param>
        /// <returns>A new blackboard containing just one key-value pair.</returns>
        public static implicit operator Blackboard(KeyValuePair<string, object> value)
        {
            return Create(value.Key, value.Value);
        }

        /// <summary>
        ///     Create a blackboard using a single key-value pair.
        /// </summary>
        /// <param name="value">Name-to-object mapping.</param>
        /// <returns>A new blackboard containing just one key-value pair.</returns>
        public static implicit operator Blackboard(KeyValuePair<string, float> value)
        {
            return Create(value.Key, value.Value);
        }

        /// <summary>
        ///     Create a blackboard using a single key-value pair.
        /// </summary>
        /// <param name="value">Name-to-object mapping.</param>
        /// <returns>A new blackboard containing just one key-value pair.</returns>
        public static implicit operator Blackboard(KeyValuePair<string, int> value)
        {
            return Create(value.Key, value.Value);
        }

        /// <summary>
        ///     Create a blackboard using a single key-value pair.
        /// </summary>
        /// <param name="value">Name-to-object mapping.</param>
        /// <returns>A new blackboard containing just one key-value pair.</returns>
        public static implicit operator Blackboard(KeyValuePair<string, bool> value)
        {
            return Create(value.Key, value.Value);
        }


        /// <summary>
        ///     Create a new blackboard using a single key-value pair.
        /// </summary>
        /// <typeparam name="T">The data type</typeparam>
        /// <param name="tag">The name of the data.</param>
        /// <param name="data">The generic data to store.</param>
        /// <returns>A blackboard containing just a name-value pair.</returns>
        public static Blackboard Create<T>(string tag, T data)
        {
            var toReturn = new Blackboard();
            toReturn.SetData(tag, data);
            return toReturn;
        }

        /// <summary>
        ///     Creates a blackboard from a dictionary of key-value pairs.
        /// </summary>
        /// <typeparam name="T">The type of data</typeparam>
        /// <param name="dict">The dictionary containing a mapping from names to values.</param>
        /// <returns>A new blackboard whose data comes from the dictionary.</returns>
        public static Blackboard CreateDict<T>(Dictionary<string, T> dict)
        {
            var toReturn = new Blackboard();
            foreach (var pair in dict)
            {
                toReturn.SetData(pair.Key, pair.Value);
            }

            return toReturn;
        }

        /// <summary>
        ///     Erase all data in the blackboard.
        /// </summary>
        public void Clear()
        {
            Data.Clear();
        }

        /// <summary>
        ///     Whether or not the given key is in the blackboard.
        /// </summary>
        /// <param name="key">Name of the data to obtain.</param>
        /// <returns>True if the blackboard has the data, false otherwise.</returns>
        public bool Has(string key)
        {
            return Data.ContainsKey(key);
        }

        /// <summary>
        ///     Returns the value of the data with the given key.
        /// </summary>
        /// <typeparam name="T">Type of the data</typeparam>
        /// <param name="key">Name of the data</param>
        /// <param name="def">Default to use if data doesn't exist</param>
        /// <returns>The data, if it exists. def otherwise.</returns>
        public T GetData<T>(string key, T def)
        {
            return Has(key) ? Data[key].GetData<T>() : def;
        }

        /// <summary>
        ///     Returns the value of the data with the given key.
        /// </summary>
        /// <typeparam name="T">The data type</typeparam>
        /// <param name="key">The name of the data.</param>
        /// <returns>The stored data, if it exists. default(T) otherwise.</returns>
        public T GetData<T>(string key)
        {
            return GetData(key, default(T));
        }

        /// <summary>
        ///     Set the generic blackboard data. Overwrites existing data.
        /// </summary>
        /// <typeparam name="T">Type of data.</typeparam>
        /// <param name="key">Name of data</param>
        /// <param name="value">value to set.</param>
        public void SetData<T>(string key, T value)
        {
            Data[key] = new ActData(value);
        }


        /// <summary>
        ///     Erases the blackboard data with the given key.
        /// </summary>
        /// <param name="key">Name of the data.</param>
        public void Erase(string key)
        {
            if (Data.ContainsKey(key))
            {
                Data.Remove(key);
            }
        }
    }
}