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
using System.Linq;
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

    public class MoneyConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JToken token = JToken.Load(reader);
            if (token.Type == JTokenType.Float || token.Type == JTokenType.Integer)
            {
                return new DwarfBux(token.ToObject<decimal>());
            }
            if (token.Type == JTokenType.String)
            {
                // customize this to suit your needs
                return new DwarfBux(Decimal.Parse(token.ToString(),
                       System.Globalization.CultureInfo.GetCultureInfo("es-ES")));
            }
            if (token.Type == JTokenType.Null && objectType == typeof(DwarfBux?))
            {
                return null;
            }
            if (token.Type == JTokenType.Object)
            {
                return token.ToObject<DwarfBux>();
            }
            throw new JsonSerializationException("Unexpected token type: " +
                                                  token.Type.ToString());
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(DwarfBux);
        }
    }

    /// <summary>
    /// Serializes and deserializes BoundingBox objects to JSON.
    /// </summary>
    public class BoxConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var box = (BoundingBox)value;
            writer.WriteStartObject();
            writer.WritePropertyName("MaxX");
            serializer.Serialize(writer, box.Max.X);
            writer.WritePropertyName("MaxY");
            serializer.Serialize(writer, box.Max.Y);
            writer.WritePropertyName("MaxZ");
            serializer.Serialize(writer, box.Max.Z);
            writer.WritePropertyName("MinX");
            serializer.Serialize(writer, box.Min.X);
            writer.WritePropertyName("MinY");
            serializer.Serialize(writer, box.Min.Y);
            writer.WritePropertyName("MinZ");
            serializer.Serialize(writer, box.Min.Z);
            writer.WriteEndObject();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject jsonObject = JObject.Load(reader);
            var properties = jsonObject.Properties().ToList();
            try
            {
                return new BoundingBox()
                {
                    Max = new Vector3(float.Parse((string)properties[0]),
                        float.Parse((string)properties[1]),
                        float.Parse((string)properties[2])),
                    Min = new Vector3(float.Parse((string)properties[3]),
                        float.Parse((string)properties[4]),
                        float.Parse((string)properties[5]))
                };
            }
            catch (System.OverflowException exception)
            {
                return new BoundingBox(new Vector3(-float.MaxValue, -float.MaxValue, -float.MaxValue),
                    new Vector3(float.MaxValue, float.MaxValue, float.MaxValue));
            }

        }

        public override bool CanWrite
        {
            get { return true; }
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(BoundingBox);
        }
    }

    /// <summary>
    /// Serializes and deserializes BoundingBox objects to JSON.
    /// </summary>
    public class ColorConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var color = (Color)value;
            writer.WriteStartArray();
            writer.WriteValue(color.R);
            writer.WriteValue(color.G);
            writer.WriteValue(color.B);
            writer.WriteValue(color.A);
            writer.WriteEndArray();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.StartArray)
            { 
                // [R, G, B, A]
                JArray jsonObject = JArray.Load(reader);
                return new Color(jsonObject[0].Value<int>(), jsonObject[1].Value<int>(), jsonObject[2].Value<int>(),
                    jsonObject[3].Value<int>());
            }
            else
            {
                //"R, G, B, A";
                string value = reader.Value.ToString();
                string[] toks = value.Split(',');
                return new Color(int.Parse(toks[0]), int.Parse(toks[1]), int.Parse(toks[2]), int.Parse(toks[3]));
            }

        }

        public override bool CanWrite
        {
            get { return true; }
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Color);
        }
    }

}