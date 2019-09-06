using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DwarfCorp
{
    public class DifficultyConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            try
            {
                var jObject = serializer.Deserialize<JValue>(reader);
                if (jObject.Value is Int32 intv)
                    return Library.EnumerateDifficulties().FirstOrDefault(d => d.CombatModifier == intv);
                else
                    return null;
            }
            catch (Exception e)
            {
                return serializer.Deserialize<Difficulty>(reader);
            }
        }                

        public override bool CanWrite
        {
            get { return false; }
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Difficulty);
        }
    }
}