using System;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DwarfCorp
{

    /// <summary>
    /// Serializes and deserializes Rectangle objects to JSON.
    /// </summary>
    public class RectangleConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JValue jObject = serializer.Deserialize<JValue>(reader);
            string[] tokens = jObject.Value.ToString().Split(' ', ',');

            string[] intTokens = new string[4];

            int i = 0;
            foreach (string s in tokens)
            {
                if (s != " " && s != ",")
                {
                    intTokens[i] = s;
                    i++;
                }
            }

            return new Rectangle(int.Parse(intTokens[0]), int.Parse(intTokens[1]), int.Parse(intTokens[2]), int.Parse(intTokens[3]));
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Rectangle);
        }
    }

}