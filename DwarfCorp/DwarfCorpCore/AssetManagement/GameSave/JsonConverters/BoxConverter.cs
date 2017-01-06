// BoxConverter.cs
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
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DwarfCorp
{
    /// <summary>
    /// Serializes and deserializes Vector3 objects to JSON.
    /// </summary>
    public class Vector3Converter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JValue jObject = serializer.Deserialize<JValue>(reader);
            string[] tokens = jObject.Value.ToString().Split(' ', ',');

            string[] intTokens = new string[3];

            int i = 0;
            foreach (string s in tokens)
            {
                if (s != "" && s != " " && s != ",")
                {
                    intTokens[i] = s;
                    i++;
                }
            }

            return new Vector3(float.Parse(intTokens[0]), float.Parse(intTokens[1]), float.Parse(intTokens[2]));
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Vector3);
        }
    }


    /// <summary>
    /// Serializes and deserializes BoundingBox objects to JSON.
    /// </summary>
    public class BoxConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JValue jObject = serializer.Deserialize<JValue>(reader);
            string[] tokens = jObject.Value.ToString().Split(':', ' ', '{', '}');
            // {Min: {X: vx, Y: vy, Z:vz}, Max: { X:vx, Y: vy, Z:vz} 4, 6, 8, 13, 15, 17

            string minX = tokens[4];
            string minY = tokens[6];
            string minZ = tokens[8];
            string maxX = tokens[13];
            string maxY = tokens[15];
            string maxZ = tokens[17];

            return new BoundingBox(new Vector3(float.Parse(minX), float.Parse(minY), float.Parse(minZ)), new Vector3(float.Parse(maxX), float.Parse(maxY), float.Parse(maxZ)));
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(BoundingBox);
        }
    }

}