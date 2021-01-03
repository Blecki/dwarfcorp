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

            if (!float.TryParse(intTokens[0], out var x) || !float.TryParse(intTokens[1], out var y) || !float.TryParse(intTokens[2], out var z))
                return Vector3.Zero;
            return new Vector3(x, y, z);
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
                try
                {
                    return new DwarfBux(decimal.Parse(token.ToString(),
                           global::System.Globalization.CultureInfo.GetCultureInfo("es-ES")));
                }
                catch (Exception)
                {
                    return new DwarfBux(0);
                }
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
                if (!float.TryParse((string)properties[0], out var maxx) || !float.TryParse((string)properties[1], out var maxy) || !float.TryParse((string)properties[2], out var maxz))
                    return Vector3.Zero;
                if (!float.TryParse((string)properties[3], out var minx) || !float.TryParse((string)properties[4], out var miny) || !float.TryParse((string)properties[5], out var minz))
                    return Vector3.Zero;

                return new BoundingBox()
                {
                    Max = new Vector3(maxx, maxy, maxz),
                    Min = new Vector3(minx, miny, minz)
                };
            }
            catch (global::System.OverflowException)
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
                if (!int.TryParse(toks[0], out var r) || !int.TryParse(toks[1], out var g) || !int.TryParse(toks[2], out var b) || !int.TryParse(toks[3], out var a))
                    return new Color(1, 1, 1, 1);

                return new Color(r, g, b, a);
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