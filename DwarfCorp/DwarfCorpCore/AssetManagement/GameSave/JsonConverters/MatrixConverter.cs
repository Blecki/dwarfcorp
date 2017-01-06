// MatrixConverter.cs
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
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DwarfCorp
{

    /// <summary>
    /// Serializes and deserializes Matrix objects to JSON.
    /// </summary>
    public class MatrixConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }



        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JValue jObject = serializer.Deserialize<JValue>(reader);
            List<string> rows = jObject.Value.ToString().Split('{', '}').ToList();
            List<string> nonEmptyRows = rows.Where(row => row != "" && row != " ").ToList();
            List<string[]> values = nonEmptyRows.Select(row => row.Split(' ')).ToList();

            float[,] vs = new float[4, 4];

            for(int r = 0; r < 4; r++)
            {
                for(int c = 0; c < 4; c++)
                {
                    vs[r, c] = float.Parse(values[r][c].Split(':')[1]);
                }
            }
            Matrix toReturn = new Matrix(vs[0, 0], vs[0, 1], vs[0, 2], vs[0, 3], vs[1, 0], vs[1, 1], vs[1, 2], vs[1, 3], vs[2, 0], vs[2, 1], vs[2, 2], vs[2, 3], vs[3, 0], vs[3, 1], vs[3, 2], vs[3, 3]);
            return toReturn;
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Matrix);
        }
    }

}