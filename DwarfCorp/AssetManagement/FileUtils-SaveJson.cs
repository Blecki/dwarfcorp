using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using DwarfCorp.GameStates;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Microsoft.Xna.Framework.Graphics;
using System.IO.Compression;

namespace DwarfCorp
{
    public static partial class FileUtils
    {
        private static List<JsonConverter> StandardConverters = new List<JsonConverter>
        {
            new BoxConverter(),
            new Vector3Converter(),
            new PointConverter(),
            new MatrixConverter(),
            new TextureContentConverter(),
            new RectangleConverter(),
            new MoneyConverter(),
            new ColorConverter(),
            new Newtonsoft.Json.Converters.StringEnumConverter(),
            new Rail.CompassConnectionConverter(),
            new AnimationFrameConverter()
        };

        public class TypeNameSerializationBinder : SerializationBinder
        {
            public TypeNameSerializationBinder()
            {
            }

            public override void BindToName(Type serializedType, out string assemblyName, out string typeName)
            {
                assemblyName = AssetManager.GetSourceModOfType(serializedType).IdentifierString;
                typeName = serializedType.AssemblyQualifiedName;
            }

            public override Type BindToType(string assemblyName, string typeName)
            {
                return AssetManager.GetTypeFromMod(typeName, assemblyName);
            }
        }

        private static JsonSerializer GetStandardSerializer(Object Context)
        {
            var serializer  = new JsonSerializer
            {
                Context = new StreamingContext(StreamingContextStates.File, Context),
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                TypeNameHandling = TypeNameHandling.Auto,
                TypeNameAssemblyFormat = FormatterAssemblyStyle.Simple,
                ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
                Formatting = Formatting.Indented,
                ContractResolver = new DefaultContractResolver(),
                Binder = new TypeNameSerializationBinder(),
                PreserveReferencesHandling = PreserveReferencesHandling.Objects,
            };

            foreach (var converter in StandardConverters)
                serializer.Converters.Add(converter);

            return serializer;
        }

        public static string SerializeBasicJSON<T>(T obj)
        {
            return JsonConvert.SerializeObject(obj, Formatting.Indented, StandardConverters.ToArray());
        }

        /// <summary>
        /// Saves an object of type T to JSON using the full serialization method (most robust).
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <param name="obj">The object to save.</param>
        /// <param name="filePath">The file path.</param>
        /// <returns>True if the object could be saved.</returns>
        public static bool SaveJSON<T>(T obj, string filePath)
        {
            return Save(GetStandardSerializer(null), obj, filePath);

        }

        /// <summary>
        /// Saves an object of type T using the given Json Serializer
        /// </summary>
        /// <typeparam name="T">the type of the object to save.</typeparam>
        /// <param name="serializer">The serializer.</param>
        /// <param name="obj">The object.</param>
        /// <param name="filePath">The file path.</param>
        /// <returns>true if the object could be saved.</returns>
        private static bool Save<T>(JsonSerializer serializer, T obj, string filePath)
        {
            using (StreamWriter filestream = new StreamWriter(File.Open(filePath, global::System.IO.FileMode.Create)))
            using (JsonWriter writer = new JsonTextWriter(filestream))
            {
                serializer.Serialize(writer, obj);
                return true;
            }
        }

        internal static string NormalizePath(string asset)
        {
            if (asset == null)
                return null;
            return asset.Replace('\\', Program.DirChar).Replace('/', Program.DirChar);
        }
    }
}
