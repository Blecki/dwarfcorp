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
            string[] tokens = jObject.Value.ToString().Split(':', ' ', '{', '}');
            string minX = tokens[2];
            string minY = tokens[4];
            string width = tokens[6];
            string height = tokens[8];


            return new Rectangle(int.Parse(minX), int.Parse(minY), int.Parse(width), int.Parse(height));
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