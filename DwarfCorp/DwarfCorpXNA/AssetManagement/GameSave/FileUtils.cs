// FileUtils.cs
// 
//  Modified MIT License (MIT)
//  
//  Copyright (c) 2015 Completely Fair Games Ltd.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// The following content pieces are considered PROPRIETARY and may not be used
// in any derivative works, commercial or non commercial, without explicit 
// written permission from Completely Fair Games:
// 
// * Images (sprites, textures, etc.)
// * 3D Models
// * Sound Effects
// * Music
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

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

    /// <summary>
    /// A static class with helper functions for saving/loading data to binary, JSON, and ZIP
    /// </summary>
    public static class FileUtils
    {

        /// <summary>
        /// Loads a serialized binary object from the given file.
        /// </summary>
        /// <typeparam name="T">The type to deserialize</typeparam>
        /// <param name="filepath">The filepath.</param>
        /// <returns>A deserialized object of type T if it could be deserialized, exception otherwise.</returns>
        public static T LoadBinary<T>(string filepath)
        {
            IFormatter formatter = new BinaryFormatter();
            Stream stream = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.None);
            T toReturn = default(T);
            try
            {
                toReturn = (T) formatter.Deserialize(stream);
            }
            catch (InvalidCastException)
            {
            }

            stream.Close();
            return toReturn;
        }


        /// <summary>
        /// Serializes and saves an object to binary file path.
        /// </summary>
        /// <typeparam name="T">The type of the object to serialize</typeparam>
        /// <param name="obj">The object.</param>
        /// <param name="filepath">The filepath.</param>
        /// <returns>True if the object could be saved.</returns>
        public static bool SaveBinary<T>(T obj, string filepath)
        {
            IFormatter formatter = new BinaryFormatter();
            Stream stream = new FileStream(filepath, FileMode.Create, FileAccess.Write, FileShare.None);
            formatter.Serialize(stream, obj);
            stream.Close();
            return true;
        }


        /// <summary>
        /// Given the inline text of a JSON serialization, creates an object of type T deserialized from it.
        /// </summary>
        /// <typeparam name="T">The type of the object being deserialized</typeparam>
        /// <param name="jsonText">The json text.</param>
        /// <param name="context">The context object (ie the World).</param>
        /// <returns>An object of type T if it could be serialized, or an exception otherwise</returns>
        public static T LoadJsonFromString<T>(string jsonText, object context = null)
        {
            return JsonConvert.DeserializeObject<T>(jsonText, new JsonSerializerSettings()
            {
                Context = new StreamingContext(StreamingContextStates.File, context),
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                TypeNameHandling = TypeNameHandling.All,
                ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
                TypeNameAssemblyFormat = FormatterAssemblyStyle.Full,
                PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                Converters = new List<JsonConverter>
                    {
                        new BoxConverter(),
                        new Vector3Converter(),
                        new MatrixConverter(),
                        new ContentConverter<Texture2D>(GameState.Game.Content, TextureManager.AssetMap),
                        new RectangleConverter(),
                        new MoneyConverter(),
                        new ColorConverter()
                    }
            });
        }

        /// <summary>
        /// Loads the json from a file.
        /// </summary>
        /// <typeparam name="T">The type of the object to deserialized</typeparam>
        /// <param name="filePath">The file path.</param>
        /// <param name="isCompressed">if set to <c>true</c> file is gzip compressed.</param>
        /// <param name="context">The context object passed to deserialized objects (ie the world).</param>
        /// <returns>An object of type T if it could be deserialized.</returns>
        public static T LoadJson<T>(string filePath, bool isCompressed, object context = null)
        {
            string jsonText = Load(filePath, isCompressed);
            return JsonConvert.DeserializeObject<T>(jsonText, new JsonSerializerSettings()
            {
                Context = new StreamingContext(StreamingContextStates.File, context),
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                TypeNameHandling = TypeNameHandling.All,
                ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
                TypeNameAssemblyFormat = FormatterAssemblyStyle.Full,
                PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                Converters = new List<JsonConverter>
                    {
                        new BoxConverter(),
                        new Vector3Converter(),
                        new MatrixConverter(),
                        new ContentConverter<Texture2D>(GameState.Game.Content, TextureManager.AssetMap),
                        new RectangleConverter(),
                        new MoneyConverter(),
                        new ColorConverter()
                    }
            });
        }

        /// <summary>
        /// Serializes an object and writes it to a file using the most basic Json settings.
        /// </summary>
        /// <typeparam name="T">The type of the object to save.</typeparam>
        /// <param name="obj">The object.</param>
        /// <param name="filePath">The file path.</param>
        /// <returns>True if the object could be saved.</returns>
        public static bool SaveBasicJson<T>(T obj, string filePath)
        {
            JsonSerializer serializer = new JsonSerializer
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                TypeNameHandling = TypeNameHandling.Auto,
                TypeNameAssemblyFormat = FormatterAssemblyStyle.Simple,
                ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
                Formatting = Formatting.Indented
            };

            serializer.Converters.Add(new BoxConverter());
            serializer.Converters.Add(new Vector3Converter());
            serializer.Converters.Add(new MatrixConverter());
            serializer.Converters.Add(new ContentConverter<Texture2D>(GameState.Game.Content, TextureManager.AssetMap));
            serializer.Converters.Add(new RectangleConverter());
            serializer.Converters.Add(new MoneyConverter());

            return Save(serializer, obj, filePath, false);

        }

        public static string SerializeBasicJSON<T>(T obj)
        {
            JsonSerializer serializer = new JsonSerializer
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                TypeNameHandling = TypeNameHandling.Auto,
                TypeNameAssemblyFormat = FormatterAssemblyStyle.Simple,
                ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
                Formatting = Formatting.Indented
            };

            serializer.Converters.Add(new BoxConverter());
            serializer.Converters.Add(new Vector3Converter());
            serializer.Converters.Add(new MatrixConverter());
            //serializer.Converters.Add(new ContentConverter<Texture2D>(GameState.Game.Content, TextureManager.AssetMap));
            serializer.Converters.Add(new RectangleConverter());
            serializer.Converters.Add(new MoneyConverter());
            serializer.Converters.Add(new ColorConverter());
            return JsonConvert.SerializeObject(obj, Formatting.Indented, serializer.Converters.ToArray());

        }



        /// <summary>
        /// Saves an object of type T to JSON using the full serialization method (most robust).
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <param name="obj">The object to save.</param>
        /// <param name="filePath">The file path.</param>
        /// <param name="compress">if set to <c>true</c> write with GZIP compression.</param>
        /// <returns>True if the object could be saved.</returns>
        public static bool SaveJSon<T>(T obj, string filePath, bool compress)
        {
            JsonSerializer serializer = new JsonSerializer
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                TypeNameHandling = TypeNameHandling.All,
                TypeNameAssemblyFormat = FormatterAssemblyStyle.Full,
                ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
                Formatting = Formatting.Indented,
                PreserveReferencesHandling = PreserveReferencesHandling.Objects
            };

            serializer.Converters.Add(new BoxConverter());
            serializer.Converters.Add(new Vector3Converter());
            serializer.Converters.Add(new MatrixConverter());
            serializer.Converters.Add(new ContentConverter<Texture2D>(GameState.Game.Content, TextureManager.AssetMap));
            serializer.Converters.Add(new RectangleConverter());
            serializer.Converters.Add(new MoneyConverter());
            serializer.Converters.Add(new ColorConverter());
            return Save(serializer, obj, filePath, compress);

        }

        /// <summary>
        /// Saves an object of type T using the given Json Serializer
        /// </summary>
        /// <typeparam name="T">the type of the object to save.</typeparam>
        /// <param name="serializer">The serializer.</param>
        /// <param name="obj">The object.</param>
        /// <param name="filePath">The file path.</param>
        /// <param name="compress">if set to <c>true</c> uses gzip compression.</param>
        /// <returns>true if the object could be saved.</returns>
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


        /// <summary>
        /// Writes the given string to an output file.
        /// </summary>
        /// <param name="output">The string to write.</param>
        /// <param name="filePath">The file path.</param>
        /// <param name="isCompressed">if set to <c>true</c> writes the file using gzip compression.</param>
        /// <returns>True if the file could be saved.</returns>
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

        /// <summary>
        /// Loads the specified file path.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="isCompressed">if set to <c>true</c> [is compressed].</param>
        /// <returns>The text content of the file.</returns>
        public static string Load(string filePath, bool isCompressed)
        {
            string text = "";
            text = isCompressed ? Decompress(filePath) : File.ReadAllText(filePath);

            return text;
        }

        /// <summary>
        /// Decompresses a byte array from gzip.
        /// </summary>
        /// <param name="gzip">The gzip-encoded byte array.</param>
        /// <returns>A decompressed byte array.</returns>
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

        /// <summary>
        /// Decompresses the file in the specified file path, returning a decompressed string.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <returns>A decompressed string.</returns>
        public static string Decompress(string filePath)
        {
            return Decompress(filePath, Encoding.UTF8);
        }

        /// <summary>
        /// Decompresses the specified file path using the given encoding.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="encoding">The encoding.</param>
        /// <returns>A decompressed string contents of the file.</returns>
        public static string Decompress(string filePath, Encoding encoding)
        {
            byte[] file = File.ReadAllBytes(filePath);
            byte[] decompressed = Decompress(file);

            return encoding.GetString(decompressed);
        }
    }

}
