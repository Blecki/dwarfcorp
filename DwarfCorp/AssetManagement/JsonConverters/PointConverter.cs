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
    public class PointConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JValue jObject = serializer.Deserialize<JValue>(reader);
            string[] tokens = jObject.Value.ToString().Split(',');

            string[] intTokens = new string[2];

            int i = 0;
            foreach (string s in tokens)
            {
                if (s != "" && s != " " && s != ",")
                {
                    intTokens[i] = s;
                    i++;
                }
            }

            if (!Int32.TryParse(intTokens[0], out var x) || !Int32.TryParse(intTokens[1], out var y))
                return Point.Zero;
            return new Point(x, y);
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Point);
        }
    }

}