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
using ICSharpCode.SharpZipLib;
using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Microsoft.Xna.Framework.Graphics;
using System.IO.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;

namespace DwarfCorp
{
    /// <summary>
    /// A static class with helper functions for saving/loading data to binary, JSON, and ZIP
    /// </summary>
    public static partial class FileUtils
    {
        private static List<JsonConverter> StandardConverters = new List<JsonConverter>
        {
            new BoxConverter(),
            new Vector3Converter(),
            new MatrixConverter(),
            new TextureContentConverter(),
            new RectangleConverter(),
            new MoneyConverter(),
            new ColorConverter(),
            new Newtonsoft.Json.Converters.StringEnumConverter(),
            new Rail.CompassConnectionConverter()
        };

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
                Formatting = Formatting.Indented,
                ContractResolver = new DefaultContractResolver()
            };

            foreach (var converter in StandardConverters)
                serializer.Converters.Add(converter);

            return Save(serializer, obj, filePath, false);
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
                PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                ContractResolver = new DefaultContractResolver()
            };

            foreach (var converter in StandardConverters)
                serializer.Converters.Add(converter);

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
                using (StreamWriter filestream = new StreamWriter(File.Open(filePath, System.IO.FileMode.OpenOrCreate)))
                using (JsonWriter writer = new JsonTextWriter(filestream))
                {
                    serializer.Serialize(writer, obj);
                    return true;
                }
            }
            else
            {
                using (var zip = new ZipOutputStream(new FileStream(filePath, FileMode.OpenOrCreate)))
                using (JsonWriter writer = new JsonTextWriter(new StreamWriter(zip)))
                {
                    zip.SetLevel(9); // 0 - store only to 9 - means best compression
                    var entry = new ZipEntry(Path.GetFileName(filePath));
                    zip.PutNextEntry(entry);
                    serializer.Serialize(writer, obj);
                    return true;
                }
            }
        }

        internal static void DeleteOldestSave(string filename, int v1, string v2)
        {
            throw new NotImplementedException();
        }
    }
}
