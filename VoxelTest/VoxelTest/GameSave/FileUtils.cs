using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO.Compression;

namespace DwarfCorp
{
    public class FileUtils
    {

        public static T LoadJson<T>(string filePath, bool isCompressed)
        {
                string jsonText = FileUtils.Load(filePath, isCompressed);
                return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(jsonText);
        }

        public static bool SaveJSon<T>(T obj, string filePath, bool compress)
        {
            string output = Newtonsoft.Json.JsonConvert.SerializeObject(obj);
            return FileUtils.Save(output, filePath, compress);
        }
        

        public static bool Save(string output, string filePath, bool isCompressed)
        {
            if (!isCompressed)
            {
                using (System.IO.StreamWriter filestream = new System.IO.StreamWriter(filePath))
                {
                    filestream.Write(output);
                    filestream.Close();
                    return true;
                }
            }
            else
            {
                using (GZipStream zip = new GZipStream(new System.IO.FileStream(filePath, System.IO.FileMode.OpenOrCreate), CompressionMode.Compress))
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
            if (isCompressed)
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
            using (GZipStream stream = new GZipStream(new System.IO.MemoryStream(gzip), CompressionMode.Decompress))
            {
                const int size = 4096;
                byte[] buffer = new byte[size];
                using (System.IO.MemoryStream memory = new System.IO.MemoryStream())
                {
                    int count = 0;
                    do
                    {
                        count = stream.Read(buffer, 0, size);
                        if (count > 0)
                        {
                            memory.Write(buffer, 0, count);
                        }
                    }
                    while (count > 0);
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
