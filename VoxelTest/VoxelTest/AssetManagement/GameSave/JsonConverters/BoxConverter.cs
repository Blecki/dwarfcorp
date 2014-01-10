using System;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DwarfCorp
{

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