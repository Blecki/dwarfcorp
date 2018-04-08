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

using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using System.Collections.Generic;

namespace DwarfCorp
{
    public static partial class FileUtils
    {
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

        public static T LoadCompressedJsonFromAbsolutePath<T>(String Path, Object Context)
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

        public static T LoadJsonFromAbsolutePath<T>(string filePath, object context = null)
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

        public static T LoadJsonFromResolvedPath<T>(string filePath, object context = null)
        {
            using (var stream = new FileStream(AssetManager.ResolveContentPath(filePath), FileMode.Open))
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

        /// <summary>
        /// Load a json list from all enabled mods, combining entries into one list. JSON must contain a List<T> as the top level element.</T>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="AssetPath"></param>
        /// <param name="Context"></param>
        /// <param name="Name">Given a T, this func must return a unique name - this is used to compare items to see if they should be overriden by the mod.</param>
        /// <returns></returns>
        public static List<T> LoadJsonListFromMultipleSources<T>(String AssetPath, Object Context, Func<T,String> Name)
        {
            var result = new Dictionary<String, T>();

            foreach (var resolvedAssetPath in AssetManager.EnumerateMatchingPaths(AssetPath))
            {
                var list = LoadJsonFromAbsolutePath<List<T>>(resolvedAssetPath, Context);
                foreach (var item in list)
                {
                    var name = Name(item);
                    if (!result.ContainsKey(name))
                        result.Add(name, item);
                }
            }

            return new List<T>(result.Values);
        }

        public static List<String> LoadConfigurationLinesFromMultipleSources(String AssetPath)
        {
            var result = new List<String>();

            foreach (var resolvedAssetPath in AssetManager.EnumerateMatchingPaths(AssetPath))
                result.AddRange(System.IO.File.ReadAllLines(resolvedAssetPath));

            return result;
        }
    }
}
