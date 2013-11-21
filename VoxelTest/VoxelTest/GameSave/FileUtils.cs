using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO.Compression;

namespace DwarfCorp
{

    class BoxConverter : JsonConverter
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

    public class FileUtils
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
            string jsonText = FileUtils.Load(filePath, isCompressed);
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(jsonText);
        }

        public static bool SaveJSon<T>(T obj, string filePath, bool compress)
        {
            string output = Newtonsoft.Json.JsonConvert.SerializeObject(obj, Formatting.Indented,
                new JsonSerializerSettings()
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Error
                });
            return FileUtils.Save(output, filePath, compress);
        }


        public static bool Save(string output, string filePath, bool isCompressed)
        {
            if(!isCompressed)
            {
                using(System.IO.StreamWriter filestream = new System.IO.StreamWriter(filePath))
                {
                    filestream.Write(output);
                    filestream.Close();
                    return true;
                }
            }
            else
            {
                using(GZipStream zip = new GZipStream(new System.IO.FileStream(filePath, System.IO.FileMode.OpenOrCreate), CompressionMode.Compress))
                {
                    byte[] data = System.Text.ASCIIEncoding.UTF8.GetBytes(output.ToCharArray());
                    zip.Write(data, 0, data.Length);
                    zip.Close();
                    return true;
                }
            }
        }

        public static string Load(string filePath, bool isCompressed)
        {
            string text = "";
            if(isCompressed)
            {
                text = Decompress(filePath);
            }
            else
            {
                text = System.IO.File.ReadAllText(filePath);
            }

            return text;
        }

        public static byte[] Decompress(byte[] gzip)
        {
            using(GZipStream stream = new GZipStream(new System.IO.MemoryStream(gzip), CompressionMode.Decompress))
            {
                const int size = 4096;
                byte[] buffer = new byte[size];
                using(System.IO.MemoryStream memory = new System.IO.MemoryStream())
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
            return Decompress(filePath, System.Text.ASCIIEncoding.UTF8);
        }

        public static string Decompress(string filePath, System.Text.Encoding encoding)
        {
            byte[] file = System.IO.File.ReadAllBytes(filePath);
            byte[] decompressed = Decompress(file);

            return encoding.GetString(decompressed);
        }
    }

}