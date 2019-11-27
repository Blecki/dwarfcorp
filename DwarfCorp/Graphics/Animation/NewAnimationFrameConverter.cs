using System;
using System.Runtime.Serialization.Formatters;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DwarfCorp
{
    public class NewAnimationFrameConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jObject = serializer.Deserialize<JValue>(reader);
            var jsonString = jObject.Value.ToString();

            var tokens = jsonString.Split(' ');
            if (tokens.Length != 3)
                throw new JsonSerializationException();

            var speed = 0.0f;
            var row = 0;
            var column = 0;

            if (!float.TryParse(tokens[0], out speed) || !int.TryParse(tokens[1], out column) || !int.TryParse(tokens[2], out row))
                throw new JsonSerializationException();

            return new NewAnimationDescriptor.Frame
            {
                Speed = speed,
                Row = row,
                Column = column
            };
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(NewAnimationDescriptor.Frame);
        }
    }

}