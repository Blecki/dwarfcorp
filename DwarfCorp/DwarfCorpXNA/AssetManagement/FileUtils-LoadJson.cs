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
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    /// <summary>
    /// A static class with helper functions for saving/loading data to binary, JSON, and ZIP
    /// </summary>
    public static partial class FileUtils
    {
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
                Converters = StandardConverters
            });
        }

        public static T LoadCompressedJson<T>(String Path, Object Context)
        {
            using (FileStream fs = new FileStream(Path, FileMode.Open))
            using (var stream = new ZipInputStream(fs))
            {
                while (stream.GetNextEntry() != null)
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        using (JsonReader json = new JsonTextReader(reader))
                        {
                            JsonSerializer serializer = new JsonSerializer()
                            {
                                Context = new StreamingContext(StreamingContextStates.File, Context),
                                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                                TypeNameHandling = TypeNameHandling.All,
                                ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
                                TypeNameAssemblyFormat = FormatterAssemblyStyle.Full,
                                PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                            };
                            foreach (var converter in StandardConverters)
                                serializer.Converters.Add(converter);
                            return serializer.Deserialize<T>(json);
                        }
                    }
                }
                return default(T);
            }
        }

        public static T LoadJson<T>(string filePath, object context = null)
        {           
            using (var stream = new FileStream(filePath, FileMode.Open))
            using (StreamReader reader = new StreamReader(stream))
            {
                using (JsonReader json = new JsonTextReader(reader))
                {
                    JsonSerializer serializer = new JsonSerializer()
                    {
                        Context = new StreamingContext(StreamingContextStates.File, context),
                        ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                        TypeNameHandling = TypeNameHandling.All,
                        ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
                        TypeNameAssemblyFormat = FormatterAssemblyStyle.Full,
                        PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                    };
                    foreach (var converter in StandardConverters)
                        serializer.Converters.Add(converter);
                    return serializer.Deserialize<T>(json);
                }
            }
        }
    }
}
