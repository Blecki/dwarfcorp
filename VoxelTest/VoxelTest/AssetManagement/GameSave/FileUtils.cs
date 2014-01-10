using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Microsoft.Xna.Framework.Graphics;
using System.IO.Compression;

namespace DwarfCorp
{

    /// <summary>
    /// A static class with helper functions for saving/loading data to binary, JSON, and ZIP
    /// </summary>
    public static class FileUtils
    {
        public static bool SaveBinary<T>(T obj, string filepath)
        {

            IFormatter formatter = new BinaryFormatter();
            Stream stream = new FileStream(filepath, FileMode.Create, FileAccess.Write, FileShare.None);
            formatter.Serialize(stream, obj);
            stream.Close();


            return true;

        }

        public static T LoadJson<T>(string filePath, bool isCompressed)
        {
            string jsonText = Load(filePath, isCompressed);
            return JsonConvert.DeserializeObject<T>(jsonText, new JsonSerializerSettings()
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Error,
                TypeNameHandling = TypeNameHandling.All,
                ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
                TypeNameAssemblyFormat = FormatterAssemblyStyle.Full,
                Converters = new List<JsonConverter>
                    {
                        new BoxConverter(),
                        new MatrixConverter(),
                        new ContentConverter<Texture2D>(GameState.Game.Content, TextureManager.AssetMap),
                        new RectangleConverter()
                    }
            });
        }

        public static bool SaveJSon<T>(T obj, string filePath, bool compress)
        {
            JsonSerializer serializer = new JsonSerializer
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Error,
                TypeNameHandling = TypeNameHandling.All,
                TypeNameAssemblyFormat = FormatterAssemblyStyle.Full,
                ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
                Formatting = Formatting.Indented
            };

            serializer.Converters.Add(new BoxConverter());
            serializer.Converters.Add(new MatrixConverter());
            serializer.Converters.Add(new ContentConverter<Texture2D>(GameState.Game.Content, TextureManager.AssetMap));
            serializer.Converters.Add(new RectangleConverter());

            return Save(serializer, obj, filePath, compress);

        }

        public static bool Save<T>(JsonSerializer serializer, T obj, string filePath, bool compress)
        {

            if (!compress)
            {
                using (StreamWriter filestream = new StreamWriter(filePath))
                using (JsonWriter writer = new JsonTextWriter(filestream))
                {
                    serializer.Serialize(writer, obj);
                    return true;
                }
            }
            else
            {
                using (GZipStream zip = new GZipStream(new FileStream(filePath, FileMode.OpenOrCreate), CompressionMode.Compress))
                using (JsonWriter writer = new JsonTextWriter(new StreamWriter(zip)))
                {
                    serializer.Serialize(writer, obj);
                    return true;
                }
            }
        }


        public static bool Save(string output, string filePath, bool isCompressed)
        {
            if(!isCompressed)
            {
                using(StreamWriter filestream = new StreamWriter(filePath))
                {
                    filestream.Write(output);
                    filestream.Close();
                    return true;
                }
            }
            else
            {
                using(GZipStream zip = new GZipStream(new FileStream(filePath, FileMode.OpenOrCreate), CompressionMode.Compress))
                {
                    byte[] data = Encoding.UTF8.GetBytes(output.ToCharArray());
                    zip.Write(data, 0, data.Length);
                    zip.Close();
                    return true;
                }
            }
        }

        public static string Load(string filePath, bool isCompressed)
        {
            string text = "";
            text = isCompressed ? Decompress(filePath) : File.ReadAllText(filePath);

            return text;
        }

        public static byte[] Decompress(byte[] gzip)
        {
            using(GZipStream stream = new GZipStream(new MemoryStream(gzip), CompressionMode.Decompress))
            {
                const int size = 4096;
                byte[] buffer = new byte[size];
                using(MemoryStream memory = new MemoryStream())
                {
                    int count = 0;
                    do
                    {
                        count = stream.Read(buffer, 0, size);
                        if(count > 0)
                        {
                            memory.Write(buffer, 0, count);
                        }
                    } while(count > 0);
                    return memory.ToArray();
                }
            }
        }

        public static string Decompress(string filePath)
        {
            return Decompress(filePath, Encoding.UTF8);
        }

        public static string Decompress(string filePath, Encoding encoding)
        {
            byte[] file = File.ReadAllBytes(filePath);
            byte[] decompressed = Decompress(file);

            return encoding.GetString(decompressed);
        }
    }

}