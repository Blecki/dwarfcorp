using System;
using System.Collections.Generic;
using System.Drawing.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DwarfCorp
{

    /// <summary>
    /// Serializes and deserializes content of type T from asset tags.
    /// </summary>
    /// <typeparam Name="T">The type of the object to convert</typeparam>
    public class ContentConverter<T> : JsonConverter
    {
        private readonly ContentManager contentManager;
        private readonly Dictionary<T, string> assetMap; 

        public ContentConverter(ContentManager manager, Dictionary<T, string> assets)
        {
            contentManager = manager;
            assetMap = assets;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if(assetMap.ContainsKey((T) value))
            {
                writer.WriteValue(assetMap[(T) value]);
            }
            else
            {
                writer.WriteValue("");
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JValue jObject = serializer.Deserialize<JValue>(reader);
            return (jObject.Value != null && (string) jObject.Value != "") ? contentManager.Load<T>(jObject.Value.ToString()) : default(T);
        }

        public override bool CanWrite
        {
            get { return true; }
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(T);
        }
    }

}